using Server.Common;
using Server.Common.Projects;
using Server.Models.Entities;

namespace Server.Scheduler;

public static class HealthFlagEvaluator
{
    public static List<string> EvaluateFlags(
        DateOnly endDate,
        IReadOnlyList<ProjectMilestone> milestones,
        decimal expectedHours,
        decimal loggedHours,
        DateOnly today,
        decimal lowHoursThreshold,
        int approachingDeadlineDays)
    {
        var flags = new List<string>();

        if (milestones.Any(m => m.DueDate < today && m.MilestoneStatus != MilestoneStatusConstants.Done))
            flags.Add(ProjectConstants.FlagOverdueMilestone);

        if (expectedHours > 0 && loggedHours < expectedHours * lowHoursThreshold)
            flags.Add(ProjectConstants.FlagLowHours);

        var daysUntilEnd = endDate.DayNumber - today.DayNumber;
        if (daysUntilEnd < approachingDeadlineDays
            && milestones.Any(m => m.MilestoneStatus != MilestoneStatusConstants.Done))
        {
            flags.Add(ProjectConstants.FlagApproachingDeadline);
        }

        return flags;
    }

    public static string MapToHealthStatus(IReadOnlyList<string> flags) =>
        flags.Count switch
        {
            0 => ProjectConstants.HealthStatuses[0],
            1 => ProjectConstants.HealthStatuses[1],
            _ => ProjectConstants.HealthStatuses[2]
        };
}
