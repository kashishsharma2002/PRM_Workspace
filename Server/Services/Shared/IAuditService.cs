namespace Server.Services.Shared;

public interface IAuditService
{
    Task LogCreateAsync(long actorUserId, string entityName, long entityId, object newValues,
        CancellationToken cancellationToken = default);

    Task LogUpdateAsync(long actorUserId, string entityName, long entityId, object? oldValues, object newValues,
        CancellationToken cancellationToken = default);

    Task LogDeactivateAsync(long actorUserId, string entityName, long entityId, object oldValues, object newValues,
        CancellationToken cancellationToken = default);

    Task LogEndAsync(long actorUserId, string entityName, long entityId, object oldValues, object newValues,
        CancellationToken cancellationToken = default);
}
