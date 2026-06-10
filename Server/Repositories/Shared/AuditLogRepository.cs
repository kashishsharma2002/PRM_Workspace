using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Shared;

public class AuditLogRepository(PrmDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await context.AuditLogs.AddAsync(auditLog, cancellationToken);
    }
}
