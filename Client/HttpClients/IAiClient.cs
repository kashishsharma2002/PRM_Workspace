using Client.Models.Ai;

namespace Client.HttpClients;

public interface IAiClient
{
    Task<AiSkillMatchResponse?> GetOrganizationalSkillMatchAsync(string requirement);
    Task<AiSkillMatchResponse?> GetProjectSkillMatchAsync(long projectId, string requirement);
    Task<AiRiskSummaryResponse?> GetProjectRiskSummaryAsync(long projectId);
    Task<TeamBuilderResponse?> BuildTeamAsync(string requirement);
}
