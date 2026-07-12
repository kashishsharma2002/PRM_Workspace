using Server.Models.DTOs.ProjectRisk;

namespace Server.Services.ProjectRisk.Abstractions;

public interface IProjectRiskInsightsService
{
    Task<AiRiskSummaryResponseDto> GetRiskSummaryAsync(
        long managerUserId,
        long projectId,
        CancellationToken cancellationToken = default);
}
