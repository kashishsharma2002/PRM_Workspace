namespace Client.HttpClients;

public class EmployeeClient(RestClient restClient) : IEmployeeClient
{
    public Task<List<TimesheetHistoryItem>?> GetMyTimesheetsAsync() =>
        restClient.GetAsync<List<TimesheetHistoryItem>>("/api/timesheets/my", requireAuth: true);

    public Task<TimesheetDetail?> GetMyTimesheetDetailAsync(long timesheetId) =>
        restClient.GetAsync<TimesheetDetail>($"/api/timesheets/my/{timesheetId}", requireAuth: true);

    public Task<List<EmployeeWeekAllocation>?> GetWeekAllocationsAsync(DateOnly weekStart) =>
        restClient.GetAsync<List<EmployeeWeekAllocation>>($"/api/timesheets/allocations?weekStart={weekStart:yyyy-MM-dd}", requireAuth: true);

    public Task<List<ActivityTagItem>?> GetActivityTagsAsync() =>
        restClient.GetAsync<List<ActivityTagItem>>("/api/activity-tags", requireAuth: true);

    public Task<TimesheetSubmitResponse?> SubmitTimesheetAsync(TimesheetSubmitRequest request) =>
        restClient.PostAsync<TimesheetSubmitResponse>("/api/timesheets", request, requireAuth: true);

    public Task<EmployeeAllocationListResponse?> GetMyAllocationsAsync() =>
        restClient.GetAsync<EmployeeAllocationListResponse>("/api/allocations/my", requireAuth: true);

    public Task<TimesheetReminderResponse?> GetReminderAsync() =>
        restClient.GetAsync<TimesheetReminderResponse>("/api/timesheets/reminder", requireAuth: true);
}
