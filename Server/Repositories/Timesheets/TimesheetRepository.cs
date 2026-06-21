using Microsoft.EntityFrameworkCore;
using Server.Common;
using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Timesheets;

public class TimesheetRepository(PrmDbContext context) : ITimesheetRepository
{
    public Task<bool> ExistsForWeekAsync(long employeeId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        context.Timesheets.AnyAsync(
            t => t.ResourceProfileId == employeeId && t.WeekStartDate == weekStart,
            cancellationToken);

    public Task<bool> HasSubmittedForWeekAsync(long employeeId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        context.Timesheets.AnyAsync(
            t => t.ResourceProfileId == employeeId
                && t.WeekStartDate == weekStart
                && t.Status == TimesheetConstants.StatusSubmitted,
            cancellationToken);

    public Task<Timesheet?> GetByEmployeeAndWeekAsync(long employeeId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        context.Timesheets
            .AsTracking()
            .FirstOrDefaultAsync(
                t => t.ResourceProfileId == employeeId && t.WeekStartDate == weekStart,
                cancellationToken);

    public async Task<IReadOnlyList<long>> GetEmployeeIdsWithTimesheetForWeekAsync(
        IEnumerable<long> employeeIds,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.ToList();
        if (ids.Count == 0)
            return [];

        return await context.Timesheets
            .Where(t => ids.Contains(t.ResourceProfileId) && t.WeekStartDate == weekStart)
            .Select(t => t.ResourceProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<long, decimal>> GetLoggedHoursByProjectForWeekAsync(
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var rows = await context.TimesheetLineItems
            .Where(li => context.Timesheets.Any(t =>
                t.Id == li.TimesheetId
                && t.WeekStartDate == weekStart
                && t.Status == TimesheetConstants.StatusSubmitted))
            .GroupBy(li => li.ProjectId)
            .Select(g => new { ProjectId = g.Key, TotalHours = g.Sum(li => li.HoursLogged) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ProjectId, r => r.TotalHours);
    }

    public Task<Timesheet?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Timesheets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Timesheet?> GetByIdForEmployeeAsync(long id, long employeeId, CancellationToken cancellationToken = default) =>
        context.Timesheets.FirstOrDefaultAsync(t => t.Id == id && t.ResourceProfileId == employeeId, cancellationToken);

    public async Task<IReadOnlyList<Timesheet>> GetByEmployeeAsync(long employeeId, CancellationToken cancellationToken = default) =>
        await context.Timesheets
            .Where(t => t.ResourceProfileId == employeeId)
            .OrderByDescending(t => t.WeekStartDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Timesheet>> GetByEmployeeIdsAndWeekAsync(
        IEnumerable<long> employeeIds,
        DateOnly weekStart,
        CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.ToList();
        if (ids.Count == 0)
            return [];

        return await context.Timesheets
            .Where(t => ids.Contains(t.ResourceProfileId) && t.WeekStartDate == weekStart)
            .ToListAsync(cancellationToken);
    }

    public Task<Timesheet?> GetByIdForEmployeeCheckAsync(long id, CancellationToken cancellationToken = default) =>
        context.Timesheets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task AddAsync(Timesheet timesheet, CancellationToken cancellationToken = default) =>
        await context.Timesheets.AddAsync(timesheet, cancellationToken);

    public async Task AddLineItemAsync(TimesheetLineItem lineItem, CancellationToken cancellationToken = default) =>
        await context.TimesheetLineItems.AddAsync(lineItem, cancellationToken);

    public async Task AddLineItemTagAsync(TimesheetLineItemActivityTag tag, CancellationToken cancellationToken = default) =>
        await context.TimesheetLineItemActivityTags.AddAsync(tag, cancellationToken);

    public async Task<IReadOnlyList<TimesheetLineItem>> GetLineItemsByTimesheetIdAsync(
        long timesheetId,
        CancellationToken cancellationToken = default) =>
        await context.TimesheetLineItems
            .Where(li => li.TimesheetId == timesheetId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TimesheetLineItemActivityTag>> GetTagsByLineItemIdsAsync(
        IEnumerable<long> lineItemIds,
        CancellationToken cancellationToken = default)
    {
        var ids = lineItemIds.ToList();
        if (ids.Count == 0)
            return [];

        return await context.TimesheetLineItemActivityTags
            .Where(t => ids.Contains(t.TimesheetLineItemId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<long, IReadOnlyList<string>>> GetRecentActivityTagsByEmployeeIdsAsync(
        IEnumerable<long> employeeIds,
        DateOnly sinceDate,
        CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<long, IReadOnlyList<string>>();

        var rows = await (
            from timesheet in context.Timesheets
            join lineItem in context.TimesheetLineItems on timesheet.Id equals lineItem.TimesheetId
            join tagLink in context.TimesheetLineItemActivityTags on lineItem.Id equals tagLink.TimesheetLineItemId
            join tag in context.ActivityTags on tagLink.ActivityTagId equals tag.Id
            where ids.Contains(timesheet.ResourceProfileId)
                  && timesheet.WeekStartDate >= sinceDate
                  && timesheet.Status == TimesheetConstants.StatusSubmitted
            select new { timesheet.ResourceProfileId, TagName = tag.TagName }
        ).Distinct().ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.ResourceProfileId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(r => r.TagName).Distinct().OrderBy(n => n).ToList());
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
