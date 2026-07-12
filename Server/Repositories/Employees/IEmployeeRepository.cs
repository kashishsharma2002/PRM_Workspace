using Server.Models.Entities;

namespace Server.Repositories.Employees;

public interface IEmployeeRepository
{
    Task<ResourceProfile?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<ResourceProfile?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<long, ResourceProfile>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResourceProfile>> GetAllAsync(string? resourceStatus, string? department, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResourceProfile>> GetByManagerIdAsync(long managerId, CancellationToken cancellationToken = default);
    Task AddAsync(ResourceProfile resourceProfile, CancellationToken cancellationToken = default);
    Task UpdateAsync(ResourceProfile resourceProfile, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResourceProfile>> GetResourceProfilesByUserIdsAsync(
        IEnumerable<long> userIds,
        CancellationToken cancellationToken = default);
}
