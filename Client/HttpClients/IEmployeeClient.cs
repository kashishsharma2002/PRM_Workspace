namespace Client.HttpClients;

public interface IEmployeeClient
{
    Task<List<TimesheetHistoryItem>?> GetMyTimesheetsAsync();
    Task<TimesheetDetail?> GetMyTimesheetDetailAsync(long timesheetId);
    Task<List<EmployeeWeekAllocation>?> GetWeekAllocationsAsync(DateOnly weekStart);
    Task<List<ActivityTagItem>?> GetActivityTagsAsync();
    Task<TimesheetSubmitResponse?> SubmitTimesheetAsync(TimesheetSubmitRequest request);
    Task<EmployeeAllocationListResponse?> GetMyAllocationsAsync();
    Task<TimesheetReminderResponse?> GetReminderAsync();
}
