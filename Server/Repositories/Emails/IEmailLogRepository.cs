using Server.Models.Entities;

namespace Server.Repositories.Emails;

public interface IEmailLogRepository
{
    Task<bool> WasSentTodayAsync(
        string recipient,
        string emailType,
        string? entityReference,
        DateTime utcToday,
        CancellationToken cancellationToken = default);

    Task<bool> WasSentForReferenceAsync(
        string recipient,
        string emailType,
        string? entityReference,
        CancellationToken cancellationToken = default);

    Task AddAsync(EmailLog emailLog, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailLog>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
