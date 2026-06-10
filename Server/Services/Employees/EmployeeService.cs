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
using Server.Services.Shared;

namespace Server.Services.Employees;

public class EmployeeService(
    PrmDbContext context,
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
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
        var employees = await employeeRepository.GetAllAsync(status, department, cancellationToken);
        var users = await userRepository.GetByIdsAsync(employees.Select(e => e.UserId), cancellationToken);

        var items = employees.Select(e => new EmployeeListItemDto
        {
            Id = e.Id,
            UserId = e.UserId,
            FullName = users.TryGetValue(e.UserId, out var user) ? user.FullName : string.Empty,
            Department = e.Department,
            EmploymentStatus = e.EmploymentStatus,
            IsActive = e.IsActive
        }).ToList();

        return new EmployeeListResponseDto
        {
            Employees = items,
            Total = items.Count,
            AllocatedCount = items.Count(i => i.EmploymentStatus == AllocationConstants.EmploymentStatusAllocated),
            BenchCount = items.Count(i => i.EmploymentStatus == AllocationConstants.EmploymentStatusBench)
        };
    }

    public async Task<EmployeeDetailDto> GetEmployeeByIdAsync(long employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);
        var user = await userRepository.GetByIdAsync(employee.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Linked user not found.");

        var employeeSkills = await employeeSkillRepository.GetByEmployeeIdAsync(employeeId, cancellationToken);
        var skills = new List<EmployeeSkillDto>();
        foreach (var es in employeeSkills)
        {
            var skill = await skillRepository.GetByIdAsync(es.SkillId, cancellationToken);
            if (skill is null) continue;
            skills.Add(new EmployeeSkillDto
            {
                SkillId = skill.Id,
                SkillName = skill.SkillName,
                Category = skill.Category,
                ProficiencyLevel = es.ProficiencyLevel
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
            Id = employee.Id,
            UserId = employee.UserId,
            FullName = user.FullName,
            EmployeeCode = employee.EmployeeCode,
            Department = employee.Department,
            Designation = employee.Designation,
            EmploymentStatus = employee.EmploymentStatus,
            IsActive = employee.IsActive,
            ManagerUserId = employee.ManagerId,
            Skills = skills,
            ActiveAllocations = allocationDtos
        };
    }

    public async Task UpdateEmployeeAsync(
        long employeeId,
        UpdateEmployeeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);
        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Department))
            employee.Department = request.Department.Trim();

        if (!string.IsNullOrWhiteSpace(request.Designation))
            employee.Designation = request.Designation.Trim();

        employee.UpdatedAt = now;
        await employeeRepository.UpdateAsync(employee, cancellationToken);
        await employeeRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateEmployeeAsync(
        long actorUserId,
        long employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);
        if (!employee.IsActive)
            throw new ValidationAppException("Employee is already inactive.");

        var user = await userRepository.GetByIdAsync(employee.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Linked user not found.");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var oldEmploymentStatus = employee.EmploymentStatus;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
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

            employee.IsActive = false;
            employee.EmploymentStatus = AllocationConstants.EmploymentStatusBench;
            employee.UpdatedAt = now;
            await employeeRepository.UpdateAsync(employee, cancellationToken);

            user.IsActive = false;
            user.UpdatedAt = now;
            await userRepository.UpdateAsync(user, cancellationToken);

            await auditService.LogDeactivateAsync(
                actorUserId,
                AuditEntityConstants.Employees,
                employee.Id,
                new { isActive = true, employmentStatus = oldEmploymentStatus },
                new { isActive = false, employmentStatus = AllocationConstants.EmploymentStatusBench, endedAllocationIds },
                cancellationToken);

            await employeeRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Employee deactivated. {EntityName} {EntityId} by {ActorUserId}",
                AuditEntityConstants.Employees, employee.Id, actorUserId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task AddSkillAsync(
        long employeeId,
        AddSkillRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await GetEmployeeOrThrowAsync(employeeId, cancellationToken);

        var skillName = request.SkillName.Trim();
        var category = request.Category.Trim().ToUpperInvariant();
        var proficiency = request.ProficiencyLevel.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;

        var skill = await skillRepository.GetByNameAsync(skillName, cancellationToken);
        if (skill is null)
        {
            skill = new Skill
            {
                SkillName = skillName,
                Category = category,
                IsActive = true,
                CreatedAt = now
            };
            await skillRepository.AddAsync(skill, cancellationToken);
            await employeeRepository.SaveChangesAsync(cancellationToken);
        }

        if (await employeeSkillRepository.ExistsAsync(employeeId, skill.Id, cancellationToken))
            throw new ConflictAppException("Employee already has this skill.");

        await employeeSkillRepository.AddAsync(new EmployeeSkill
        {
            EmployeeId = employeeId,
            SkillId = skill.Id,
            ProficiencyLevel = proficiency,
            CreatedAt = now
        }, cancellationToken);

        await employeeRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSkillProficiencyAsync(
        long employeeId,
        long skillId,
        UpdateSkillProficiencyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await GetEmployeeOrThrowAsync(employeeId, cancellationToken);

        var employeeSkill = await employeeSkillRepository.GetAsync(employeeId, skillId, cancellationToken)
            ?? throw new NotFoundAppException("Skill not found for this employee.");

        employeeSkill.ProficiencyLevel = request.ProficiencyLevel.Trim().ToUpperInvariant();
        await employeeSkillRepository.UpdateAsync(employeeSkill, cancellationToken);
        await employeeRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveSkillAsync(long employeeId, long skillId, CancellationToken cancellationToken = default)
    {
        await GetEmployeeOrThrowAsync(employeeId, cancellationToken);

        var employeeSkill = await employeeSkillRepository.GetAsync(employeeId, skillId, cancellationToken)
            ?? throw new NotFoundAppException("Skill not found for this employee.");

        await employeeSkillRepository.RemoveAsync(employeeSkill, cancellationToken);
        await employeeRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignManagerAsync(
        long employeeId,
        AssignManagerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeOrThrowAsync(employeeId, cancellationToken);

        var manager = await userRepository.GetByIdAsync(request.ManagerUserId, cancellationToken)
            ?? throw new NotFoundAppException("Manager user not found.");

        if (!manager.IsActive || !string.Equals(manager.Role, RoleConstants.Manager, StringComparison.OrdinalIgnoreCase))
            throw new ValidationAppException("Specified user is not an active manager.", errorCode: ErrorCodes.InvalidManager);

        if (employee.UserId == request.ManagerUserId)
            throw new ValidationAppException("Employee cannot be their own manager.");

        employee.ManagerId = request.ManagerUserId;
        employee.UpdatedAt = DateTime.UtcNow;
        await employeeRepository.UpdateAsync(employee, cancellationToken);
        await employeeRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<TeamDashboardDto> GetTeamDashboardAsync(
        long managerUserId,
        CancellationToken cancellationToken = default)
    {
        var team = await employeeRepository.GetByManagerIdAsync(managerUserId, cancellationToken);
        if (team.Count == 0)
        {
            return new TeamDashboardDto();
        }

        var employeeIds = team.Select(e => e.Id).ToList();
        var userIds = team.Select(e => e.UserId).ToList();
        var users = await userRepository.GetByIdsAsync(userIds, cancellationToken);
        var allEmployeeSkills = await employeeSkillRepository.GetByEmployeeIdsAsync(employeeIds, cancellationToken);
        var skillIds = allEmployeeSkills.Select(es => es.SkillId).Distinct();
        var skills = await skillRepository.GetByIdsAsync(skillIds, cancellationToken);
        var activeAllocations = await allocationRepository.GetActiveByEmployeeIdsAsync(employeeIds, cancellationToken);

        var utilizationByEmployee = activeAllocations
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.AllocationPercentage));

        var skillsByEmployee = allEmployeeSkills
            .GroupBy(es => es.EmployeeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(es => skills.TryGetValue(es.SkillId, out var skill) ? skill.SkillName : string.Empty)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList());

        var bench = new List<TeamBenchEmployeeDto>();
        var active = new List<TeamActiveEmployeeDto>();
        var partialCount = 0;

        foreach (var employee in team)
        {
            var utilization = utilizationByEmployee.GetValueOrDefault(employee.Id, 0m);
            var name = users.TryGetValue(employee.UserId, out var user) ? user.FullName : string.Empty;

            if (utilization == 0)
            {
                bench.Add(new TeamBenchEmployeeDto
                {
                    Id = employee.Id,
                    Name = name,
                    Department = employee.Department,
                    Skills = skillsByEmployee.GetValueOrDefault(employee.Id, [])
                });
            }
            else
            {
                if (utilization < AllocationConstants.MaxUtilizationPercentage)
                    partialCount++;

                active.Add(new TeamActiveEmployeeDto
                {
                    Id = employee.Id,
                    Name = name,
                    AllocationPercentage = utilization,
                    AvailabilityPercentage = AllocationConstants.MaxUtilizationPercentage - utilization
                });
            }
        }

        return new TeamDashboardDto
        {
            BenchEmployees = bench,
            ActiveEmployees = active,
            BenchCount = bench.Count,
            PartialCount = partialCount
        };
    }

    public async Task<TeamMemberDetailDto> GetTeamMemberDetailAsync(
        long managerUserId,
        long employeeId,
        CancellationToken cancellationToken = default)
    {
        var employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new NotFoundAppException("Employee not found.", ErrorCodes.EmployeeNotFound);

        if (employee.ManagerId != managerUserId)
            throw new ForbiddenAppException("Employee is not on your team.", ErrorCodes.EmployeeNotOnTeam);

        var user = await userRepository.GetByIdAsync(employee.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Linked user not found.");

        var employeeSkills = await employeeSkillRepository.GetByEmployeeIdAsync(employeeId, cancellationToken);
        var skillDtos = new List<EmployeeSkillDto>();
        foreach (var es in employeeSkills)
        {
            var skill = await skillRepository.GetByIdAsync(es.SkillId, cancellationToken);
            if (skill is null) continue;
            skillDtos.Add(new EmployeeSkillDto
            {
                SkillId = skill.Id,
                SkillName = skill.SkillName,
                Category = skill.Category,
                ProficiencyLevel = es.ProficiencyLevel
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

        var totalUtilization = activeAllocations.Sum(a => a.AllocationPercentage);
        var sinceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7 * AllocationConstants.RecentActivityWeeks));
        var recentTags = await timesheetRepository.GetRecentActivityTagsByEmployeeIdsAsync(
            [employeeId], sinceDate, cancellationToken);
        var tagNames = recentTags.GetValueOrDefault(employeeId, []);

        return new TeamMemberDetailDto
        {
            Id = employee.Id,
            FullName = user.FullName,
            Department = employee.Department,
            EmploymentStatus = employee.EmploymentStatus,
            TotalUtilizationPercentage = totalUtilization,
            Skills = skillDtos,
            ActiveAllocations = allocationDtos,
            RecentActivityTags = tagNames
        };
    }

    private async Task<Employee> GetEmployeeOrThrowAsync(long employeeId, CancellationToken cancellationToken)
    {
        return await employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new NotFoundAppException("Employee not found.", ErrorCodes.EmployeeNotFound);
    }
}
