using Server.Common;
using Server.Services.Projects.Abstractions;

namespace Server.Services.Projects;

public class ProjectHealthFlagEvaluator(IEnumerable<IProjectHealthFlagRule> rules) : IProjectHealthFlagEvaluator
{
    private readonly IReadOnlyList<IProjectHealthFlagRule> _rules = rules.ToList();

    public List<string> EvaluateFlags(ProjectHealthEvaluationContext context) =>
        _rules
            .Where(rule => rule.IsTriggered(context))
            .Select(rule => rule.FlagName)
            .ToList();

    public string MapToHealthStatus(IReadOnlyList<string> flags) =>
        flags.Count switch
        {
            0 => ProjectConstants.HealthStatuses[0],
            1 => ProjectConstants.HealthStatuses[1],
            _ => ProjectConstants.HealthStatuses[2]
        };
}
