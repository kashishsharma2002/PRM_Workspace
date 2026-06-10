namespace Server.Common;

public static class ProjectConstants
{
    public static readonly string[] CreateStatuses = ["PLANNED", "ACTIVE", "ON_HOLD"];
    public static readonly string[] UpdateStatuses = ["PLANNED", "ACTIVE", "ON_HOLD", "COMPLETED"];
    public static readonly string[] MilestoneStatuses = ["NOT_STARTED", "IN_PROGRESS", "DONE"];
    public static readonly string[] HealthStatuses = ["GREEN", "AMBER", "RED"];

    public const string FlagOverdueMilestone = "OVERDUE_MILESTONE";
    public const string FlagLowHours = "LOW_HOURS";
    public const string FlagApproachingDeadline = "APPROACHING_DEADLINE";
    public const decimal LowHoursThreshold = 0.6m;
    public const int ApproachingDeadlineDays = 28;
}
