using Client.Models.Ai;
using Client.Models.ProjectRisk;
using Client.Models.SkillMatching;

namespace Client.HttpClients;

public interface IAiClient
{
    Task<AiSkillMatchResponse?> GetOrganizationalSkillMatchAsync(string requirement);
    Task<AiSkillMatchResponse?> GetProjectSkillMatchAsync(long projectId, string requirement);
    Task<AiRiskSummaryResponse?> GetProjectRiskSummaryAsync(long projectId);
    Task<TeamBuilderResponse?> BuildTeamAsync(string requirement);
}
