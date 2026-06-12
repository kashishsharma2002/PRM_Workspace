using Server.Models.Entities;

namespace Server.Repositories.Timesheets;

public interface ITimesheetRepository
{
    Task<bool> ExistsForWeekAsync(long employeeId, DateOnly weekStart, CancellationToken cancellationToken = default);
    Task<bool> HasSubmittedForWeekAsync(long employeeId, DateOnly weekStart, CancellationToken cancellationToken = default);
    Task<Timesheet?> GetByEmployeeAndWeekAsync(long employeeId, DateOnly weekStart, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<long>> GetEmployeeIdsWithTimesheetForWeekAsync(
        IEnumerable<long> employeeIds,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<long, decimal>> GetLoggedHoursByProjectForWeekAsync(
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
    Task<Timesheet?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Timesheet?> GetByIdForEmployeeAsync(long id, long employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Timesheet>> GetByEmployeeAsync(long employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Timesheet>> GetByEmployeeIdsAndWeekAsync(
        IEnumerable<long> employeeIds,
        DateOnly weekStart,
        CancellationToken cancellationToken = default);
    Task<Timesheet?> GetByIdForEmployeeCheckAsync(long id, CancellationToken cancellationToken = default);
    Task AddAsync(Timesheet timesheet, CancellationToken cancellationToken = default);
    Task AddLineItemAsync(TimesheetLineItem lineItem, CancellationToken cancellationToken = default);
    Task AddLineItemTagAsync(TimesheetLineItemActivityTag tag, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TimesheetLineItem>> GetLineItemsByTimesheetIdAsync(long timesheetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TimesheetLineItemActivityTag>> GetTagsByLineItemIdsAsync(IEnumerable<long> lineItemIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<long, IReadOnlyList<string>>> GetRecentActivityTagsByEmployeeIdsAsync(
        IEnumerable<long> employeeIds,
        DateOnly sinceDate,
        CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
