using Server.Common;
using Server.Repositories.Scheduler;
using Server.Repositories.SystemConfig;
using Server.Services.Compliance;
using Server.Services.Employees;
using Server.Services.Projects;
using Server.Services.Timesheets;

namespace Server.Scheduler;

public class SchedulerRunner(
    IProjectHealthService projectHealthService,
    ITimesheetComplianceService timesheetComplianceService,
    ISchedulerTimesheetService schedulerTimesheetService,
    IResourceStatusService resourceStatusService,
    ISchedulerJobLogRepository jobLogRepository,
    ILogger<SchedulerRunner> logger) : ISchedulerRunner
{
    private const string JobName = "BackgroundScheduler";

    public async Task RunScheduledJobsAsync(CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;

        try
        {
            var healthResult = await projectHealthService.ProcessProjectHealthNotificationsAsync(cancellationToken);
            var complianceResult = await timesheetComplianceService.ProcessTimesheetComplianceAsync(cancellationToken);
            var missedCreated = await schedulerTimesheetService.MarkMissedTimesheetsAsync(cancellationToken);
            var statusesUpdated = await resourceStatusService.ReconcileAllResourceStatusesAsync(cancellationToken);
            var completedAt = DateTime.UtcNow;

            var emailsSent = healthResult.EmailsSent + complianceResult.EmailsSent;
            var emailsFailed = healthResult.EmailsFailed + complianceResult.EmailsFailed;

            await jobLogRepository.LogAsync(
                JobName,
                SchedulerJobStatusConstants.Success,
                startedAt,
                completedAt,
                cancellationToken: cancellationToken);

            logger.LogInformation(
                "Scheduler completed. Evaluated={Evaluated}, HealthFailed={HealthFailed}, EmailsSent={EmailsSent}, EmailsFailed={EmailsFailed}, RiskAnalysesCompleted={RiskCompleted}, RiskAnalysesFailed={RiskFailed}, MissedCreated={MissedCreated}, StatusesUpdated={StatusesUpdated}, ElapsedMs={ElapsedMs}",
                healthResult.EvaluatedCount,
                healthResult.FailedCount,
                emailsSent,
                emailsFailed,
                healthResult.RiskAnalysesCompleted,
                healthResult.RiskAnalysesFailed,
                missedCreated,
                statusesUpdated,
                (completedAt - startedAt).TotalMilliseconds);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Scheduler run failed");

            try
            {
                await jobLogRepository.LogAsync(
                    JobName,
                    SchedulerJobStatusConstants.Failed,
                    startedAt,
                    DateTime.UtcNow,
                    ex.ToString(),
                    cancellationToken);
            }
            catch (Exception logEx)
            {
                logger.LogError(logEx, "Failed to write scheduler job log after run failure");
            }

            throw;
        }
    }
}
