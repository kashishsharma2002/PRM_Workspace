using Server.Models.DTOs.SkillMatching;

namespace Server.Services.SkillMatching.Abstractions;

public interface ISkillMatchingService
{
    Task<AiSkillMatchResponseDto> GetSkillMatchAsync(
        long managerUserId,
        long projectId,
        string? requirement,
        SkillMatchOptions? options = null,
        CancellationToken cancellationToken = default);

    Task<AiSkillMatchResponseDto> GetOrganizationalSkillMatchAsync(
        long managerUserId,
        string? requirement,
        CancellationToken cancellationToken = default);
}
