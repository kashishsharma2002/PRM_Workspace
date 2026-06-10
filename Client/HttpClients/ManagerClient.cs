namespace Client.HttpClients;

public class ManagerClient(RestClient restClient) : IManagerClient
{
    public Task<ManagerProjectListResponse?> GetMyProjectsAsync() =>
        restClient.GetAsync<ManagerProjectListResponse>("/api/projects/my", requireAuth: true);

    public Task<ManagerProjectDetail?> GetProjectDetailAsync(long projectId) =>
        restClient.GetAsync<ManagerProjectDetail>($"/api/projects/{projectId}/manager", requireAuth: true);

    public Task<TeamTimesheetListResponse?> GetTeamTimesheetsAsync(DateOnly? weekStart)
    {
        var endpoint = weekStart is null
            ? "/api/timesheets/team"
            : $"/api/timesheets/team?weekStart={weekStart:yyyy-MM-dd}";
        return restClient.GetAsync<TeamTimesheetListResponse>(endpoint, requireAuth: true);
    }

    public Task<ManagerTimesheetDetail?> GetTimesheetDetailAsync(long timesheetId) =>
        restClient.GetAsync<ManagerTimesheetDetail>($"/api/timesheets/team/{timesheetId}", requireAuth: true);
}
