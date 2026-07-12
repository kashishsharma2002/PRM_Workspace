using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Common.Audit;
using Server.Common.Errors;
using Server.Common.Projects;
using Server.Common.Roles;
using Server.Repositories.Roles;
using Server.Exceptions;
using Server.Models.DTOs.Projects;
using Server.Models.Entities;
using Server.Services.Shared;
using Server.Services.SystemConfig;
using Server.Services.Projects.Abstractions;

namespace Server.Services.Projects;

public partial class ProjectService(
    IProjectRepository projectRepository,
    IMilestoneRepository milestoneRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IAllocationRepository allocationRepository,
    IEmployeeRepository employeeRepository,
    ITimesheetRepository timesheetRepository,
    ISystemConfigService systemConfigService,
    IProjectHealthFlagEvaluator projectHealthFlagEvaluator,
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
                HealthStatus = project.HealthStatus,
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

    public async Task ArchiveProjectAsync(
        long actorUserId,
        long projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await GetProjectOrThrowAsync(projectId, cancellationToken);
        var now = DateTime.UtcNow;
        var oldStatus = project.ProjectStatus;

        project.ProjectStatus = ProjectStatusConstants.Completed;
        project.IsActive = false;
        project.UpdatedAt = now;

        await projectRepository.UpdateAsync(project, cancellationToken);

        await auditService.LogUpdateAsync(
            actorUserId,
            AuditEntityConstants.Projects,
            project.Id,
            new { projectStatus = oldStatus, isActive = true },
            new { projectStatus = project.ProjectStatus, isActive = project.IsActive },
            cancellationToken);

        await projectRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Project archived. {EntityName} {EntityId} by {ActorUserId}",
            AuditEntityConstants.Projects, project.Id, actorUserId);
    }

    private static int SumDoneStoryPoints(IReadOnlyList<ProjectMilestone> milestones) =>
        milestones.Where(m => m.MilestoneStatus == MilestoneStatusConstants.Done).Sum(m => m.StoryPoints);

    private async Task ValidateManagerAsync(long managerUserId, CancellationToken cancellationToken)
    {
        var manager = await userRepository.GetByIdAsync(managerUserId, cancellationToken)
            ?? throw new ValidationAppException("Manager user not found.");

        if (!manager.IsActive
            || !await roleRepository.UserHasRoleAsync(managerUserId, RoleConstants.Manager, cancellationToken))
            throw new ValidationAppException("Specified user is not an active manager.", errorCode: ErrorCodes.InvalidManager);
    }

    private async Task<Project> GetProjectOrThrowAsync(long projectId, CancellationToken cancellationToken)
    {
        return await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundAppException("Project not found.", ErrorCodes.ProjectNotFound);
    }
}
