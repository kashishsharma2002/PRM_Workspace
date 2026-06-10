using Server.Models.DTOs.Ai;

namespace Server.Services.Ai;

public class AiIntegrationService : IAiIntegrationService
{
    public Task<AiRiskSummaryResponseDto> GetRiskSummaryAsync(long managerUserId, long projectId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("AI risk summary is available in Phase 8.");

    public Task<AiSkillMatchResponseDto> GetSkillMatchAsync(long managerUserId, long projectId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("AI skill match is available in Phase 8.");
}
