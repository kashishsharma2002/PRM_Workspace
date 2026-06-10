using Server.Common;

namespace Server.Scheduler;

public class BackgroundScheduler(IServiceProvider serviceProvider, ILogger<BackgroundScheduler> logger) : BackgroundService
{
    private const string JobName = "BackgroundScheduler";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var startedAt = DateTime.UtcNow;

            try
            {
                using var scope = serviceProvider.CreateScope();
                var configRepo = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
                var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
                var timesheetService = scope.ServiceProvider.GetRequiredService<ITimesheetService>();
                var jobLogRepo = scope.ServiceProvider.GetRequiredService<ISchedulerJobLogRepository>();

                var healthResult = await projectService.EvaluateAllProjectsHealthAsync(stoppingToken);
                var missedCreated = await timesheetService.MarkMissedTimesheetsAsync(stoppingToken);
                var completedAt = DateTime.UtcNow;

                await jobLogRepo.LogAsync(JobName, "SUCCESS", startedAt, completedAt, cancellationToken: stoppingToken);

                logger.LogInformation(
                    "Scheduler completed. Evaluated={Evaluated}, HealthFailed={HealthFailed}, MissedCreated={MissedCreated}, ElapsedMs={ElapsedMs}",
                    healthResult.EvaluatedCount,
                    healthResult.FailedCount,
                    missedCreated,
                    (completedAt - startedAt).TotalMilliseconds);

                var intervalHours = await GetSchedulerIntervalHoursAsync(configRepo, stoppingToken);
                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Scheduler run failed");

                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var jobLogRepo = scope.ServiceProvider.GetRequiredService<ISchedulerJobLogRepository>();
                    await jobLogRepo.LogAsync(
                        JobName,
                        "FAILED",
                        startedAt,
                        DateTime.UtcNow,
                        ex.ToString(),
                        stoppingToken);
                }
                catch (Exception logEx)
                {
                    logger.LogError(logEx, "Failed to write scheduler job log after run failure");
                }

                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var configRepo = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
                    var intervalHours = await GetSchedulerIntervalHoursAsync(configRepo, stoppingToken);
                    await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private static async Task<int> GetSchedulerIntervalHoursAsync(
        ISystemConfigRepository configRepo,
        CancellationToken cancellationToken)
    {
        var config = await configRepo.GetByKeyAsync(ConfigKeys.SchedulerIntervalHours, cancellationToken);
        if (config is null || !int.TryParse(config.ConfigValue, out var interval) || interval <= 0)
            return 4;

        return interval;
    }
}
