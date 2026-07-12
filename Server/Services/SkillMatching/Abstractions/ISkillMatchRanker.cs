using Server.Models.DTOs.SkillMatching;

namespace Server.Services.SkillMatching.Abstractions;

public interface ISkillMatchRanker
{
    AiSkillMatchResponseDto RankAndFilterMatches(
        AiSkillMatchResponseDto result,
        List<Server.Models.DTOs.SkillMatching.Context.AiSkillMatchCandidateContext> candidatePool,
        string? requirement = null);
}
