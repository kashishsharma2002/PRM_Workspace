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
    IDbTransactionManager transactionManager,
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
        var profileIds = allocations.Select(a => a.ResourceProfileId).Distinct().ToList();
        var projectIds = allocations.Select(a => a.ProjectId).Distinct().ToList();

        var profilesById = await employeeRepository.GetByIdsAsync(profileIds, cancellationToken);
        var userIds = profilesById.Values.Select(p => p.UserId).Distinct().ToList();
        var usersById = await userRepository.GetByIdsAsync(userIds, cancellationToken);
        var projectsById = await projectRepository.GetByIdsAsync(projectIds, cancellationToken);

        var items = allocations.Select(allocation =>
        {
            var employeeName = string.Empty;
            if (profilesById.TryGetValue(allocation.ResourceProfileId, out var employee)
                && usersById.TryGetValue(employee.UserId, out var user))
            {
                employeeName = user.FullName;
            }

            return new AllocationListItemDto
            {
                Id = allocation.Id,
                EmployeeName = employeeName,
                ProjectName = projectsById.TryGetValue(allocation.ProjectId, out var project)
                    ? project.ProjectName
                    : "Unknown",
                AllocationPercentage = allocation.AllocationPercentage,
                AllocationStartDate = allocation.AllocationStartDate,
                AllocationEndDate = allocation.AllocationEndDate,
                AllocationStatus = allocation.AllocationStatus
            };
        }).ToList();

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
        var projectIds = allocations.Select(a => a.ProjectId).Distinct().ToList();
        var projectsById = await projectRepository.GetByIdsAsync(projectIds, cancellationToken);

        var items = allocations.Select(allocation => new EmployeeAllocationItemDto
        {
            ProjectId = allocation.ProjectId,
            ProjectName = projectsById.TryGetValue(allocation.ProjectId, out var project)
                ? project.ProjectName
                : "Unknown",
            AllocationPercentage = allocation.AllocationPercentage,
            AllocationStartDate = allocation.AllocationStartDate,
            AllocationEndDate = allocation.AllocationEndDate,
            AllocationStatus = allocation.AllocationStatus
        }).ToList();

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
        var (profile, user, project) = await ValidateCreateRequestAsync(managerUserId, request, cancellationToken);

        await using var transaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        try
        {
            var allocation = await PersistNewAllocationAsync(managerUserId, request, profile, cancellationToken);
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
        var (allocation, profile) = await ValidateEndRequestAsync(managerUserId, allocationId, cancellationToken);

        await using var transaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        try
        {
            await PersistEndedAllocationAsync(managerUserId, allocation, profile, cancellationToken);
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
        var (allocation, profile, user, project, newPercentage, newStartDate, newEndDate) =
            await ValidateUpdateRequestAsync(managerUserId, allocationId, request, cancellationToken);

        await using var transaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        try
        {
            await PersistUpdatedAllocationAsync(
                managerUserId,
                allocation,
                profile,
                newPercentage,
                newStartDate,
                newEndDate,
                cancellationToken);
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

    private async Task<(ResourceProfile Profile, User User, Project Project)> ValidateCreateRequestAsync(
        long managerUserId,
        CreateAllocationRequestDto request,
        CancellationToken cancellationToken)
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

        return (profile, user, project);
    }

    private async Task<ProjectAllocation> PersistNewAllocationAsync(
        long managerUserId,
        CreateAllocationRequestDto request,
        ResourceProfile profile,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
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
        return allocation;
    }

    private async Task<(ProjectAllocation Allocation, ResourceProfile Profile)> ValidateEndRequestAsync(
        long managerUserId,
        long allocationId,
        CancellationToken cancellationToken)
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

        return (allocation, profile);
    }

    private async Task PersistEndedAllocationAsync(
        long managerUserId,
        ProjectAllocation allocation,
        ResourceProfile profile,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;
        var oldStatus = allocation.AllocationStatus;

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
    }

    private async Task<(ProjectAllocation Allocation, ResourceProfile Profile, User User, Project Project, decimal NewPercentage, DateOnly NewStartDate, DateOnly NewEndDate)>
        ValidateUpdateRequestAsync(
            long managerUserId,
            long allocationId,
            UpdateAllocationRequestDto request,
            CancellationToken cancellationToken)
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

        return (allocation, profile, user, project, newPercentage, newStartDate, newEndDate);
    }

    private async Task PersistUpdatedAllocationAsync(
        long managerUserId,
        ProjectAllocation allocation,
        ResourceProfile profile,
        decimal newPercentage,
        DateOnly newStartDate,
        DateOnly newEndDate,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var oldValues = new
        {
            allocation.AllocationPercentage,
            allocation.AllocationStartDate,
            allocation.AllocationEndDate
        };

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
