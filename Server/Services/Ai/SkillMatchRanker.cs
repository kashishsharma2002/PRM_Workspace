using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Models.DTOs.Ai;
using Server.Models.DTOs.Ai.Context;

namespace Server.Services.Ai;

public class SkillMatchRanker(ILogger<SkillMatchRanker> logger)
{
    private const decimal MinimumMatchThreshold = 50m;
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

            var skillMatchScore = CalculateSkillMatchScore(candidate, match.SkillName);
            var proficiencyScore = CalculateProficiencyScore(candidate, match.SkillName);
            var capacityScore = candidate.RemainingCapacityPercentage;

            var llmScore = Math.Min(100m, Math.Max(0m, match.MatchScore));
            var finalScore = (llmScore * LlmScoreWeight) +
                            (skillMatchScore * SkillMatchWeight) +
                            (proficiencyScore * ProficiencyWeight) +
                            (capacityScore * CapacityWeight);

            if (finalScore >= MinimumMatchThreshold)
            {
                match.MatchScore = (int)Math.Round(finalScore);
                rankedMatches.Add(match);
            }
            else
            {
                logger.LogDebug(
                    "Filtered out low-scoring match: {EmployeeName} (score: {Score})",
                    match.EmployeeName, finalScore);
            }
        }

        rankedMatches = rankedMatches.OrderByDescending(m => m.MatchScore).ToList();

        if (rankedMatches.Count < response.Matches.Count)
        {
            logger.LogDebug(
                "Ranked and filtered matches. Input: {InputCount}, Output: {OutputCount}, Threshold: {Threshold}",
                response.Matches.Count, rankedMatches.Count, MinimumMatchThreshold);
        }

        response.Matches = rankedMatches;
        return response;
    }

    private decimal CalculateSkillMatchScore(AiSkillMatchCandidateContext candidate, string? matchedSkill)
    {
        if (string.IsNullOrWhiteSpace(matchedSkill) || candidate.Skills == null)
            return 0m;

        var matchCount = candidate.Skills.Count(s =>
            s.SkillName.Equals(matchedSkill, StringComparison.OrdinalIgnoreCase));

        return matchCount > 0 ? 100m : 0m;
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
