using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Common.Allocations;
using Server.Common.Audit;
using Server.Common.Errors;
using Server.Common.Roles;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Employees;
using Server.Models.Entities;
using Server.Repositories.Roles;
using Server.Services.Shared;

namespace Server.Services.Employees;

public partial class EmployeeService(
    IDbTransactionManager transactionManager,
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ISkillRepository skillRepository,
    IEmployeeSkillRepository employeeSkillRepository,
    IAllocationRepository allocationRepository,
    IProjectRepository projectRepository,
    ITimesheetRepository timesheetRepository,
    IAuditService auditService,
    ILogger<EmployeeService> logger) : IEmployeeService
{
    public async Task<EmployeeListResponseDto> GetAllEmployeesAsync(
        string? status,
        string? department,
        CancellationToken cancellationToken = default)
    {
        var profiles = await employeeRepository.GetAllAsync(status, department, cancellationToken);
        var users = await userRepository.GetByIdsAsync(profiles.Select(p => p.UserId), cancellationToken);

        var items = profiles.Select(p =>
        {
            users.TryGetValue(p.UserId, out var user);
            return new EmployeeListItemDto
            {
                Id = p.Id,
                UserId = p.UserId,
                FullName = user?.FullName ?? string.Empty,
                Department = user?.Department,
                EmploymentStatus = p.ResourceStatus,
                IsActive = user?.IsActive ?? false
            };
        }).ToList();

        return new EmployeeListResponseDto
        {
            Employees = items,
            Total = items.Count,
            AllocatedCount = items.Count(i => i.EmploymentStatus == ResourceStatusConstants.Allocated),
            BenchCount = items.Count(i => i.EmploymentStatus == ResourceStatusConstants.Bench)
        };
    }

    public async Task<EmployeeDetailDto> GetEmployeeByIdAsync(long employeeId, CancellationToken cancellationToken = default)
    {
        var profile = await GetResourceProfileOrThrowAsync(employeeId, cancellationToken);
        var user = await userRepository.GetByIdAsync(profile.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Linked user not found.");

        var profileSkills = await employeeSkillRepository.GetByUserIdAsync(profile.UserId, cancellationToken);
        var skills = new List<EmployeeSkillDto>();
        foreach (var ps in profileSkills)
        {
            var skill = await skillRepository.GetByIdAsync(ps.SkillId, cancellationToken);
            if (skill is null) continue;
            skills.Add(new EmployeeSkillDto
            {
                SkillId = skill.Id,
                SkillName = skill.SkillName,
                Category = skill.Category,
                ProficiencyLevel = ps.ProficiencyLevel
            });
        }

        var activeAllocations = await allocationRepository.GetActiveByEmployeeIdAsync(employeeId, cancellationToken);
        var allocationDtos = new List<ActiveAllocationDto>();
        foreach (var allocation in activeAllocations)
        {
            var project = await projectRepository.GetByIdAsync(allocation.ProjectId, cancellationToken);
            allocationDtos.Add(new ActiveAllocationDto
            {
                AllocationId = allocation.Id,
                ProjectName = project?.ProjectName ?? "Unknown",
                AllocationPercentage = allocation.AllocationPercentage,
                AllocationStartDate = allocation.AllocationStartDate,
                AllocationEndDate = allocation.AllocationEndDate
            });
        }

        return new EmployeeDetailDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = user.FullName,
            EmployeeCode = $"EMP-{profile.UserId:D6}",
            Department = user.Department,
            Designation = user.Designation,
            EmploymentStatus = profile.ResourceStatus,
            IsActive = user.IsActive,
            ManagerUserId = profile.ManagerId,
            Skills = skills,
            ActiveAllocations = allocationDtos
        };
    }

    public async Task UpdateEmployeeAsync(
        long employeeId,
        UpdateEmployeeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetResourceProfileOrThrowAsync(employeeId, cancellationToken);
        var user = await userRepository.GetByIdAsync(profile.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Linked user not found.");

        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Department))
            user.Department = request.Department.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(request.Designation))
            user.Designation = request.Designation.Trim().ToUpperInvariant();

        user.UpdatedAt = now;
        profile.UpdatedAt = now;

        await userRepository.UpdateAsync(user, cancellationToken);
        await employeeRepository.UpdateAsync(profile, cancellationToken);
        await employeeRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateEmployeeAsync(
        long actorUserId,
        long employeeId,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetResourceProfileOrThrowAsync(employeeId, cancellationToken);
        var user = await userRepository.GetByIdAsync(profile.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Linked user not found.");

        if (!user.IsActive)
            throw new ValidationAppException("Employee is already inactive.");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var oldResourceStatus = profile.ResourceStatus;

        await using var transaction = await transactionManager.BeginTransactionAsync(cancellationToken);
        try
        {
            var activeAllocations = await allocationRepository.GetActiveByEmployeeIdAsync(employeeId, cancellationToken);
            var endedAllocationIds = new List<long>();

            foreach (var allocation in activeAllocations)
            {
                allocation.AllocationStatus = AllocationStatusConstants.Ended;
                allocation.AllocationEndDate = today;
                allocation.UpdatedAt = now;
                await allocationRepository.UpdateAsync(allocation, cancellationToken);
                endedAllocationIds.Add(allocation.Id);
            }

            profile.ResourceStatus = ResourceStatusConstants.Bench;
            profile.UpdatedAt = now;
            await employeeRepository.UpdateAsync(profile, cancellationToken);

            user.IsActive = false;
            user.UpdatedAt = now;
            await userRepository.UpdateAsync(user, cancellationToken);

            await auditService.LogDeactivateAsync(
                actorUserId,
                AuditEntityConstants.Employees,
                profile.Id,
                new { isActive = true, resourceStatus = oldResourceStatus },
                new { isActive = false, resourceStatus = ResourceStatusConstants.Bench, endedAllocationIds },
                cancellationToken);

            await employeeRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Employee deactivated. {EntityName} {EntityId} by {ActorUserId}",
                AuditEntityConstants.Employees, profile.Id, actorUserId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task AssignManagerAsync(
        long employeeId,
        AssignManagerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var profile = await GetResourceProfileOrThrowAsync(employeeId, cancellationToken);

        var manager = await userRepository.GetByIdAsync(request.ManagerUserId, cancellationToken)
            ?? throw new NotFoundAppException("Manager user not found.");

        if (!manager.IsActive
            || !await roleRepository.UserHasRoleAsync(request.ManagerUserId, RoleConstants.Manager, cancellationToken))
            throw new ValidationAppException("Specified user is not an active manager.", errorCode: ErrorCodes.InvalidManager);

        if (profile.UserId == request.ManagerUserId)
            throw new ValidationAppException("Employee cannot be their own manager.");

        profile.ManagerId = request.ManagerUserId;
        profile.UpdatedAt = DateTime.UtcNow;
        await employeeRepository.UpdateAsync(profile, cancellationToken);
        await employeeRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<ResourceProfile> GetResourceProfileOrThrowAsync(long employeeId, CancellationToken cancellationToken)
    {
        return await employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new NotFoundAppException("Employee not found.", ErrorCodes.EmployeeNotFound);
    }
}
