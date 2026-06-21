namespace Server.Services.Ai.Abstractions;

public interface IAiPromptBuilder
{
    string BuildRiskSummaryPrompt(long projectId, string jsonContext);
    string BuildSkillMatchPrompt(long projectId, string? requirement, string jsonContext);
    string BuildAtRiskSkillMatchPrompt(long projectId, string? requirement, string jsonContext);
    string BuildOrganizationalSkillMatchPrompt(string? requirement, string jsonContext);
    string BuildTeamBuilderPrompt(string requirement, string jsonContext);
    string BuildTeamBuilderRepairPrompt(string requirement, string invalidResponse);
}
