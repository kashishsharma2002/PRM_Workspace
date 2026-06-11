using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Common.Allocations;
using Server.Common.Audit;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Allocations;
using Server.Models.Entities;
using Server.Services.Employees;
using Server.Services.Shared;

namespace Server.Services.Allocations;

public class AllocationService(
    PrmDbContext context,
    IAllocationRepository allocationRepository,
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    IProjectRepository projectRepository,
    IResourceStatusService resourceStatusService,
    IAuditService auditService,
    ILogger<AllocationService> logger) : IAllocationService
{
    public async Task<AllocationListResponseDto> GetAllAllocationsAsync(
        long? employeeId,
        long? projectId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var allocations = await allocationRepository.GetAllAsync(employeeId, projectId, status, cancellationToken);
        var items = new List<AllocationListItemDto>();

        foreach (var allocation in allocations)
        {
            var employee = await employeeRepository.GetByIdAsync(allocation.ResourceProfileId, cancellationToken);
            var employeeName = string.Empty;
            if (employee is not null)
            {
                var user = await userRepository.GetByIdAsync(employee.UserId, cancellationToken);
                employeeName = user?.FullName ?? string.Empty;
            }

            var project = await projectRepository.GetByIdAsync(allocation.ProjectId, cancellationToken);

            items.Add(new AllocationListItemDto
            {
                Id = allocation.Id,
                EmployeeName = employeeName,
                ProjectName = project?.ProjectName ?? "Unknown",
                AllocationPercentage = allocation.AllocationPercentage,
                AllocationStartDate = allocation.AllocationStartDate,
                AllocationEndDate = allocation.AllocationEndDate,
                AllocationStatus = allocation.AllocationStatus
            });
        }

        return new AllocationListResponseDto
        {
            Allocations = items,
            TotalActiveCount = items.Count(i => i.AllocationStatus == AllocationStatusConstants.Active)
        };
    }

    public async Task<EmployeeAllocationListResponseDto> GetMyAllocationsAsync(
        long employeeId,
        CancellationToken cancellationToken = default)
    {
        var allocations = await allocationRepository.GetByEmployeeIdAsync(employeeId, cancellationToken);
        var items = new List<EmployeeAllocationItemDto>();

        foreach (var allocation in allocations)
        {
            var project = await projectRepository.GetByIdAsync(allocation.ProjectId, cancellationToken);
            items.Add(new EmployeeAllocationItemDto
            {
                ProjectId = allocation.ProjectId,
                ProjectName = project?.ProjectName ?? "Unknown",
                AllocationPercentage = allocation.AllocationPercentage,
                AllocationStartDate = allocation.AllocationStartDate,
                AllocationEndDate = allocation.AllocationEndDate,
                AllocationStatus = allocation.AllocationStatus
            });
        }

        var totalUtilization = items
            .Where(i => i.AllocationStatus == AllocationStatusConstants.Active)
            .Sum(i => i.AllocationPercentage);

        return new EmployeeAllocationListResponseDto
        {
            Allocations = items,
            TotalUtilizationPercentage = totalUtilization
        };
    }

    public async Task<CreateAllocationResponseDto> CreateAllocationAsync(
        long managerUserId,
        CreateAllocationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var profile = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundAppException("Employee not found.");

        var user = await userRepository.GetByIdAsync(profile.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Linked user not found.");

        if (!user.IsActive)
            throw new ValidationAppException("Employee is not active.");

        if (profile.ManagerId != managerUserId)
            throw new ForbiddenAppException("Employee is not on your team.");

        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundAppException("Project not found.");

        if (project.ManagerUserId != managerUserId)
            throw new ForbiddenAppException("You do not own this project.");

        var projectStatus = project.ProjectStatus.Trim().ToUpperInvariant();
        if (!AllocationConstants.AllocatableProjectStatuses.Contains(projectStatus))
            throw new ValidationAppException("Project must be in ACTIVE or PLANNED status.");

        if (request.AllocationStartDate < project.StartDate || request.AllocationEndDate > project.EndDate)
            throw new ValidationAppException("Allocation dates must be within the project date range.");

        var activeAllocations = await allocationRepository.GetActiveByEmployeeIdAsync(request.EmployeeId, cancellationToken);
        ValidateUtilization(
            user.FullName,
            activeAllocations,
            request.AllocationStartDate,
            request.AllocationEndDate,
            request.AllocationPercentage);

        var now = DateTime.UtcNow;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var allocation = new ProjectAllocation
            {
                ResourceProfileId = request.EmployeeId,
                ProjectId = request.ProjectId,
                AllocationPercentage = request.AllocationPercentage,
                AllocationStartDate = request.AllocationStartDate,
                AllocationEndDate = request.AllocationEndDate,
                AllocationStatus = AllocationStatusConstants.Active,
                AllocatedByUserId = managerUserId,
                CreatedAt = now,
                UpdatedAt = now
            };

            await allocationRepository.AddAsync(allocation, cancellationToken);
            await allocationRepository.SaveChangesAsync(cancellationToken);
            await resourceStatusService.ApplyStatusFromActiveAllocationsAsync(profile.Id, cancellationToken);
            await employeeRepository.SaveChangesAsync(cancellationToken);

            await auditService.LogCreateAsync(
                managerUserId,
                AuditEntityConstants.ProjectAllocations,
                allocation.Id,
                new
                {
                    allocation.ResourceProfileId,
                    allocation.ProjectId,
                    allocation.AllocationPercentage,
                    allocation.AllocationStartDate,
                    allocation.AllocationEndDate
                },
                cancellationToken);

            await allocationRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Allocation created. {EntityName} {EntityId} by {ActorUserId}",
                AuditEntityConstants.ProjectAllocations, allocation.Id, managerUserId);

            var updatedProfile = await employeeRepository.GetByIdAsync(profile.Id, cancellationToken);

            return new CreateAllocationResponseDto
            {
                AllocationId = allocation.Id,
                EmployeeId = allocation.ResourceProfileId,
                ProjectId = allocation.ProjectId,
                AllocationPercentage = allocation.AllocationPercentage,
                AllocationStatus = allocation.AllocationStatus,
                EmploymentStatus = updatedProfile?.ResourceStatus ?? ResourceStatusConstants.Bench
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<EndAllocationResponseDto> EndAllocationAsync(
        long managerUserId,
        long allocationId,
        CancellationToken cancellationToken = default)
    {
        var allocation = await allocationRepository.GetByIdAsync(allocationId, cancellationToken)
            ?? throw new NotFoundAppException("Allocation not found.");

        if (!string.Equals(allocation.AllocationStatus, AllocationStatusConstants.Active, StringComparison.OrdinalIgnoreCase))
            throw new ValidationAppException("Allocation is not active.");

        var project = await projectRepository.GetByIdAsync(allocation.ProjectId, cancellationToken)
            ?? throw new NotFoundAppException("Project not found.");

        if (project.ManagerUserId != managerUserId)
            throw new ForbiddenAppException("You do not own this project.");

        var profile = await employeeRepository.GetByIdAsync(allocation.ResourceProfileId, cancellationToken)
            ?? throw new NotFoundAppException("Employee not found.");

        if (profile.ManagerId != managerUserId)
            throw new ForbiddenAppException("Employee is not on your team.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;
        var oldStatus = allocation.AllocationStatus;
        var activeAllocations = await allocationRepository.GetActiveByEmployeeIdAsync(allocation.ResourceProfileId, cancellationToken);
        var hasOtherActive = activeAllocations.Any(a => a.Id != allocation.Id);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            allocation.AllocationEndDate = today;
            allocation.AllocationStatus = AllocationStatusConstants.Ended;
            allocation.UpdatedAt = now;
            await allocationRepository.UpdateAsync(allocation, cancellationToken);
            await allocationRepository.SaveChangesAsync(cancellationToken);
            await resourceStatusService.ApplyStatusFromActiveAllocationsAsync(profile.Id, cancellationToken);
            await employeeRepository.SaveChangesAsync(cancellationToken);
            var updatedProfileForAudit = await employeeRepository.GetByIdAsync(profile.Id, cancellationToken);

            await auditService.LogEndAsync(
                managerUserId,
                AuditEntityConstants.ProjectAllocations,
                allocation.Id,
                new { allocationStatus = oldStatus },
                new
                {
                    allocationStatus = allocation.AllocationStatus,
                    allocation.AllocationEndDate,
                    resourceStatus = updatedProfileForAudit?.ResourceStatus
                },
                cancellationToken);

            await allocationRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Allocation ended. {EntityName} {EntityId} by {ActorUserId}",
                AuditEntityConstants.ProjectAllocations, allocation.Id, managerUserId);

            var updatedProfile = await employeeRepository.GetByIdAsync(profile.Id, cancellationToken);

            return new EndAllocationResponseDto
            {
                AllocationId = allocation.Id,
                EmployeeId = allocation.ResourceProfileId,
                EmploymentStatus = updatedProfile?.ResourceStatus ?? ResourceStatusConstants.Bench,
                AllocationEndDate = allocation.AllocationEndDate
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<UpdateAllocationResponseDto> UpdateAllocationAsync(
        long managerUserId,
        long allocationId,
        UpdateAllocationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var allocation = await allocationRepository.GetByIdAsync(allocationId, cancellationToken)
            ?? throw new NotFoundAppException("Allocation not found.");

        if (!string.Equals(allocation.AllocationStatus, AllocationStatusConstants.Active, StringComparison.OrdinalIgnoreCase))
            throw new ValidationAppException("Allocation is not active.");

        var project = await projectRepository.GetByIdAsync(allocation.ProjectId, cancellationToken)
            ?? throw new NotFoundAppException("Project not found.");

        if (project.ManagerUserId != managerUserId)
            throw new ForbiddenAppException("You do not own this project.");

        var profile = await employeeRepository.GetByIdAsync(allocation.ResourceProfileId, cancellationToken)
            ?? throw new NotFoundAppException("Employee not found.");

        if (profile.ManagerId != managerUserId)
            throw new ForbiddenAppException("Employee is not on your team.");

        var user = await userRepository.GetByIdAsync(profile.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Linked user not found.");

        if (!user.IsActive)
            throw new ValidationAppException("Employee is not active.");

        var newPercentage = request.AllocationPercentage ?? allocation.AllocationPercentage;
        var newStartDate = request.AllocationStartDate ?? allocation.AllocationStartDate;
        var newEndDate = request.AllocationEndDate ?? allocation.AllocationEndDate;

        if (newEndDate <= newStartDate)
            throw new ValidationAppException("End date must be after start date.");

        if (newStartDate < project.StartDate || newEndDate > project.EndDate)
            throw new ValidationAppException("Allocation dates must be within the project date range.");

        var activeAllocations = await allocationRepository.GetActiveByEmployeeIdAsync(allocation.ResourceProfileId, cancellationToken);
        var otherActive = activeAllocations.Where(a => a.Id != allocation.Id).ToList();
        ValidateUtilization(
            user.FullName,
            otherActive,
            newStartDate,
            newEndDate,
            newPercentage);

        var now = DateTime.UtcNow;
        var oldValues = new
        {
            allocation.AllocationPercentage,
            allocation.AllocationStartDate,
            allocation.AllocationEndDate
        };

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            allocation.AllocationPercentage = newPercentage;
            allocation.AllocationStartDate = newStartDate;
            allocation.AllocationEndDate = newEndDate;
            allocation.UpdatedAt = now;
            await allocationRepository.UpdateAsync(allocation, cancellationToken);
            await allocationRepository.SaveChangesAsync(cancellationToken);
            await resourceStatusService.ApplyStatusFromActiveAllocationsAsync(profile.Id, cancellationToken);
            await employeeRepository.SaveChangesAsync(cancellationToken);

            await auditService.LogUpdateAsync(
                managerUserId,
                AuditEntityConstants.ProjectAllocations,
                allocation.Id,
                oldValues,
                new
                {
                    allocation.AllocationPercentage,
                    allocation.AllocationStartDate,
                    allocation.AllocationEndDate
                },
                cancellationToken);

            await allocationRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Allocation updated. {EntityName} {EntityId} by {ActorUserId}",
                AuditEntityConstants.ProjectAllocations, allocation.Id, managerUserId);

            var updatedProfile = await employeeRepository.GetByIdAsync(profile.Id, cancellationToken);

            return new UpdateAllocationResponseDto
            {
                AllocationId = allocation.Id,
                EmployeeId = allocation.ResourceProfileId,
                AllocationPercentage = allocation.AllocationPercentage,
                AllocationStartDate = allocation.AllocationStartDate,
                AllocationEndDate = allocation.AllocationEndDate,
                AllocationStatus = allocation.AllocationStatus,
                EmploymentStatus = updatedProfile?.ResourceStatus ?? ResourceStatusConstants.Bench
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void ValidateUtilization(
        string employeeName,
        IReadOnlyList<ProjectAllocation> activeAllocations,
        DateOnly startDate,
        DateOnly endDate,
        decimal newAllocationPercentage)
    {
        var overlappingTotal = activeAllocations
            .Where(a => a.AllocationStartDate <= endDate && a.AllocationEndDate >= startDate)
            .Sum(a => a.AllocationPercentage);

        var total = overlappingTotal + newAllocationPercentage;
        if (total > AllocationConstants.MaxUtilizationPercentage)
            throw new ValidationAppException(
                $"{employeeName} would be at {total:0}% utilisation. Maximum is 100%.");
    }
}
