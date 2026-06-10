using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Scheduler;

public class SchedulerJobLogRepository(PrmDbContext context) : ISchedulerJobLogRepository
{
    public async Task LogAsync(
        string jobName,
        string status,
        DateTime startedAt,
        DateTime completedAt,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        await context.SchedulerJobLogs.AddAsync(new SchedulerJobLog
        {
            JobName = jobName,
            Status = status,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            ErrorMessage = errorMessage
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}
