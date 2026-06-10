using Server.Common;
using Server.Models.Entities;

namespace Server.Scheduler;

public static class HealthFlagEvaluator
{
    public static List<string> EvaluateFlags(
        DateOnly endDate,
        IReadOnlyList<ProjectMilestone> milestones,
        decimal expectedHours,
        decimal loggedHours,
        DateOnly today)
    {
        var flags = new List<string>();

        if (milestones.Any(m => m.DueDate < today && m.MilestoneStatus != "DONE"))
            flags.Add(ProjectConstants.FlagOverdueMilestone);

        if (expectedHours > 0 && loggedHours < expectedHours * ProjectConstants.LowHoursThreshold)
            flags.Add(ProjectConstants.FlagLowHours);

        var daysUntilEnd = endDate.DayNumber - today.DayNumber;
        if (daysUntilEnd < ProjectConstants.ApproachingDeadlineDays
            && milestones.Any(m => m.MilestoneStatus != "DONE"))
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
