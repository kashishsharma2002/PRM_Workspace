using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Ai;

public class AiRequestLogRepository(PrmDbContext context) : IAiRequestLogRepository
{
    public async Task AddAsync(AiRequestLog log, CancellationToken cancellationToken = default) =>
        await context.AiRequestLogs.AddAsync(log, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
