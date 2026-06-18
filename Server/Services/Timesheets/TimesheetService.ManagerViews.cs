using Server.Common;
using Server.Exceptions;
using Server.Models.DTOs.Timesheets;
using Server.Services.Shared;

namespace Server.Services.Timesheets;

public partial class TimesheetService
{
    public async Task<TeamTimesheetListResponseDto> GetTeamTimesheetsAsync(
        long managerUserId,
        DateOnly? weekStart,
        CancellationToken cancellationToken = default)
    {
        var resolvedWeek = weekStart ?? WeekDateHelper.GetCurrentWeekMonday();
        var weekEnd = WeekDateHelper.GetWeekEnd(resolvedWeek);
        var team = await employeeRepository.GetByManagerIdAsync(managerUserId, cancellationToken);
        var employeeIds = team.Select(e => e.Id).ToList();

        if (employeeIds.Count == 0)
        {
            return new TeamTimesheetListResponseDto
            {
                WeekStartDate = resolvedWeek,
                Rows = [],
                FrozenEmployees = []
            };
        }

        var userIds = team.Select(e => e.UserId).Distinct().ToList();
        var users = await userRepository.GetByIdsAsync(userIds, cancellationToken);
        var employeeNameLookup = team.ToDictionary(
            e => e.Id,
            e => users.TryGetValue(e.UserId, out var user) ? user.FullName : "Unknown");

        var frozenEmployees = team
            .Where(e => e.IsTimesheetFrozen)
            .Select(e => new FrozenTeamMemberDto
            {
                EmployeeId = e.Id,
                EmployeeName = employeeNameLookup.GetValueOrDefault(e.Id, "Unknown")
            })
            .OrderBy(e => e.EmployeeName)
            .ToList();

        var allocations = await allocationRepository.GetActiveByEmployeeIdsForWeekAsync(
            employeeIds, resolvedWeek, weekEnd, cancellationToken);
        var timesheets = await timesheetRepository.GetByEmployeeIdsAndWeekAsync(
            employeeIds, resolvedWeek, cancellationToken);
        var timesheetByEmployee = timesheets.ToDictionary(t => t.ResourceProfileId);

        var rows = new List<TeamTimesheetRowDto>();
        foreach (var allocation in allocations)
        {
            var project = await projectRepository.GetByIdAsync(allocation.ProjectId, cancellationToken);
            var projectName = project?.ProjectName ?? "Unknown";
            var employeeName = employeeNameLookup.GetValueOrDefault(allocation.ResourceProfileId, "Unknown");

            if (!timesheetByEmployee.TryGetValue(allocation.ResourceProfileId, out var timesheet))
            {
                if (weekEnd < DateOnly.FromDateTime(DateTime.Today))
                {
                    rows.Add(new TeamTimesheetRowDto
                    {
                        TimesheetId = null,
                        EmployeeName = employeeName,
                        ProjectName = projectName,
                        HoursLogged = 0,
                        Status = TimesheetConstants.StatusMissed
                    });
                }

                continue;
            }

            if (timesheet.Status == TimesheetConstants.StatusMissed)
            {
                rows.Add(new TeamTimesheetRowDto
                {
                    TimesheetId = timesheet.Id,
                    EmployeeName = employeeName,
                    ProjectName = projectName,
                    HoursLogged = 0,
                    Status = TimesheetConstants.StatusMissed
                });
                continue;
            }

            if (timesheet.Status != TimesheetConstants.StatusSubmitted)
                continue;

            var lineItems = await timesheetRepository.GetLineItemsByTimesheetIdAsync(timesheet.Id, cancellationToken);
            var projectLine = lineItems.FirstOrDefault(li => li.ProjectId == allocation.ProjectId);
            if (projectLine is null)
                continue;

            rows.Add(new TeamTimesheetRowDto
            {
                TimesheetId = timesheet.Id,
                EmployeeName = employeeName,
                ProjectName = projectName,
                HoursLogged = projectLine.HoursLogged,
                Status = TimesheetConstants.StatusSubmitted
            });
        }

        return new TeamTimesheetListResponseDto
        {
            WeekStartDate = resolvedWeek,
            Rows = rows,
            FrozenEmployees = frozenEmployees
        };
    }

    public async Task<ManagerTimesheetDetailDto> GetTimesheetForManagerAsync(
        long managerUserId,
        long timesheetId,
        CancellationToken cancellationToken = default)
    {
        var timesheet = await timesheetRepository.GetByIdForEmployeeCheckAsync(timesheetId, cancellationToken)
            ?? throw new NotFoundAppException("Timesheet not found.");

        var profile = await employeeRepository.GetByIdAsync(timesheet.ResourceProfileId, cancellationToken)
            ?? throw new NotFoundAppException("Timesheet not found.");

        if (profile.ManagerId != managerUserId)
            throw new NotFoundAppException("Timesheet not found.");

        var user = await userRepository.GetByIdAsync(profile.UserId, cancellationToken);
        var detail = await GetTimesheetDetailAsync(timesheet.ResourceProfileId, timesheetId, cancellationToken);

        return new ManagerTimesheetDetailDto
        {
            Id = detail.Id,
            EmployeeName = user?.FullName ?? "Unknown",
            WeekStartDate = detail.WeekStartDate,
            Status = detail.Status,
            TotalHours = detail.TotalHours,
            LineItems = detail.LineItems
        };
    }
}
