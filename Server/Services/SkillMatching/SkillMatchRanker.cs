using Microsoft.Extensions.Logging;
using Server.Models.DTOs.SkillMatching;
using Server.Models.DTOs.SkillMatching.Context;
using Server.Services.SkillMatching.Abstractions;

namespace Server.Services.SkillMatching;

public class SkillMatchRanker(
    ISkillMatchCandidateFilter skillMatchCandidateFilter,
    ILogger<SkillMatchRanker> logger) : ISkillMatchRanker
{
    private const decimal LlmScoreWeight = 0.40m;
    private const decimal SkillMatchWeight = 0.30m;
    private const decimal ProficiencyWeight = 0.20m;
    private const decimal CapacityWeight = 0.10m;

    public AiSkillMatchResponseDto RankAndFilterMatches(
        AiSkillMatchResponseDto response,
        List<AiSkillMatchCandidateContext> candidatePool,
        string? requirement = null)
    {
        if (response.Matches == null || response.Matches.Count == 0)
            return response;

        var candidateByName = candidatePool.ToDictionary(c => c.FullName, c => c);
        var rankedMatches = new List<AiSkillMatchItemDto>();

        foreach (var match in response.Matches)
        {
            if (!candidateByName.TryGetValue(match.EmployeeName, out var candidate))
            {
                logger.LogWarning(
                    "Hallucinated candidate in LLM response: {EmployeeName}. Dropping match.",
                    match.EmployeeName);
                continue;
            }

            var skillMatchScore = CalculateSkillMatchScore(candidate, match.SkillName, requirement);
            if (skillMatchScore < 100m)
            {
                logger.LogDebug(
                    "Filtered out non-matching skill candidate: {EmployeeName} for requirement: {Requirement}",
                    match.EmployeeName, requirement);
                continue;
            }

            var keywords = string.IsNullOrWhiteSpace(requirement)
                ? []
                : skillMatchCandidateFilter.ExtractKeywords(requirement);
            var resolvedSkill = skillMatchCandidateFilter.ResolvePrimaryMatchingSkill(candidate, keywords);
            if (!string.IsNullOrWhiteSpace(resolvedSkill))
                match.SkillName = resolvedSkill;

            var proficiencyScore = CalculateProficiencyScore(candidate, match.SkillName);
            var capacityScore = candidate.RemainingCapacityPercentage;

            var llmScore = Math.Min(100m, Math.Max(0m, match.MatchScore));
            var finalScore = (llmScore * LlmScoreWeight) +
                            (skillMatchScore * SkillMatchWeight) +
                            (proficiencyScore * ProficiencyWeight) +
                            (capacityScore * CapacityWeight);

            match.MatchScore = (int)Math.Round(finalScore);
            match.RemainingCapacityPercentage = candidate.RemainingCapacityPercentage;
            rankedMatches.Add(match);
        }

        response.Matches = rankedMatches.OrderByDescending(m => m.MatchScore).ToList();
        return response;
    }

    private decimal CalculateSkillMatchScore(
        AiSkillMatchCandidateContext candidate,
        string? matchedSkill,
        string? requirement)
    {
        if (candidate.Skills == null || candidate.Skills.Count == 0)
            return 0m;

        if (string.IsNullOrWhiteSpace(requirement))
            return 0m;

        var keywords = skillMatchCandidateFilter.ExtractKeywords(requirement);
        if (keywords.Count == 0 || !skillMatchCandidateFilter.HasRelevantSkills(candidate, keywords))
            return 0m;

        if (!string.IsNullOrWhiteSpace(matchedSkill) &&
            candidate.Skills.Any(s => s.SkillName.Equals(matchedSkill, StringComparison.OrdinalIgnoreCase)))
        {
            return 100m;
        }

        return 100m;
    }

    private decimal CalculateProficiencyScore(AiSkillMatchCandidateContext candidate, string? skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName) || candidate.Skills == null)
            return 0m;

        var skill = candidate.Skills.FirstOrDefault(s =>
            s.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase));

        if (skill == null)
            return 0m;

        return skill.ProficiencyLevel.ToUpperInvariant() switch
        {
            "ADVANCED" => 100m,
            "INTERMEDIATE" => 66m,
            "BEGINNER" => 33m,
            _ => 0m
        };
    }
}
