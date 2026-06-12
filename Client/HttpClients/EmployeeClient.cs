using Client.Common;

namespace Client.HttpClients;

public class EmployeeClient(RestClient restClient) : IEmployeeClient
{
    public Task<List<TimesheetHistoryItem>?> GetMyTimesheetsAsync() =>
        restClient.GetAsync<List<TimesheetHistoryItem>>(ApiRoutes.TimesheetsMy, requireAuth: true);

    public Task<TimesheetDetail?> GetMyTimesheetDetailAsync(long timesheetId) =>
        restClient.GetAsync<TimesheetDetail>(ApiRoutes.TimesheetMyById(timesheetId), requireAuth: true);

    public Task<List<EmployeeWeekAllocation>?> GetWeekAllocationsAsync(DateOnly weekStart) =>
        restClient.GetAsync<List<EmployeeWeekAllocation>>(ApiRoutes.TimesheetAllocations(weekStart), requireAuth: true);

    public Task<List<ActivityTagItem>?> GetActivityTagsAsync() =>
        restClient.GetAsync<List<ActivityTagItem>>(ApiRoutes.ActivityTags, requireAuth: true);

    public Task<TimesheetSubmitResponse?> SubmitTimesheetAsync(TimesheetSubmitRequest request) =>
        restClient.PostAsync<TimesheetSubmitResponse>(ApiRoutes.Timesheets, request, requireAuth: true);

    public Task<EmployeeAllocationListResponse?> GetMyAllocationsAsync() =>
        restClient.GetAsync<EmployeeAllocationListResponse>(ApiRoutes.AllocationsMy, requireAuth: true);

    public Task<TimesheetReminderResponse?> GetReminderAsync() =>
        restClient.GetAsync<TimesheetReminderResponse>(ApiRoutes.TimesheetsReminder, requireAuth: true);
}
