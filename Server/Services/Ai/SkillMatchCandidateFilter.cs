using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Server.Models.DTOs.Ai.Context;

namespace Server.Services.Ai;

public class SkillMatchCandidateFilter(ILogger<SkillMatchCandidateFilter> logger)
{
    private const int MaxCandidatePoolSize = 30;

    public List<AiSkillMatchCandidateContext> FilterByCandidateSkills(
        string? requirement,
        List<AiSkillMatchCandidateContext> candidates)
    {
        if (string.IsNullOrWhiteSpace(requirement) || candidates.Count == 0)
            return candidates;

        var keywords = ExtractKeywords(requirement);
        if (keywords.Count == 0)
            return candidates;

        var filtered = candidates
            .Where(c => HasRelevantSkills(c, keywords))
            .ToList();

        if (filtered.Count == 0)
        {
            logger.LogWarning(
                "No candidates matched requirement keywords: {Keywords}. Returning full pool.",
                string.Join(", ", keywords));
            return candidates;
        }

        if (filtered.Count > MaxCandidatePoolSize)
        {
            filtered = filtered.Take(MaxCandidatePoolSize).ToList();
            logger.LogDebug(
                "Candidate pool reduced from {Total} to {Limited} (max pool size: {MaxPoolSize})",
                candidates.Count, filtered.Count, MaxCandidatePoolSize);
        }

        logger.LogDebug(
            "Filtered candidates by skills. Input: {InputCount}, Output: {OutputCount}, Keywords: {Keywords}",
            candidates.Count, filtered.Count, string.Join(", ", keywords));

        return filtered;
    }

    private List<string> ExtractKeywords(string requirement)
    {
        var words = Regex.Split(requirement.ToLowerInvariant(), @"\s+")
            .Where(w => w.Length > 2 && !IsCommonWord(w))
            .Distinct()
            .ToList();

        return words;
    }

    private bool HasRelevantSkills(AiSkillMatchCandidateContext candidate, List<string> keywords)
    {
        if (candidate.Skills == null || candidate.Skills.Count == 0)
            return false;

        var skillNamesCombined = string.Join(" ", candidate.Skills.Select(s => s.SkillName)).ToLowerInvariant();

        return keywords.Any(keyword =>
            skillNamesCombined.Contains(keyword) ||
            IsPartialMatch(skillNamesCombined, keyword));
    }

    private bool IsPartialMatch(string source, string keyword)
    {
        return source.Split(' ').Any(word => word.StartsWith(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsCommonWord(string word)
    {
        var common = new[] { "the", "and", "for", "with", "from", "your", "this", "that" };
        return common.Contains(word, StringComparer.OrdinalIgnoreCase);
    }
}
