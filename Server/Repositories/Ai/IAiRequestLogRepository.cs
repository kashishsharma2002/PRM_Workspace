using Server.Models.Entities;

namespace Server.Repositories.Ai;

public interface IAiRequestLogRepository
{
    Task AddAsync(AiRequestLog log, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
