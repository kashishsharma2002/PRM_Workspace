using Server.Common;
using Server.Services.Employees;
using Server.Services.Projects;
using Server.Services.SystemConfig;
using Server.Services.Timesheets;

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
                var resourceStatusService = scope.ServiceProvider.GetRequiredService<IResourceStatusService>();
                var jobLogRepo = scope.ServiceProvider.GetRequiredService<ISchedulerJobLogRepository>();

                var healthResult = await projectService.EvaluateAllProjectsHealthAsync(stoppingToken);
                var missedCreated = await timesheetService.MarkMissedTimesheetsAsync(stoppingToken);
                var statusesUpdated = await resourceStatusService.ReconcileAllResourceStatusesAsync(stoppingToken);
                var completedAt = DateTime.UtcNow;

                await jobLogRepo.LogAsync(
                    JobName,
                    SchedulerJobStatusConstants.Success,
                    startedAt,
                    completedAt,
                    cancellationToken: stoppingToken);

                logger.LogInformation(
                    "Scheduler completed. Evaluated={Evaluated}, HealthFailed={HealthFailed}, MissedCreated={MissedCreated}, StatusesUpdated={StatusesUpdated}, ElapsedMs={ElapsedMs}",
                    healthResult.EvaluatedCount,
                    healthResult.FailedCount,
                    missedCreated,
                    statusesUpdated,
                    (completedAt - startedAt).TotalMilliseconds);

                var intervalHours = await GetSchedulerIntervalHoursAsync(configRepo, stoppingToken);
                logger.LogInformation(
                    "Scheduler next run in {IntervalHours} hour(s) ({ConfigKey} from system config).",
                    intervalHours,
                    ConfigKeys.SchedulerIntervalHours);
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
                        SchedulerJobStatusConstants.Failed,
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
            return SchedulerDefaults.IntervalHours;

        return interval;
    }
}
