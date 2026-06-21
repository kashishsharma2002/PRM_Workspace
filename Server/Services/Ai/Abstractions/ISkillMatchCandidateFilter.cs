using Server.Models.DTOs.Ai.Context;

namespace Server.Services.Ai.Abstractions;

public interface ISkillMatchCandidateFilter
{
    List<AiSkillMatchCandidateContext> FilterByCandidateSkills(
        string? requirement,
        List<AiSkillMatchCandidateContext> candidates);

    List<string> ExtractKeywords(string requirement);

    bool HasRelevantSkills(AiSkillMatchCandidateContext candidate, List<string> keywords);

    string? ResolvePrimaryMatchingSkill(AiSkillMatchCandidateContext candidate, List<string> keywords);
}
