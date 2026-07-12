using Server.Common;
using Server.Common.Projects;
using Server.Services.Projects.Abstractions;

namespace Server.Services.Projects.Rules;

public class OverdueMilestoneFlagRule : IProjectHealthFlagRule
{
    public string FlagName => ProjectConstants.FlagOverdueMilestone;

    public bool IsTriggered(ProjectHealthEvaluationContext context) =>
        context.Milestones.Any(m =>
            m.DueDate < context.Today
            && m.MilestoneStatus != MilestoneStatusConstants.Done);
}
