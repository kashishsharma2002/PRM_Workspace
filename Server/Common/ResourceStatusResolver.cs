namespace Server.Common;

public static class ResourceStatusResolver
{
    public static string FromUtilization(decimal utilizationPercentage)
    {
        if (utilizationPercentage <= 0m)
            return ResourceStatusConstants.Bench;

        if (utilizationPercentage < AllocationConstants.MaxUtilizationPercentage)
            return ResourceStatusConstants.PartiallyAllocated;

        return ResourceStatusConstants.Allocated;
    }
}
