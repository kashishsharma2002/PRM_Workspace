using Server.Models.DTOs.Ai;
using Server.Models.DTOs.Ai.Context;

namespace Server.Services.Ai.Abstractions;

public interface ITeamBuilderResponseNormalizer
{
    TeamBuilderResponseDto Normalize(
        TeamBuilderResponseDto response,
        IReadOnlyList<AiSkillMatchCandidateContext> assignableCandidates,
        IReadOnlyList<AiSkillMatchCandidateContext> allCandidates);
}
