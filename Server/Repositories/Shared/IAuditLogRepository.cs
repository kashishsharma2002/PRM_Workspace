using Server.Models.Entities;

namespace Server.Repositories.Shared;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
}
