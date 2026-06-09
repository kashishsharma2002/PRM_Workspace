namespace Server.Common;

public static class ProjectConstants
{
    public static readonly string[] CreateStatuses = ["PLANNED", "ACTIVE", "ON_HOLD"];
    public static readonly string[] UpdateStatuses = ["PLANNED", "ACTIVE", "ON_HOLD", "COMPLETED"];
    public static readonly string[] MilestoneStatuses = ["NOT_STARTED", "IN_PROGRESS", "DONE"];
}
