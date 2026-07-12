using Server.Models.DTOs.Scheduler;

namespace Server.Services.Compliance;

public interface ITimesheetComplianceService
{
    Task<SchedulerComplianceResultDto> ProcessTimesheetComplianceAsync(
        CancellationToken cancellationToken = default);
}
