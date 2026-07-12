using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;
using Server.Models.Queries;

namespace Server.Repositories.Shared;

public class AuditLogRepository(PrmDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await context.AuditLogs.AddAsync(auditLog, cancellationToken);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> QueryAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var auditLogs = context.AuditLogs.AsNoTracking();

        if (query.From.HasValue)
            auditLogs = auditLogs.Where(a => a.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            auditLogs = auditLogs.Where(a => a.CreatedAt <= query.To.Value);

        if (query.ActorUserId.HasValue)
            auditLogs = auditLogs.Where(a => a.ActorUserId == query.ActorUserId.Value);

        if (!string.IsNullOrWhiteSpace(query.EntityName))
            auditLogs = auditLogs.Where(a => a.EntityName == query.EntityName.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(query.ActionType))
            auditLogs = auditLogs.Where(a => a.ActionType == query.ActionType.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            auditLogs = auditLogs.Where(a =>
                (a.Summary != null && a.Summary.Contains(term)) ||
                a.EntityName.Contains(term) ||
                a.ActionType.Contains(term));
        }

        var totalCount = await auditLogs.CountAsync(cancellationToken);

        var items = await auditLogs
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
