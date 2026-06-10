using Microsoft.Extensions.Logging;
using Server.Models.DTOs.Ai;

namespace Server.Services.Ai;

public class AiIntegrationService(ILogger<AiIntegrationService> logger) : IAiIntegrationService
{
    public Task<AiRiskSummaryResponseDto> GetRiskSummaryAsync(long managerUserId, long projectId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("AI risk summary requested for project {ProjectId} by manager {ManagerUserId}", projectId, managerUserId);
        throw new NotImplementedException("AI risk summary is available in Phase 8.");
    }

    public Task<AiSkillMatchResponseDto> GetSkillMatchAsync(long managerUserId, long projectId, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("AI skill match requested for project {ProjectId} by manager {ManagerUserId}", projectId, managerUserId);
        throw new NotImplementedException("AI skill match is available in Phase 8.");
    }
}
