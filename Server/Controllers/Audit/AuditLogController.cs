using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Common.Roles;
using Server.Models.DTOs.Audit;
using Server.Models.Queries;
using Server.Services.Audit;

namespace Server.Controllers.Audit;

[Authorize(Roles = RoleConstants.Admin)]
[ApiController]
[Route("api/audit-logs")]
public class AuditLogController(IAuditQueryService auditQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AuditLogListResponseDto>>> GetAuditLogs(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] long? actorUserId,
        [FromQuery] string? entityName,
        [FromQuery] string? actionType,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new AuditLogQuery
        {
            From = from,
            To = to,
            ActorUserId = actorUserId,
            EntityName = entityName,
            ActionType = actionType,
            Search = search,
            Page = page,
            PageSize = pageSize
        };

        var result = await auditQueryService.GetAuditLogsAsync(query, cancellationToken);
        return Ok(ApiResponse<AuditLogListResponseDto>.Ok(result, "Activity log retrieved."));
    }
}
