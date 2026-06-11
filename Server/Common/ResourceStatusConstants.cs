namespace Server.Common;

public static class ResourceStatusConstants
{
    public const string Bench = "BENCH";
    public const string PartiallyAllocated = "PARTIALLY_ALLOCATED";
    public const string Allocated = "ALLOCATED";

    public static readonly string[] All = [Bench, PartiallyAllocated, Allocated];
}
