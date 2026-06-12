namespace Server.Repositories.Scheduler;

public interface ISchedulerJobLogRepository
{
    Task LogAsync(
        string jobName,
        string status,
        DateTime startedAt,
        DateTime completedAt,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}
