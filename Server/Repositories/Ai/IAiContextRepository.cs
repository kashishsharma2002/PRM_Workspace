using Server.Services.Ai.Models;

namespace Server.Repositories.Ai;

public interface IAiContextRepository
{
    Task<AiRiskContextModel?> GetRiskContextAsync(long projectId, CancellationToken cancellationToken = default);
    Task<AiSkillMatchContextModel?> GetSkillMatchContextAsync(long projectId, CancellationToken cancellationToken = default);
    Task<AiOrganizationalSkillMatchContextModel> GetOrganizationalSkillMatchContextAsync(CancellationToken cancellationToken = default);
}
