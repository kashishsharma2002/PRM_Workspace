using Server.Models.DTOs.Timesheets;

namespace Server.Services.Timesheets;

public interface IEmployeeTimesheetService
{
    Task<TimesheetSubmitResponseDto> SubmitTimesheetAsync(
        long employeeId,
        long actorUserId,
        TimesheetSubmitRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TimesheetHistoryItemDto>> GetMyTimesheetsAsync(
        long employeeId,
        CancellationToken cancellationToken = default);

    Task<TimesheetDetailDto> GetTimesheetDetailAsync(
        long employeeId,
        long timesheetId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeWeekAllocationDto>> GetWeekAllocationsAsync(
        long employeeId,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityTagDto>> GetActivityTagsAsync(CancellationToken cancellationToken = default);

    Task<TimesheetReminderResponseDto> GetTimesheetReminderAsync(
        long employeeId,
        CancellationToken cancellationToken = default);
}
