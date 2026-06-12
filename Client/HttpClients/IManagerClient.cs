using Client.Models.Allocations;

namespace Client.HttpClients;

public interface IManagerClient
{
    Task<ManagerProjectListResponse?> GetMyProjectsAsync();
    Task<ManagerProjectDetail?> GetProjectDetailAsync(long projectId);
    Task<TeamTimesheetListResponse?> GetTeamTimesheetsAsync(DateOnly? weekStart);
    Task<ManagerTimesheetDetail?> GetTimesheetDetailAsync(long timesheetId);
    Task<TeamDashboard?> GetTeamDashboardAsync();
    Task<TeamMemberDetail?> GetTeamMemberDetailAsync(long employeeId);
    Task<CreateAllocationResponse?> CreateAllocationAsync(CreateAllocationRequest request);
    Task<EndAllocationResponse?> EndAllocationAsync(long allocationId);
}
