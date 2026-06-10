using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Common.Audit;
using Server.Common.Errors;
using Server.Common.Projects;
using Server.Common.Roles;
using Server.Common.Timesheets;
using Server.Exceptions;
using Server.Models.DTOs.Projects;
using Server.Models.DTOs.Scheduler;
using Server.Models.Entities;
using Server.Scheduler;
using Server.Services.Shared;

namespace Server.Services.Projects;

public class ProjectService(
    IProjectRepository projectRepository,
    IMilestoneRepository milestoneRepository,
    IUserRepository userRepository,
    IAllocationRepository allocationRepository,
    IEmployeeRepository employeeRepository,
    ITimesheetRepository timesheetRepository,
    ISystemConfigRepository systemConfigRepository,
    IAuditService auditService,
    ILogger<ProjectService> logger) : IProjectService
{
    public async Task<CreateProjectResponseDto> CreateProjectAsync(
        long actorUserId,
        CreateProjectRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.EndDate < request.StartDate)
            throw new ValidationAppException("End date must be on or after start date.");

        await ValidateManagerAsync(request.ManagerUserId, cancellationToken);

        var now = DateTime.UtcNow;
        var status = request.ProjectStatus.Trim().ToUpperInvariant();

        var project = new Project
        {
            ProjectCode = "TEMP",
            ProjectName = request.ProjectName.Trim(),
            Description = request.Description?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ProjectStatus = status,
            HealthStatus = HealthStatusConstants.Green,
            TotalStoryPoints = request.TotalStoryPoints,
            ManagerUserId = request.ManagerUserId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await projectRepository.AddAsync(project, cancellationToken);
        await projectRepository.SaveChangesAsync(cancellationToken);

        project.ProjectCode = $"PRJ-{project.Id:D6}";
        await projectRepository.UpdateAsync(project, cancellationToken);

        await auditService.LogCreateAsync(
            actorUserId,
            AuditEntityConstants.Projects,
            project.Id,
            new { project.ProjectCode, project.ProjectName, project.ManagerUserId },
            cancellationToken);

        await projectRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Project created. {EntityName} {EntityId} by {ActorUserId}",
            AuditEntityConstants.Projects, project.Id, actorUserId);

        return new CreateProjectResponseDto
        {
            ProjectId = project.Id,
            ProjectCode = project.ProjectCode
        };
    }

    public async Task<ProjectListResponseDto> GetAllProjectsAsync(CancellationToken cancellationToken = default)
    {
        var projects = await projectRepository.GetAllAsync(cancellationToken);
        var managerIds = projects.Select(p => p.ManagerUserId).Distinct();
        var managers = await userRepository.GetByIdsAsync(managerIds, cancellationToken);

        var items = new List<ProjectListItemDto>();
        foreach (var project in projects)
        {
            var milestones = await milestoneRepository.GetByProjectIdAsync(project.Id, cancellationToken);
            items.Add(new ProjectListItemDto
            {
                Id = project.Id,
                ProjectName = project.ProjectName,
                ManagerName = managers.TryGetValue(project.ManagerUserId, out var manager) ? manager.FullName : string.Empty,
                EndDate = project.EndDate,
                ProjectStatus = project.ProjectStatus,
                StoryPointsDone = SumDoneStoryPoints(milestones),
                TotalStoryPoints = project.TotalStoryPoints
            });
        }

        return new ProjectListResponseDto { Projects = items };
    }

    public async Task<ProjectDetailDto> GetProjectByIdAsync(long projectId, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectOrThrowAsync(projectId, cancellationToken);
        var manager = await userRepository.GetByIdAsync(project.ManagerUserId, cancellationToken);
        var milestones = await milestoneRepository.GetByProjectIdAsync(projectId, cancellationToken);

        return new ProjectDetailDto
        {
            Id = project.Id,
            ProjectCode = project.ProjectCode,
            ProjectName = project.ProjectName,
            Description = project.Description,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            ProjectStatus = project.ProjectStatus,
            HealthStatus = project.HealthStatus,
            TotalStoryPoints = project.TotalStoryPoints,
            StoryPointsDone = SumDoneStoryPoints(milestones),
            ManagerUserId = project.ManagerUserId,
            ManagerName = manager?.FullName ?? string.Empty
        };
    }

    public async Task UpdateProjectAsync(
        long projectId,
        UpdateProjectRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var project = await GetProjectOrThrowAsync(projectId, cancellationToken);

        if (request.EndDate < request.StartDate)
            throw new ValidationAppException("End date must be on or after start date.");

        await ValidateManagerAsync(request.ManagerUserId, cancellationToken);

        project.ProjectName = request.ProjectName.Trim();
        project.Description = request.Description?.Trim();
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.ProjectStatus = request.ProjectStatus.Trim().ToUpperInvariant();
        project.ManagerUserId = request.ManagerUserId;
        project.TotalStoryPoints = request.TotalStoryPoints;
        project.UpdatedAt = DateTime.UtcNow;

        await projectRepository.UpdateAsync(project, cancellationToken);
        await projectRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<MilestoneListResponseDto> GetMilestonesAsync(long projectId, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectOrThrowAsync(projectId, cancellationToken);
        var milestones = await milestoneRepository.GetByProjectIdAsync(projectId, cancellationToken);
        var completed = SumDoneStoryPoints(milestones);

        return new MilestoneListResponseDto
        {
            ProjectName = project.ProjectName,
            Milestones = milestones.Select(m => new MilestoneListItemDto
            {
                Id = m.Id,
                MilestoneTitle = m.MilestoneTitle,
                DueDate = m.DueDate,
                StoryPoints = m.StoryPoints,
                MilestoneStatus = m.MilestoneStatus,
                SortOrder = m.SortOrder
            }).ToList(),
            TotalStoryPoints = project.TotalStoryPoints,
            CompletedStoryPoints = completed,
            RemainingStoryPoints = Math.Max(0, project.TotalStoryPoints - completed)
        };
    }

    public async Task AddMilestoneAsync(
        long projectId,
        CreateMilestoneRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var project = await GetProjectOrThrowAsync(projectId, cancellationToken);

        if (request.DueDate < project.StartDate || request.DueDate > project.EndDate)
            throw new ValidationAppException("Due date must be within the project start and end dates.");

        var now = DateTime.UtcNow;
        var sortOrder = request.SortOrder ?? await milestoneRepository.GetNextSortOrderAsync(projectId, cancellationToken);

        await milestoneRepository.AddAsync(new ProjectMilestone
        {
            ProjectId = projectId,
            MilestoneTitle = request.MilestoneTitle.Trim(),
            DueDate = request.DueDate,
            StoryPoints = request.StoryPoints,
            MilestoneStatus = MilestoneStatusConstants.NotStarted,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);

        await projectRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMilestoneStatusAsync(
        long projectId,
        long milestoneId,
        UpdateMilestoneStatusRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await GetProjectOrThrowAsync(projectId, cancellationToken);

        var milestone = await milestoneRepository.GetByIdAsync(milestoneId, cancellationToken)
            ?? throw new NotFoundAppException("Milestone not found.");

        if (milestone.ProjectId != projectId)
            throw new NotFoundAppException("Milestone not found for this project.");

        milestone.MilestoneStatus = request.MilestoneStatus.Trim().ToUpperInvariant();
        milestone.UpdatedAt = DateTime.UtcNow;

        await milestoneRepository.UpdateAsync(milestone, cancellationToken);
        await projectRepository.SaveChangesAsync(cancellationToken);
    }

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
                EndDate = p.EndDate,
                HealthStatus = p.HealthStatus
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
            IsOverdue = m.DueDate < today && m.MilestoneStatus != "DONE"
        }).ToList();

        var allocations = await allocationRepository.GetActiveByProjectIdAsync(projectId, cancellationToken);
        var resources = new List<ManagerProjectResourceDto>();
        foreach (var allocation in allocations)
        {
            var employee = await employeeRepository.GetByIdAsync(allocation.EmployeeId, cancellationToken);
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

        var riskFlags = await BuildRiskFlagsAsync(
            projectId, project.EndDate, milestoneDtos, allocations, cancellationToken);

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

        var maxWeeklyHours = await GetMaxWeeklyHoursAsync(cancellationToken);
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

                var flags = HealthFlagEvaluator.EvaluateFlags(
                    project.EndDate, projectMilestones, expectedHours, loggedHours, today);
                var healthStatus = HealthFlagEvaluator.MapToHealthStatus(flags);

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

    private async Task<List<string>> BuildRiskFlagsAsync(
        long projectId,
        DateOnly endDate,
        IReadOnlyList<ManagerProjectMilestoneDto> milestones,
        IReadOnlyList<ProjectAllocation> allocations,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var flags = new List<string>();

        if (milestones.Any(m => m.IsOverdue))
            flags.Add(ProjectConstants.FlagOverdueMilestone);

        var daysUntilEnd = endDate.DayNumber - today.DayNumber;
        if (daysUntilEnd < ProjectConstants.ApproachingDeadlineDays
            && milestones.Any(m => m.MilestoneStatus != "DONE"))
        {
            flags.Add(ProjectConstants.FlagApproachingDeadline);
        }

        var lastWeek = WeekDateHelper.GetMostRecentCompletedWeekMonday();
        var maxWeeklyHours = await GetMaxWeeklyHoursAsync(cancellationToken);
        var employeeIds = allocations.Select(a => a.EmployeeId).Distinct().ToList();
        var timesheets = await timesheetRepository.GetByEmployeeIdsAndWeekAsync(employeeIds, lastWeek, cancellationToken);
        var timesheetLookup = timesheets.ToDictionary(t => t.EmployeeId);

        foreach (var allocation in allocations)
        {
            if (!timesheetLookup.TryGetValue(allocation.EmployeeId, out var timesheet)
                || timesheet.Status != TimesheetConstants.StatusSubmitted)
                continue;

            var lineItems = await timesheetRepository.GetLineItemsByTimesheetIdAsync(timesheet.Id, cancellationToken);
            var projectHours = lineItems
                .Where(li => li.ProjectId == projectId)
                .Sum(li => li.HoursLogged);

            var expectedHours = allocation.AllocationPercentage / 100m * maxWeeklyHours;
            if (projectHours < expectedHours * ProjectConstants.LowHoursThreshold)
                flags.Add(ProjectConstants.FlagLowHours);
        }

        return flags.Distinct().ToList();
    }

    private async Task<decimal> GetMaxWeeklyHoursAsync(CancellationToken cancellationToken)
    {
        var config = await systemConfigRepository.GetByKeyAsync(ConfigKeys.MaxWeeklyHours, cancellationToken);
        if (config is null || !decimal.TryParse(config.ConfigValue, out var maxHours))
            return TimesheetDefaults.DefaultMaxWeeklyHours;

        return maxHours;
    }

    private static int SumDoneStoryPoints(IReadOnlyList<ProjectMilestone> milestones) =>
        milestones.Where(m => m.MilestoneStatus == MilestoneStatusConstants.Done).Sum(m => m.StoryPoints);

    private async Task ValidateManagerAsync(long managerUserId, CancellationToken cancellationToken)
    {
        var manager = await userRepository.GetByIdAsync(managerUserId, cancellationToken)
            ?? throw new ValidationAppException("Manager user not found.");

        if (!manager.IsActive || !string.Equals(manager.Role, RoleConstants.Manager, StringComparison.OrdinalIgnoreCase))
            throw new ValidationAppException("Specified user is not an active manager.", errorCode: ErrorCodes.InvalidManager);
    }

    private async Task<Project> GetProjectOrThrowAsync(long projectId, CancellationToken cancellationToken)
    {
        return await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundAppException("Project not found.", ErrorCodes.ProjectNotFound);
    }
}
