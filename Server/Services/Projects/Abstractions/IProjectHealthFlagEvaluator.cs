namespace Server.Services.Projects.Abstractions;

public interface IProjectHealthFlagEvaluator
{
    List<string> EvaluateFlags(ProjectHealthEvaluationContext context);

    string MapToHealthStatus(IReadOnlyList<string> flags);
}
