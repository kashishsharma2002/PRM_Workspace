using Client.Common;
using Client.Models.Ai;

namespace Client.HttpClients;

public class AiClient(RestClient restClient) : IAiClient
{
    public Task<AiSkillMatchResponse?> GetOrganizationalSkillMatchAsync(string requirement) =>
        restClient.GetAsync<AiSkillMatchResponse>(ApiRoutes.AiSkillMatch(requirement), requireAuth: true);

    public Task<AiSkillMatchResponse?> GetProjectSkillMatchAsync(long projectId, string requirement) =>
        restClient.GetAsync<AiSkillMatchResponse>(
            ApiRoutes.AiProjectSkillMatch(projectId, requirement),
            requireAuth: true);

    public Task<AiRiskSummaryResponse?> GetProjectRiskSummaryAsync(long projectId) =>
        restClient.GetAsync<AiRiskSummaryResponse>(
            ApiRoutes.AiProjectRiskSummary(projectId),
            requireAuth: true);

    public Task<TeamBuilderResponse?> BuildTeamAsync(string requirement) =>
        restClient.GetAsync<TeamBuilderResponse>(ApiRoutes.AiTeamBuilder(requirement), requireAuth: true);
}
