using System.Text.Json;
using Server.Exceptions;
using Server.Models.DTOs.Projects;
using Server.Models.Entities;
using Server.Repositories.Interfaces;
using Server.Services.Interfaces;

namespace Server.Services;

public class ProjectService(
    IProjectRepository projectRepository,
    IMilestoneRepository milestoneRepository,
    IUserRepository userRepository,
    IAuditLogRepository auditLogRepository) : IProjectService
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
            HealthStatus = "GREEN",
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

        await auditLogRepository.AddAsync(new AuditLog
        {
            ActorUserId = actorUserId,
            EntityName = "PROJECTS",
            EntityId = project.Id,
            ActionType = "CREATE",
            NewValues = JsonSerializer.Serialize(new { project.ProjectCode, project.ProjectName, project.ManagerUserId }),
            CreatedAt = now
        }, cancellationToken);

        await projectRepository.SaveChangesAsync(cancellationToken);

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
            MilestoneStatus = "NOT_STARTED",
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

    private static int SumDoneStoryPoints(IReadOnlyList<ProjectMilestone> milestones) =>
        milestones.Where(m => m.MilestoneStatus == "DONE").Sum(m => m.StoryPoints);

    private async Task ValidateManagerAsync(long managerUserId, CancellationToken cancellationToken)
    {
        var manager = await userRepository.GetByIdAsync(managerUserId, cancellationToken)
            ?? throw new ValidationAppException("Manager user not found.");

        if (!manager.IsActive || !string.Equals(manager.Role, "MANAGER", StringComparison.OrdinalIgnoreCase))
            throw new ValidationAppException("Specified user is not an active manager.");
    }

    private async Task<Project> GetProjectOrThrowAsync(long projectId, CancellationToken cancellationToken)
    {
        return await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundAppException("Project not found.");
    }
}
