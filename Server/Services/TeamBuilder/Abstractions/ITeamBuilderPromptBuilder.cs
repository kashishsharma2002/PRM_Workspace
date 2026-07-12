namespace Server.Services.TeamBuilder.Abstractions;

public interface ITeamBuilderPromptBuilder
{
    string BuildTeamBuilderPrompt(string requirement, string jsonContext);
    string BuildTeamBuilderRepairPrompt(string requirement, string previousResponse);
}
