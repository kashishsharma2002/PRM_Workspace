namespace Server.Services.Employees;

public interface IResourceStatusService
{
    Task<int> ReconcileAllResourceStatusesAsync(CancellationToken cancellationToken = default);

    Task ApplyStatusFromActiveAllocationsAsync(long profileId, CancellationToken cancellationToken = default);

    string ResolveStatusFromUtilization(decimal utilizationPercentage);
}
