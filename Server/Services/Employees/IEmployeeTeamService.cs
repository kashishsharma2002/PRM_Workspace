using Server.Models.DTOs.Employees;

namespace Server.Services.Employees;

public interface IEmployeeTeamService
{
    Task<TeamDashboardDto> GetTeamDashboardAsync(long managerUserId, CancellationToken cancellationToken = default);
    Task<TeamMemberDetailDto> GetTeamMemberDetailAsync(long managerUserId, long employeeId, CancellationToken cancellationToken = default);
    Task RestoreTimesheetAccessAsync(long managerUserId, long employeeId, CancellationToken cancellationToken = default);
}
