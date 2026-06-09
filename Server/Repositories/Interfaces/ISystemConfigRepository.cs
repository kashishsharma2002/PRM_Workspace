using Server.Models.Entities;

namespace Server.Repositories.Interfaces;

public interface ISystemConfigRepository
{
    Task<IReadOnlyList<SystemConfiguration>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SystemConfiguration?> GetByKeyAsync(string configKey, CancellationToken cancellationToken = default);
    Task UpdateAsync(SystemConfiguration configuration, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
