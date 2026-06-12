using Server.Models.Entities;

namespace Server.Repositories.Timesheets;

public interface IActivityTagRepository
{
    Task<IReadOnlyList<ActivityTag>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<ActivityTag?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityTag>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);
}
