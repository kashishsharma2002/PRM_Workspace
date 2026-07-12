using Server.Common;
using Server.Services.Projects.Abstractions;

namespace Server.Services.Projects.Rules;

public class LowHoursFlagRule : IProjectHealthFlagRule
{
    public string FlagName => ProjectConstants.FlagLowHours;

    public bool IsTriggered(ProjectHealthEvaluationContext context) =>
        context.ExpectedHours > 0
        && context.LoggedHours < context.ExpectedHours * context.LowHoursThreshold;
}
