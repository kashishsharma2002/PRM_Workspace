using Server.Models.DTOs.Timesheets;

namespace Server.Services.Timesheets;

public interface IManagerTimesheetService
{
    Task<TeamTimesheetListResponseDto> GetTeamTimesheetsAsync(
        long managerUserId,
        DateOnly? weekStart,
        CancellationToken cancellationToken = default);

    Task<ManagerTimesheetDetailDto> GetTimesheetForManagerAsync(
        long managerUserId,
        long timesheetId,
        CancellationToken cancellationToken = default);
}
