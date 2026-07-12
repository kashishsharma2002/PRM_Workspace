namespace Server.Services.ProjectRisk.Abstractions;

public interface IProjectRiskPromptBuilder
{
    string BuildRiskSummaryPrompt(long projectId, string jsonContext);
}
