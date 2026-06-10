namespace Client.HttpClients;

public interface IManagerClient
{
    Task<ManagerProjectListResponse?> GetMyProjectsAsync();
    Task<ManagerProjectDetail?> GetProjectDetailAsync(long projectId);
    Task<TeamTimesheetListResponse?> GetTeamTimesheetsAsync(DateOnly? weekStart);
    Task<ManagerTimesheetDetail?> GetTimesheetDetailAsync(long timesheetId);
}
