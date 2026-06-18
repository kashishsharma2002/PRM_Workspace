using Server.Models.DTOs.Ai;

namespace Server.Services.Ai.Abstractions;

public interface IAiIntegrationService
{
    Task<AiRiskSummaryResponseDto> GetRiskSummaryAsync(long managerUserId, long projectId, CancellationToken cancellationToken = default);
    Task<AiSkillMatchResponseDto> GetSkillMatchAsync(
        long managerUserId,
        long projectId,
        string? requirement,
        SkillMatchOptions? options = null,
        CancellationToken cancellationToken = default);
    Task<AiSkillMatchResponseDto> GetOrganizationalSkillMatchAsync(long managerUserId, string? requirement, CancellationToken cancellationToken = default);
    Task<TeamBuilderResponseDto> BuildTeamAsync(long managerUserId, string? requirement, CancellationToken cancellationToken = default);
}
