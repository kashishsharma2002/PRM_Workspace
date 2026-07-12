using Server.Common;
using Server.Common.Projects;
using Server.Services.Projects.Abstractions;

namespace Server.Services.Projects.Rules;

public class ApproachingDeadlineFlagRule : IProjectHealthFlagRule
{
    public string FlagName => ProjectConstants.FlagApproachingDeadline;

    public bool IsTriggered(ProjectHealthEvaluationContext context)
    {
        var daysUntilEnd = context.EndDate.DayNumber - context.Today.DayNumber;
        return daysUntilEnd < context.ApproachingDeadlineDays
            && context.Milestones.Any(m => m.MilestoneStatus != MilestoneStatusConstants.Done);
    }
}
