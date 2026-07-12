namespace Server.Scheduler;

public interface ISchedulerRunner
{
    Task RunScheduledJobsAsync(CancellationToken cancellationToken = default);
}
