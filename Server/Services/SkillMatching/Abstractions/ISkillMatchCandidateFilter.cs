using Server.Models.DTOs.SkillMatching;
using Server.Models.DTOs.SkillMatching.Context;

namespace Server.Services.SkillMatching.Abstractions;

public interface ISkillMatchCandidateFilter
{
    List<AiSkillMatchCandidateContext> FilterByCandidateSkills(
        string? requirement,
        List<AiSkillMatchCandidateContext> candidates);

    List<string> ExtractKeywords(string requirement);

    bool HasRelevantSkills(AiSkillMatchCandidateContext candidate, List<string> keywords);

    string? ResolvePrimaryMatchingSkill(AiSkillMatchCandidateContext candidate, List<string> keywords);
}
