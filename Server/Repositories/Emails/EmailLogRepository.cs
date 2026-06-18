using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Emails;

public class EmailLogRepository(PrmDbContext context) : IEmailLogRepository
{
    public Task<bool> WasSentTodayAsync(
        string recipient,
        string emailType,
        string? entityReference,
        DateTime utcToday,
        CancellationToken cancellationToken = default) =>
        context.EmailLogs.AnyAsync(
            l => l.Recipient == recipient
                && l.EmailType == emailType
                && l.EntityReference == entityReference
                && l.SentTime >= utcToday,
            cancellationToken);

    public Task<bool> WasSentForReferenceAsync(
        string recipient,
        string emailType,
        string? entityReference,
        CancellationToken cancellationToken = default) =>
        context.EmailLogs.AnyAsync(
            l => l.Recipient == recipient
                && l.EmailType == emailType
                && l.EntityReference == entityReference,
            cancellationToken);

    public async Task AddAsync(EmailLog emailLog, CancellationToken cancellationToken = default) =>
        await context.EmailLogs.AddAsync(emailLog, cancellationToken);

    public async Task<IReadOnlyList<EmailLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
        await context.EmailLogs
            .OrderByDescending(l => l.SentTime)
            .Take(count)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
