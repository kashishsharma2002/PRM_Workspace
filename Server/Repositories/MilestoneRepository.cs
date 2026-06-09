using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class MilestoneRepository(PrmDbContext context) : IMilestoneRepository
{
    public async Task<IReadOnlyList<ProjectMilestone>> GetByProjectIdAsync(long projectId, CancellationToken cancellationToken = default) =>
        await context.ProjectMilestones
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);

    public Task<ProjectMilestone?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.ProjectMilestones.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<short> GetNextSortOrderAsync(long projectId, CancellationToken cancellationToken = default)
    {
        var max = await context.ProjectMilestones
            .Where(m => m.ProjectId == projectId)
            .Select(m => (short?)m.SortOrder)
            .MaxAsync(cancellationToken);
        return (short)((max ?? 0) + 1);
    }

    public async Task AddAsync(ProjectMilestone milestone, CancellationToken cancellationToken = default)
    {
        await context.ProjectMilestones.AddAsync(milestone, cancellationToken);
    }

    public Task UpdateAsync(ProjectMilestone milestone, CancellationToken cancellationToken = default)
    {
        context.ProjectMilestones.Update(milestone);
        return Task.CompletedTask;
    }
}
