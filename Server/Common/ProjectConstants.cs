using Server.Common.Projects;

namespace Server.Common;

public static class ProjectConstants
{
    public static readonly string[] CreateStatuses =
        [ProjectStatusConstants.Planned, ProjectStatusConstants.Active, ProjectStatusConstants.OnHold];

    public static readonly string[] UpdateStatuses =
    [
        ProjectStatusConstants.Planned, ProjectStatusConstants.Active,
        ProjectStatusConstants.OnHold, ProjectStatusConstants.Completed
    ];

    public static readonly string[] MilestoneStatuses =
        [MilestoneStatusConstants.NotStarted, MilestoneStatusConstants.InProgress, MilestoneStatusConstants.Done];

    public static readonly string[] HealthStatuses =
        [HealthStatusConstants.Green, HealthStatusConstants.Amber, HealthStatusConstants.Red];

    public const string FlagOverdueMilestone = "OVERDUE_MILESTONE";
    public const string FlagLowHours = "LOW_HOURS";
    public const string FlagApproachingDeadline = "APPROACHING_DEADLINE";
    public const decimal LowHoursThreshold = 0.6m;
    public const int ApproachingDeadlineDays = 28;
}
