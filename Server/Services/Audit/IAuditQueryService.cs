using Server.Models.DTOs.Audit;
using Server.Models.Queries;

namespace Server.Services.Audit;

public interface IAuditQueryService
{
    Task<AuditLogListResponseDto> GetAuditLogsAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);
}
