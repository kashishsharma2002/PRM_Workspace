using System.Text.Json;
using Server.Common;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Allocations;
using Server.Models.Entities;

namespace Server.Services.Allocations;

public class AllocationService(
    PrmDbContext context,
    IAllocationRepository allocationRepository,
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    IProjectRepository projectRepository,
    IAuditLogRepository auditLogRepository) : IAllocationService
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
            var employee = await employeeRepository.GetByIdAsync(allocation.EmployeeId, cancellationToken);
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
            TotalActiveCount = items.Count(i => i.AllocationStatus == TimesheetConstants.AllocationStatusActive)
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
            .Where(i => i.AllocationStatus == TimesheetConstants.AllocationStatusActive)
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
        var employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new NotFoundAppException("Employee not found.");

        if (!employee.IsActive)
            throw new ValidationAppException("Employee is not active.");

        if (employee.ManagerId != managerUserId)
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

        var user = await userRepository.GetByIdAsync(employee.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Linked user not found.");

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
                EmployeeId = request.EmployeeId,
                ProjectId = request.ProjectId,
                AllocationPercentage = request.AllocationPercentage,
                AllocationStartDate = request.AllocationStartDate,
                AllocationEndDate = request.AllocationEndDate,
                AllocationStatus = TimesheetConstants.AllocationStatusActive,
                AllocatedByManagerId = managerUserId,
                CreatedAt = now,
                UpdatedAt = now
            };

            await allocationRepository.AddAsync(allocation, cancellationToken);
            await allocationRepository.SaveChangesAsync(cancellationToken);

            employee.EmploymentStatus = AllocationConstants.EmploymentStatusAllocated;
            employee.UpdatedAt = now;
            await employeeRepository.UpdateAsync(employee, cancellationToken);

            await auditLogRepository.AddAsync(new AuditLog
            {
                ActorUserId = managerUserId,
                EntityName = "PROJECT_ALLOCATIONS",
                EntityId = allocation.Id,
                ActionType = "CREATE",
                NewValues = JsonSerializer.Serialize(new
                {
                    allocation.EmployeeId,
                    allocation.ProjectId,
                    allocation.AllocationPercentage,
                    allocation.AllocationStartDate,
                    allocation.AllocationEndDate
                }),
                CreatedAt = now
            }, cancellationToken);

            await allocationRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CreateAllocationResponseDto
            {
                AllocationId = allocation.Id,
                EmployeeId = allocation.EmployeeId,
                ProjectId = allocation.ProjectId,
                AllocationPercentage = allocation.AllocationPercentage,
                AllocationStatus = allocation.AllocationStatus,
                EmploymentStatus = employee.EmploymentStatus
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

        if (!string.Equals(allocation.AllocationStatus, TimesheetConstants.AllocationStatusActive, StringComparison.OrdinalIgnoreCase))
            throw new ValidationAppException("Allocation is not active.");

        var project = await projectRepository.GetByIdAsync(allocation.ProjectId, cancellationToken)
            ?? throw new NotFoundAppException("Project not found.");

        if (project.ManagerUserId != managerUserId)
            throw new ForbiddenAppException("You do not own this project.");

        var employee = await employeeRepository.GetByIdAsync(allocation.EmployeeId, cancellationToken)
            ?? throw new NotFoundAppException("Employee not found.");

        if (employee.ManagerId != managerUserId)
            throw new ForbiddenAppException("Employee is not on your team.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;
        var oldStatus = allocation.AllocationStatus;
        var activeAllocations = await allocationRepository.GetActiveByEmployeeIdAsync(allocation.EmployeeId, cancellationToken);
        var hasOtherActive = activeAllocations.Any(a => a.Id != allocation.Id);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            allocation.AllocationEndDate = today;
            allocation.AllocationStatus = TimesheetConstants.AllocationStatusEnded;
            allocation.UpdatedAt = now;
            await allocationRepository.UpdateAsync(allocation, cancellationToken);

            if (!hasOtherActive)
            {
                employee.EmploymentStatus = AllocationConstants.EmploymentStatusBench;
                employee.UpdatedAt = now;
                await employeeRepository.UpdateAsync(employee, cancellationToken);
            }

            await auditLogRepository.AddAsync(new AuditLog
            {
                ActorUserId = managerUserId,
                EntityName = "PROJECT_ALLOCATIONS",
                EntityId = allocation.Id,
                ActionType = "END",
                OldValues = JsonSerializer.Serialize(new { allocationStatus = oldStatus }),
                NewValues = JsonSerializer.Serialize(new
                {
                    allocationStatus = allocation.AllocationStatus,
                    allocation.AllocationEndDate,
                    employee.EmploymentStatus
                }),
                CreatedAt = now
            }, cancellationToken);

            await allocationRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new EndAllocationResponseDto
            {
                AllocationId = allocation.Id,
                EmployeeId = allocation.EmployeeId,
                EmploymentStatus = employee.EmploymentStatus,
                AllocationEndDate = allocation.AllocationEndDate
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
