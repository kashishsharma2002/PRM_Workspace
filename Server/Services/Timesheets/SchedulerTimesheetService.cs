using Server.Common;
using Server.Common.Timesheets;
using Server.Models.Entities;
using Server.Repositories.Allocations;
using Server.Repositories.Timesheets;

namespace Server.Services.Timesheets;

public class SchedulerTimesheetService(
    IAllocationRepository allocationRepository,
    ITimesheetRepository timesheetRepository) : ISchedulerTimesheetService
{
    public async Task<int> MarkMissedTimesheetsAsync(CancellationToken cancellationToken = default)
    {
        var lastWeekStart = WeekDateHelper.GetMostRecentCompletedWeekMonday();
        var weekEnd = WeekDateHelper.GetWeekEnd(lastWeekStart);
        var allocations = await allocationRepository.GetAllActiveForWeekAsync(lastWeekStart, weekEnd, cancellationToken);
        var employeeIds = allocations.Select(a => a.ResourceProfileId).Distinct().ToList();

        if (employeeIds.Count == 0)
            return 0;

        var existingEmployeeIds = await timesheetRepository.GetEmployeeIdsWithTimesheetForWeekAsync(
            employeeIds, lastWeekStart, cancellationToken);
        var existingSet = existingEmployeeIds.ToHashSet();
        var now = DateTime.UtcNow;
        var created = 0;

        foreach (var employeeId in employeeIds)
        {
            if (existingSet.Contains(employeeId))
                continue;

            await timesheetRepository.AddAsync(new Timesheet
            {
                ResourceProfileId = employeeId,
                WeekStartDate = lastWeekStart,
                Status = TimesheetConstants.StatusMissed,
                TotalHours = 0,
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken);

            created++;
        }

        if (created > 0)
            await timesheetRepository.SaveChangesAsync(cancellationToken);

        return created;
    }
}
