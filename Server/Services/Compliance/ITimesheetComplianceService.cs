namespace Server.Services.Compliance;

public interface ITimesheetComplianceService
{
    Task ProcessTimesheetComplianceAsync(CancellationToken cancellationToken = default);
}
