using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Common.Roles;
using Server.Models.DTOs.Timesheets;

namespace Server.Controllers.Timesheets;

[ApiController]
[Route("api/timesheets")]
public class TimesheetController(ITimesheetService timesheetService) : ControllerBase
{
    [Authorize(Roles = RoleConstants.Employee)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<TimesheetSubmitResponseDto>>> SubmitTimesheet(
        [FromBody] TimesheetSubmitRequestDto request,
        CancellationToken cancellationToken)
    {
        var employeeId = GetEmployeeId();
        var actorUserId = GetActorUserId();
        var result = await timesheetService.SubmitTimesheetAsync(employeeId, actorUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<TimesheetSubmitResponseDto>.Ok(result, "Timesheet submitted."));
    }

    [Authorize(Roles = RoleConstants.Employee)]
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TimesheetHistoryItemDto>>>> GetMyTimesheets(
        CancellationToken cancellationToken)
    {
        var employeeId = GetEmployeeId();
        var result = await timesheetService.GetMyTimesheetsAsync(employeeId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TimesheetHistoryItemDto>>.Ok(result, "Timesheets retrieved."));
    }

    [Authorize(Roles = RoleConstants.Employee)]
    [HttpGet("my/{id:long}")]
    public async Task<ActionResult<ApiResponse<TimesheetDetailDto>>> GetMyTimesheetDetail(
        long id,
        CancellationToken cancellationToken)
    {
        var employeeId = GetEmployeeId();
        var result = await timesheetService.GetTimesheetDetailAsync(employeeId, id, cancellationToken);
        return Ok(ApiResponse<TimesheetDetailDto>.Ok(result, "Timesheet detail retrieved."));
    }

    [Authorize(Roles = RoleConstants.Employee)]
    [HttpGet("week-allocations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EmployeeWeekAllocationDto>>>> GetWeekAllocations(
        [FromQuery] DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        var employeeId = GetEmployeeId();
        var result = await timesheetService.GetWeekAllocationsAsync(employeeId, weekStart, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<EmployeeWeekAllocationDto>>.Ok(result, "Week allocations retrieved."));
    }

    [Authorize(Roles = RoleConstants.Employee)]
    [HttpGet("reminder")]
    public async Task<ActionResult<ApiResponse<object>>> GetMissedReminder(CancellationToken cancellationToken)
    {
        var employeeId = GetEmployeeId();
        var showReminder = await timesheetService.HasMissedTimesheetReminderAsync(employeeId, cancellationToken);
        var weekStart = WeekDateHelper.GetMostRecentCompletedWeekMonday();
        return Ok(ApiResponse<object>.Ok(new
        {
            showReminder,
            weekStartDate = weekStart
        }, "Reminder status retrieved."));
    }

    [Authorize(Roles = RoleConstants.Manager)]
    [HttpGet("team")]
    public async Task<ActionResult<ApiResponse<TeamTimesheetListResponseDto>>> GetTeamTimesheets(
        [FromQuery] DateOnly? week,
        CancellationToken cancellationToken)
    {
        var managerUserId = GetActorUserId();
        var result = await timesheetService.GetTeamTimesheetsAsync(managerUserId, week, cancellationToken);
        return Ok(ApiResponse<TeamTimesheetListResponseDto>.Ok(result, "Team timesheets retrieved."));
    }

    [Authorize(Roles = RoleConstants.Manager)]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<ManagerTimesheetDetailDto>>> GetTimesheetForManager(
        long id,
        CancellationToken cancellationToken)
    {
        var managerUserId = GetActorUserId();
        var result = await timesheetService.GetTimesheetForManagerAsync(managerUserId, id, cancellationToken);
        return Ok(ApiResponse<ManagerTimesheetDetailDto>.Ok(result, "Timesheet detail retrieved."));
    }

    private long GetEmployeeId()
    {
        var claim = User.FindFirstValue("employee_id")
            ?? throw new UnauthorizedAccessException("Employee ID not found in token.");
        return long.Parse(claim);
    }

    private long GetActorUserId()
    {
        var claim = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
        return long.Parse(claim);
    }
}
