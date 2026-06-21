namespace Server.Services.Shared;

public interface IAuditService
{
    Task LogCreateAsync(long actorUserId, string entityName, long entityId, object newValues,
        CancellationToken cancellationToken = default, string? summary = null);

    Task LogUpdateAsync(long actorUserId, string entityName, long entityId, object? oldValues, object newValues,
        CancellationToken cancellationToken = default, string? summary = null);

    Task LogDeactivateAsync(long actorUserId, string entityName, long entityId, object oldValues, object newValues,
        CancellationToken cancellationToken = default, string? summary = null);

    Task LogEndAsync(long actorUserId, string entityName, long entityId, object oldValues, object newValues,
        CancellationToken cancellationToken = default, string? summary = null);

    Task LogAuthEventAsync(long actorUserId, string entityName, long entityId, string actionType, object newValues,
        CancellationToken cancellationToken = default, string? summary = null);
}
