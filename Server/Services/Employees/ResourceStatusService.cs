using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Repositories.Allocations;
using Server.Repositories.Employees;

namespace Server.Services.Employees;

public class ResourceStatusService(
    IEmployeeRepository employeeRepository,
    IAllocationRepository allocationRepository,
    ILogger<ResourceStatusService> logger) : IResourceStatusService
{
    public string ResolveStatusFromUtilization(decimal utilizationPercentage) =>
        ResourceStatusResolver.FromUtilization(utilizationPercentage);

    public async Task ApplyStatusFromActiveAllocationsAsync(
        long profileId,
        CancellationToken cancellationToken = default)
    {
        var profile = await employeeRepository.GetByIdAsync(profileId, cancellationToken);
        if (profile is null)
            return;

        var activeAllocations = await allocationRepository.GetActiveByEmployeeIdAsync(profileId, cancellationToken);
        var utilization = activeAllocations.Sum(a => a.AllocationPercentage);
        var resolvedStatus = ResolveStatusFromUtilization(utilization);

        if (profile.ResourceStatus == resolvedStatus)
            return;

        profile.ResourceStatus = resolvedStatus;
        profile.UpdatedAt = DateTime.UtcNow;
        await employeeRepository.UpdateAsync(profile, cancellationToken);
    }

    public async Task<int> ReconcileAllResourceStatusesAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await employeeRepository.GetAllAsync(null, null, cancellationToken);
        if (profiles.Count == 0)
            return 0;

        var profileIds = profiles.Select(p => p.Id).ToList();
        var activeAllocations = await allocationRepository.GetActiveByEmployeeIdsAsync(profileIds, cancellationToken);
        var utilizationByProfile = activeAllocations
            .GroupBy(a => a.ResourceProfileId)
            .ToDictionary(g => g.Key, g => g.Sum(a => a.AllocationPercentage));

        var now = DateTime.UtcNow;
        var updatedCount = 0;

        foreach (var profile in profiles)
        {
            var utilization = utilizationByProfile.GetValueOrDefault(profile.Id, 0m);
            var resolvedStatus = ResolveStatusFromUtilization(utilization);

            if (profile.ResourceStatus == resolvedStatus)
                continue;

            profile.ResourceStatus = resolvedStatus;
            profile.UpdatedAt = now;
            await employeeRepository.UpdateAsync(profile, cancellationToken);
            updatedCount++;
        }

        if (updatedCount > 0)
            await employeeRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Resource status reconciliation updated {UpdatedCount} profiles.", updatedCount);
        return updatedCount;
    }
}
