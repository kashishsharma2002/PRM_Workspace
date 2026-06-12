using Client.Common;
using Client.Models.Allocations;

namespace Client.HttpClients;

public class ManagerClient(RestClient restClient) : IManagerClient
{
    public Task<ManagerProjectListResponse?> GetMyProjectsAsync() =>
        restClient.GetAsync<ManagerProjectListResponse>(ApiRoutes.ProjectsMy, requireAuth: true);

    public Task<ManagerProjectDetail?> GetProjectDetailAsync(long projectId) =>
        restClient.GetAsync<ManagerProjectDetail>(ApiRoutes.ProjectManager(projectId), requireAuth: true);

    public Task<TeamTimesheetListResponse?> GetTeamTimesheetsAsync(DateOnly? weekStart)
    {
        var endpoint = weekStart is null
            ? ApiRoutes.TimesheetsTeam
            : ApiRoutes.TimesheetsTeamWithWeek(weekStart.Value);
        return restClient.GetAsync<TeamTimesheetListResponse>(endpoint, requireAuth: true);
    }

    public Task<ManagerTimesheetDetail?> GetTimesheetDetailAsync(long timesheetId) =>
        restClient.GetAsync<ManagerTimesheetDetail>(ApiRoutes.TimesheetTeamById(timesheetId), requireAuth: true);

    public Task<TeamDashboard?> GetTeamDashboardAsync() =>
        restClient.GetAsync<TeamDashboard>(ApiRoutes.EmployeesMyTeam, requireAuth: true);

    public Task<TeamMemberDetail?> GetTeamMemberDetailAsync(long employeeId) =>
        restClient.GetAsync<TeamMemberDetail>(ApiRoutes.EmployeeTeamMember(employeeId), requireAuth: true);

    public Task<CreateAllocationResponse?> CreateAllocationAsync(CreateAllocationRequest request) =>
        restClient.PostAsync<CreateAllocationResponse>(ApiRoutes.Allocations, request, requireAuth: true);

    public Task<EndAllocationResponse?> EndAllocationAsync(long allocationId) =>
        restClient.PutAsync<EndAllocationResponse>(ApiRoutes.AllocationEnd(allocationId), new { }, requireAuth: true);
}
