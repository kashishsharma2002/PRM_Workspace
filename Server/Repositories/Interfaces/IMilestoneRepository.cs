using Server.Models.Entities;

namespace Server.Repositories.Interfaces;

public interface IMilestoneRepository
{
    Task<IReadOnlyList<ProjectMilestone>> GetByProjectIdAsync(long projectId, CancellationToken cancellationToken = default);
    Task<ProjectMilestone?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<short> GetNextSortOrderAsync(long projectId, CancellationToken cancellationToken = default);
    Task AddAsync(ProjectMilestone milestone, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProjectMilestone milestone, CancellationToken cancellationToken = default);
}
