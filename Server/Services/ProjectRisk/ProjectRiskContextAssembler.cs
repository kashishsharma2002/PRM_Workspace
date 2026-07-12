using Server.Common.Llm;
using Server.Common.Projects;
using Server.Models.DTOs.ProjectRisk.Context;
using Server.Repositories.Allocations;
using Server.Repositories.Projects;
using Server.Repositories.Timesheets;
using Server.Services.ProjectRisk.Abstractions;

namespace Server.Services.ProjectRisk;

public class ProjectRiskContextAssembler(
    IProjectRepository projectRepository,
    IMilestoneRepository milestoneRepository,
    IAllocationRepository allocationRepository,
    ITimesheetRepository timesheetRepository) : IProjectRiskContextAssembler
{
    public async Task<ProjectRiskContextModel> BuildAsync(long projectId, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project with ID {projectId} not found.");

        var milestones = await milestoneRepository.GetByProjectIdAsync(projectId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fourWeeksAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-AiValidationLimits.RecentTimesheetWindowDays));

        var allocations = await allocationRepository
            .GetActiveWithEmployeeNamesByProjectIdAsync(projectId, cancellationToken);
        var timesheetHours = await timesheetRepository
            .GetRecentLoggedHoursByProjectIdAsync(projectId, fourWeeksAgo, cancellationToken);

        return new ProjectRiskContextModel
        {
            ProjectId = project.Id,
            ProjectName = project.ProjectName,
            ProjectCode = project.ProjectCode,
            Description = project.Description,
            ProjectStatus = project.ProjectStatus,
            HealthStatus = project.HealthStatus,
            StartDate = project.StartDate.ToString("yyyy-MM-dd"),
            EndDate = project.EndDate.ToString("yyyy-MM-dd"),
            Milestones = milestones.Select(m => new ProjectRiskMilestoneContext
            {
                Title = m.MilestoneTitle,
                DueDate = m.DueDate.ToString("yyyy-MM-dd"),
                Status = m.MilestoneStatus,
                StoryPoints = m.StoryPoints,
                IsOverdue = m.MilestoneStatus != MilestoneStatusConstants.Done && m.DueDate < today
            }).ToList(),
            Allocations = allocations.Select(a => new ProjectRiskAllocationContext
            {
                EmployeeName = a.EmployeeName,
                AllocationPercentage = a.AllocationPercentage,
                StartDate = a.AllocationStartDate.ToString("yyyy-MM-dd"),
                EndDate = a.AllocationEndDate.ToString("yyyy-MM-dd")
            }).ToList(),
            RecentLoggedHours = timesheetHours.Select(t => new ProjectRiskTimesheetHoursContext
            {
                EmployeeName = t.EmployeeName,
                HoursLogged = t.HoursLogged,
                WorkDate = t.WorkDate?.ToString("yyyy-MM-dd")
            }).ToList()
        };
    }
}
