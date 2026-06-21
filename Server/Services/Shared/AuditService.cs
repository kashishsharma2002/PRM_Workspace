using System.Text.Json;
using Server.Common.Audit;
using Server.Models.Entities;

namespace Server.Services.Shared;

public class AuditService(IAuditLogRepository auditLogRepository) : IAuditService
{
    public Task LogCreateAsync(long actorUserId, string entityName, long entityId, object newValues,
        CancellationToken cancellationToken = default, string? summary = null) =>
        AddAsync(actorUserId, entityName, entityId, AuditActionConstants.Create, null, newValues, summary, cancellationToken);

    public Task LogUpdateAsync(long actorUserId, string entityName, long entityId, object? oldValues, object newValues,
        CancellationToken cancellationToken = default, string? summary = null) =>
        AddAsync(actorUserId, entityName, entityId, AuditActionConstants.Update, oldValues, newValues, summary, cancellationToken);

    public Task LogDeactivateAsync(long actorUserId, string entityName, long entityId, object oldValues, object newValues,
        CancellationToken cancellationToken = default, string? summary = null) =>
        AddAsync(actorUserId, entityName, entityId, AuditActionConstants.Deactivate, oldValues, newValues, summary, cancellationToken);

    public Task LogEndAsync(long actorUserId, string entityName, long entityId, object oldValues, object newValues,
        CancellationToken cancellationToken = default, string? summary = null) =>
        AddAsync(actorUserId, entityName, entityId, AuditActionConstants.End, oldValues, newValues, summary, cancellationToken);

    public Task LogAuthEventAsync(long actorUserId, string entityName, long entityId, string actionType, object newValues,
        CancellationToken cancellationToken = default, string? summary = null) =>
        AddAsync(actorUserId, entityName, entityId, actionType, null, newValues, summary, cancellationToken);

    private async Task AddAsync(
        long actorUserId,
        string entityName,
        long entityId,
        string actionType,
        object? oldValues,
        object newValues,
        string? summary,
        CancellationToken cancellationToken)
    {
        await auditLogRepository.AddAsync(new AuditLog
        {
            ActorUserId = actorUserId,
            EntityName = entityName,
            EntityId = entityId,
            ActionType = actionType,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
            NewValues = JsonSerializer.Serialize(newValues),
            Summary = summary,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }
}
