using Server.Models.DTOs.Ai;

namespace Server.Services.Ai;

public interface IAiIntegrationService
{
    Task<AiRiskSummaryResponseDto> GetRiskSummaryAsync(long managerUserId, long projectId, CancellationToken cancellationToken = default);
    Task<AiSkillMatchResponseDto> GetSkillMatchAsync(long managerUserId, long projectId, CancellationToken cancellationToken = default);
}
