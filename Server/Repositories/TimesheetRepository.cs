using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class TimesheetRepository(PrmDbContext context) : ITimesheetRepository
{
    public Task<bool> ExistsForWeekAsync(long employeeId, DateOnly weekStart, CancellationToken cancellationToken = default) =>
        context.Timesheets.AnyAsync(
            t => t.EmployeeId == employeeId && t.WeekStartDate == weekStart,
            cancellationToken);

    public Task<Timesheet?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Timesheets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Timesheet?> GetByIdForEmployeeAsync(long id, long employeeId, CancellationToken cancellationToken = default) =>
        context.Timesheets.FirstOrDefaultAsync(t => t.Id == id && t.EmployeeId == employeeId, cancellationToken);

    public async Task<IReadOnlyList<Timesheet>> GetByEmployeeAsync(long employeeId, CancellationToken cancellationToken = default) =>
        await context.Timesheets
            .Where(t => t.EmployeeId == employeeId)
            .OrderByDescending(t => t.WeekStartDate)
            .ToListAsync(cancellationToken);

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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
