using Server.Common.Projects;
using Server.Exceptions;
using Server.Models.DTOs.Projects;
using Server.Models.Entities;

namespace Server.Services.Projects;

public partial class ProjectService
{
    public async Task<MilestoneListResponseDto> GetMilestonesAsync(long projectId, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectOrThrowAsync(projectId, cancellationToken);
        var milestones = await milestoneRepository.GetByProjectIdAsync(projectId, cancellationToken);
        var completed = SumDoneStoryPoints(milestones);

        return new MilestoneListResponseDto
        {
            ProjectName = project.ProjectName,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
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

        var status = request.MilestoneStatus.Trim().ToUpperInvariant();
        milestone.MilestoneStatus = status;
        milestone.UpdatedAt = DateTime.UtcNow;
        milestone.CompletedAt = status == MilestoneStatusConstants.Done
            ? DateTime.UtcNow
            : null;

        await milestoneRepository.UpdateAsync(milestone, cancellationToken);
        await projectRepository.SaveChangesAsync(cancellationToken);
    }
}
