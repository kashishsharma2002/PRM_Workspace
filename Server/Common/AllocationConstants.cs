namespace Server.Common;

public static class AllocationConstants
{
    public const decimal MaxUtilizationPercentage = 100m;

    public const string EmploymentStatusBench = "BENCH";
    public const string EmploymentStatusAllocated = "ALLOCATED";

    public static readonly string[] AllocatableProjectStatuses = ["ACTIVE", "PLANNED"];

    public const int RecentActivityWeeks = 4;
}
