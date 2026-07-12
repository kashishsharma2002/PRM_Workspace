using Server.Common;
using Server.Common.Errors;
using Server.Common.Projects;
using Server.Common.Timesheets;
using Server.Exceptions;
using Server.Models.DTOs.Projects;
using Server.Models.DTOs.Scheduler;
using Server.Models.Entities;
using Server.Scheduler;
using Server.Services.Projects.Abstractions;
using Server.Services.Shared;

namespace Server.Services.Projects;

public partial class ProjectService
{
    public async Task<ManagerProjectListResponseDto> GetMyProjectsAsync(
        long managerUserId,
        CancellationToken cancellationToken = default)
    {
        var projects = await projectRepository.GetByManagerUserIdAsync(managerUserId, cancellationToken);
        return new ManagerProjectListResponseDto
        {
            Projects = projects.Select(p => new ManagerProjectListItemDto
            {
                Id = p.Id,
                ProjectName = p.ProjectName,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                HealthStatus = p.HealthStatus,
                ProjectStatus = p.ProjectStatus
            }).ToList()
        };
    }

    public async Task<ManagerProjectDetailDto> GetManagerProjectDetailAsync(
        long managerUserId,
        long projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundAppException("Project not found.", ErrorCodes.ProjectNotFound);

        if (project.ManagerUserId != managerUserId)
            throw new NotFoundAppException("Project not found.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var milestones = await milestoneRepository.GetByProjectIdAsync(projectId, cancellationToken);
        var milestoneDtos = milestones.Select(m => new ManagerProjectMilestoneDto
        {
            MilestoneTitle = m.MilestoneTitle,
            DueDate = m.DueDate,
            MilestoneStatus = m.MilestoneStatus,
            IsOverdue = m.DueDate < today && m.MilestoneStatus != MilestoneStatusConstants.Done
        }).ToList();

        var allocations = await allocationRepository.GetActiveByProjectIdAsync(projectId, cancellationToken);
        var resources = new List<ManagerProjectResourceDto>();
        foreach (var allocation in allocations)
        {
            var employee = await employeeRepository.GetByIdAsync(allocation.ResourceProfileId, cancellationToken);
            var user = employee is not null
                ? await userRepository.GetByIdAsync(employee.UserId, cancellationToken)
                : null;

            resources.Add(new ManagerProjectResourceDto
            {
                AllocationId = allocation.Id,
                EmployeeName = user?.FullName ?? "Unknown",
                AllocationPercentage = allocation.AllocationPercentage,
                AllocationStartDate = allocation.AllocationStartDate,
                AllocationEndDate = allocation.AllocationEndDate
            });
        }

        var riskFlags = await EvaluateRiskFlagsAsync(projectId, project.EndDate, milestones, allocations, cancellationToken);

        return new ManagerProjectDetailDto
        {
            Id = project.Id,
            ProjectCode = project.ProjectCode,
            ProjectName = project.ProjectName,
            EndDate = project.EndDate,
            HealthStatus = project.HealthStatus,
            Milestones = milestoneDtos,
            AllocatedResources = resources,
            RiskFlags = riskFlags
        };
    }

    public async Task<SchedulerHealthResultDto> EvaluateAllProjectsHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lastWeek = WeekDateHelper.GetMostRecentCompletedWeekMonday();
        var weekEnd = WeekDateHelper.GetWeekEnd(lastWeek);

        var projects = await projectRepository.GetActiveAsync(cancellationToken);
        var projectIds = projects.Select(p => p.Id).ToList();
        var milestones = await milestoneRepository.GetByProjectIdsAsync(projectIds, cancellationToken);
        var milestonesByProject = milestones.GroupBy(m => m.ProjectId).ToDictionary(g => g.Key, g => g.ToList());

        var maxWeeklyHours = await systemConfigService.GetMaxWeeklyHoursAsync(cancellationToken);
        var allocations = await allocationRepository.GetAllActiveForWeekAsync(lastWeek, weekEnd, cancellationToken);
        var allocationsByProject = allocations.GroupBy(a => a.ProjectId).ToDictionary(g => g.Key, g => g.ToList());
        var loggedHoursByProject = await timesheetRepository.GetLoggedHoursByProjectForWeekAsync(lastWeek, cancellationToken);

        var evaluatedCount = 0;
        var failedCount = 0;

        foreach (var project in projects)
        {
            try
            {
                var projectMilestones = milestonesByProject.GetValueOrDefault(project.Id, []);
                var projectAllocations = allocationsByProject.GetValueOrDefault(project.Id, []);
                var expectedHours = projectAllocations.Sum(a => a.AllocationPercentage / 100m * maxWeeklyHours);
                var loggedHours = loggedHoursByProject.GetValueOrDefault(project.Id, 0m);

                var context = ProjectHealthEvaluationContext.Create(
                    project.EndDate,
                    projectMilestones,
                    expectedHours,
                    loggedHours,
                    today);
                var flags = projectHealthFlagEvaluator.EvaluateFlags(context);
                var healthStatus = projectHealthFlagEvaluator.MapToHealthStatus(flags);

                await projectRepository.UpdateHealthStatusAsync(project.Id, healthStatus, cancellationToken);
                evaluatedCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                logger.LogWarning(ex, "Health evaluation failed for project {ProjectId}", project.Id);
            }
        }

        return new SchedulerHealthResultDto
        {
            EvaluatedCount = evaluatedCount,
            FailedCount = failedCount
        };
    }

    private async Task<List<string>> EvaluateRiskFlagsAsync(
        long projectId,
        DateOnly endDate,
        IReadOnlyList<ProjectMilestone> milestones,
        IReadOnlyList<ProjectAllocation> allocations,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lastWeek = WeekDateHelper.GetMostRecentCompletedWeekMonday();
        var maxWeeklyHours = await systemConfigService.GetMaxWeeklyHoursAsync(cancellationToken);
        var expectedHours = allocations.Sum(a => a.AllocationPercentage / 100m * maxWeeklyHours);
        var loggedHoursByProject = await timesheetRepository.GetLoggedHoursByProjectForWeekAsync(lastWeek, cancellationToken);
        var loggedHours = loggedHoursByProject.GetValueOrDefault(projectId, 0m);

        var context = ProjectHealthEvaluationContext.Create(
            endDate,
            milestones,
            expectedHours,
            loggedHours,
            today);

        return projectHealthFlagEvaluator.EvaluateFlags(context);
    }
}
