using Server.Models.DTOs.Ai;
using Server.Models.DTOs.Ai.Context;

namespace Server.Services.Ai.Abstractions;

public interface ISkillMatchRanker
{
    AiSkillMatchResponseDto RankAndFilterMatches(
        AiSkillMatchResponseDto result,
        List<AiSkillMatchCandidateContext> candidatePool,
        string? requirement);
}
