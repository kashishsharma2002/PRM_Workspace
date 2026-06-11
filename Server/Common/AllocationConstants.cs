using Server.Common.Projects;

namespace Server.Common;

public static class AllocationConstants
{
    public const decimal MaxUtilizationPercentage = 100m;

    public const string EmploymentStatusBench = "BENCH";
    public const string EmploymentStatusPartiallyAllocated = "PARTIALLY_ALLOCATED";
    public const string EmploymentStatusAllocated = "ALLOCATED";

    public static readonly string[] AllocatableProjectStatuses =
        [ProjectStatusConstants.Active, ProjectStatusConstants.Planned];

    public const int RecentActivityWeeks = 4;
}
