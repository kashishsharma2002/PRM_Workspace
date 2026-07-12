using Server.Models.DTOs.Timesheets;

namespace Server.Services.Timesheets;

public interface ITimesheetService : IEmployeeTimesheetService, IManagerTimesheetService, ISchedulerTimesheetService
{
    Task<bool> HasMissedTimesheetReminderAsync(long employeeId, CancellationToken cancellationToken = default);
}
