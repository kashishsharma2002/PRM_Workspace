namespace Server.Services.Timesheets;

public interface ISchedulerTimesheetService
{
    Task<int> MarkMissedTimesheetsAsync(CancellationToken cancellationToken = default);
}
