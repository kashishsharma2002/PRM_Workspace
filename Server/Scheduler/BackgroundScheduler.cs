using Server.Common;
using Server.Repositories.SystemConfig;

namespace Server.Scheduler;

public class BackgroundScheduler(IServiceProvider serviceProvider, ILogger<BackgroundScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<ISchedulerRunner>();
                var configRepo = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();

                await runner.RunScheduledJobsAsync(stoppingToken);

                var intervalHours = await GetSchedulerIntervalHoursAsync(configRepo, stoppingToken);
                logger.LogInformation(
                    "Scheduler next run in {IntervalHours} hour(s) ({ConfigKey} from system config).",
                    intervalHours,
                    ConfigKeys.SchedulerIntervalHours);
                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
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
