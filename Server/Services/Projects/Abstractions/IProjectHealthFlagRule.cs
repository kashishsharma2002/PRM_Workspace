using Server.Services.Projects;

namespace Server.Services.Projects.Abstractions;

public interface IProjectHealthFlagRule
{
    string FlagName { get; }

    bool IsTriggered(ProjectHealthEvaluationContext context);
}
