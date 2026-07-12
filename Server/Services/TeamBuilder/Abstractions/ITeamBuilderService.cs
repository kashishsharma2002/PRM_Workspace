using Server.Models.DTOs.Ai;

namespace Server.Services.TeamBuilder.Abstractions;

public interface ITeamBuilderService
{
    Task<TeamBuilderResponseDto> BuildTeamAsync(
        long managerUserId,
        string? requirement,
        CancellationToken cancellationToken = default);
}
