using Server.Models.DTOs.Ai;
using Server.Models.DTOs.SkillMatching.Context;

namespace Server.Services.TeamBuilder.Abstractions;

public interface ITeamBuilderResponseNormalizer
{
    TeamBuilderResponseDto Normalize(
        TeamBuilderResponseDto response,
        IReadOnlyList<AiSkillMatchCandidateContext> assignableCandidates,
        IReadOnlyList<AiSkillMatchCandidateContext> allCandidates);
}
