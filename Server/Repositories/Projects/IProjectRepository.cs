using Server.Models.Entities;

namespace Server.Repositories.Projects;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetByManagerUserIdAsync(long managerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task UpdateHealthStatusAsync(long projectId, string healthStatus, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string projectCode, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
