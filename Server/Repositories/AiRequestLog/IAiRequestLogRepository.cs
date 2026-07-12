using Server.Models.Entities;

namespace Server.Repositories.AiRequestLog;

public interface IAiRequestLogRepository
{
    Task AddAsync(Models.Entities.AiRequestLog log, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
