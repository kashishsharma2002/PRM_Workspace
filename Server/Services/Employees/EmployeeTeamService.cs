using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Common.Allocations;
using Server.Common.Audit;
using Server.Common.Errors;
using Server.Exceptions;
using Server.Models.DTOs.Employees;
using Server.Repositories.Allocations;
using Server.Repositories.Employees;
using Server.Repositories.Projects;
using Server.Repositories.Timesheets;
using Server.Repositories.Users;
using Server.Services.Shared;

namespace Server.Services.Employees;

public class EmployeeTeamService(
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    ISkillRepository skillRepository,
    IEmployeeSkillRepository employeeSkillRepository,
    IAllocationRepository allocationRepository,
    IProjectRepository projectRepository,
    ITimesheetRepository timesheetRepository,
    IAuditService auditService,
    ILogger<EmployeeTeamService> logger) : IEmployeeTeamService
{
    public async Task<TeamDashboardDto> GetTeamDashboardAsync(
        long managerUserId,
        CancellationToken cancellationToken = default)
    {
        var team = await employeeRepository.GetByManagerIdAsync(managerUserId, cancellationToken);
        if (team.Count == 0)
            return new TeamDashboardDto();

        var profileIds = team.Select(e => e.Id).ToList();
        var userIds = team.Select(e => e.UserId).ToList();
        var users = await userRepository.GetByIdsAsync(userIds, cancellationToken);
        var allUserSkills = await employeeSkillRepository.GetByUserIdsAsync(userIds, cancellationToken);
        var skillIds = allUserSkills.Select(es => es.SkillId).Distinct();
        var skills = await skillRepository.GetByIdsAsync(skillIds, cancellationToken);
        var activeAllocations = await allocationRepository.GetActiveByEmployeeIdsAsync(profileIds, cancellationToken);

        var utilizationByProfile = activeAllocations
            .GroupBy(a => a.ResourceProfileId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.AllocationPercentage));

        var skillsByUserId = allUserSkills
            .GroupBy(es => es.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(es => skills.TryGetValue(es.SkillId, out var skill) ? skill.SkillName : string.Empty)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList());

        var bench = new List<TeamBenchEmployeeDto>();
        var active = new List<TeamActiveEmployeeDto>();
        var partialCount = 0;

        foreach (var profile in team)
        {
            var utilization = utilizationByProfile.GetValueOrDefault(profile.Id, 0m);
            users.TryGetValue(profile.UserId, out var user);
            var name = user?.FullName ?? string.Empty;

            if (utilization == 0)
            {
                bench.Add(new TeamBenchEmployeeDto
                {
                    Id = profile.Id,
                    Name = name,
                    Department = user?.Department,
                    Skills = skillsByUserId.GetValueOrDefault(profile.UserId, [])
                });
            }
            else
            {
                if (utilization < AllocationConstants.MaxUtilizationPercentage)
                    partialCount++;

                active.Add(new TeamActiveEmployeeDto
                {
                    Id = profile.Id,
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
        var profile = await employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new NotFoundAppException("Employee not found.", ErrorCodes.EmployeeNotFound);

        if (profile.ManagerId != managerUserId)
            throw new ForbiddenAppException("Employee is not on your team.", ErrorCodes.EmployeeNotOnTeam);

        var user = await userRepository.GetByIdAsync(profile.UserId, cancellationToken)
            ?? throw new NotFoundAppException("Linked user not found.");

        var profileSkills = await employeeSkillRepository.GetByUserIdAsync(profile.UserId, cancellationToken);
        var skillIds = profileSkills.Select(ps => ps.SkillId).Distinct().ToList();
        var skillsById = await skillRepository.GetByIdsAsync(skillIds, cancellationToken);
        var skillDtos = EmployeeDtoMapper.MapSkills(profileSkills, skillsById);

        var activeAllocations = await allocationRepository.GetActiveByEmployeeIdAsync(employeeId, cancellationToken);
        var projectIds = activeAllocations.Select(a => a.ProjectId).Distinct().ToList();
        var projectsById = await projectRepository.GetByIdsAsync(projectIds, cancellationToken);
        var allocationDtos = EmployeeDtoMapper.MapActiveAllocations(activeAllocations, projectsById);

        var totalUtilization = activeAllocations.Sum(a => a.AllocationPercentage);
        var sinceDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7 * AllocationConstants.RecentActivityWeeks));
        var recentTags = await timesheetRepository.GetRecentActivityTagsByEmployeeIdsAsync(
            [employeeId], sinceDate, cancellationToken);
        var tagNames = recentTags.GetValueOrDefault(employeeId, []);

        return new TeamMemberDetailDto
        {
            Id = profile.Id,
            FullName = user.FullName,
            Department = user.Department,
            EmploymentStatus = profile.ResourceStatus,
            IsTimesheetFrozen = profile.IsTimesheetFrozen,
            TotalUtilizationPercentage = totalUtilization,
            Skills = skillDtos,
            ActiveAllocations = allocationDtos,
            RecentActivityTags = tagNames
        };
    }

    public async Task RestoreTimesheetAccessAsync(
        long managerUserId,
        long employeeId,
        CancellationToken cancellationToken = default)
    {
        var profile = await employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new NotFoundAppException("Employee not found.", ErrorCodes.EmployeeNotFound);

        if (profile.ManagerId != managerUserId)
            throw new ForbiddenAppException("Employee is not on your team.", ErrorCodes.EmployeeNotOnTeam);

        if (!profile.IsTimesheetFrozen)
            throw new ValidationAppException("Timesheet access is not frozen for this employee.");

        var user = await userRepository.GetByIdAsync(profile.UserId, cancellationToken);
        var employeeName = user?.FullName ?? $"Employee {employeeId}";

        profile.IsTimesheetFrozen = false;
        profile.UpdatedAt = DateTime.UtcNow;
        await employeeRepository.UpdateAsync(profile, cancellationToken);
        await employeeRepository.SaveChangesAsync(cancellationToken);

        await auditService.LogUpdateAsync(
            managerUserId,
            AuditEntityConstants.ResourceProfiles,
            profile.Id,
            new { is_timesheet_frozen = true },
            new { is_timesheet_frozen = false, employee = employeeName, restored_by_manager = managerUserId },
            cancellationToken);

        logger.LogInformation(
            "Timesheet access restored for employee {EmployeeId} by manager {ManagerUserId}",
            employeeId,
            managerUserId);
    }
}
