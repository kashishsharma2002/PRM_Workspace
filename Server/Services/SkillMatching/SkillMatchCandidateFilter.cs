using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Models.DTOs.SkillMatching.Context;
using Server.Services.SkillMatching.Abstractions;

namespace Server.Services.SkillMatching;

public class SkillMatchCandidateFilter(ILogger<SkillMatchCandidateFilter> logger) : ISkillMatchCandidateFilter
{
    private const int MaxCandidatePoolSize = 30;

    private static readonly Dictionary<string, string[]> KeywordToSkillCategories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["devops"] = [EmployeeConstants.DevOpsCategory],
            ["devsecops"] = [EmployeeConstants.DevOpsCategory],
            ["sre"] = [EmployeeConstants.DevOpsCategory],
            ["docker"] = [EmployeeConstants.DevOpsCategory],
            ["kubernetes"] = [EmployeeConstants.DevOpsCategory],
            ["k8s"] = [EmployeeConstants.DevOpsCategory],
            ["java"] = [EmployeeConstants.BackendCategory],
            ["python"] = [EmployeeConstants.BackendCategory],
            ["spring"] = [EmployeeConstants.BackendCategory],
            ["dotnet"] = [EmployeeConstants.BackendCategory],
            ["react"] = [EmployeeConstants.FrontendCategory],
            ["angular"] = [EmployeeConstants.FrontendCategory],
            ["vue"] = [EmployeeConstants.FrontendCategory],
            ["frontend"] = [EmployeeConstants.FrontendCategory],
            ["backend"] = [EmployeeConstants.BackendCategory],
            ["qa"] = [EmployeeConstants.QaCategory],
            ["testing"] = [EmployeeConstants.QaCategory],
            ["selenium"] = [EmployeeConstants.QaCategory],
        };

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
            logger.LogDebug(
                "No candidates matched requirement keywords: {Keywords}.",
                string.Join(", ", keywords));
            return [];
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

    public List<string> ExtractKeywords(string requirement)
    {
        var normalized = requirement.ToLowerInvariant();
        var words = Regex.Split(normalized, @"[\s,.;:()]+")
            .Select(w => w.Trim())
            .Where(w => w.Length >= 2 && !IsCommonWord(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Contains("devops", StringComparison.Ordinal) && !words.Contains("devops"))
            words.Add("devops");
        if (normalized.Contains("c#", StringComparison.Ordinal) && !words.Contains("c#"))
            words.Add("c#");
        if (normalized.Contains(".net", StringComparison.Ordinal) && !words.Contains(".net"))
            words.Add(".net");

        return words;
    }

    public bool HasRelevantSkills(AiSkillMatchCandidateContext candidate, List<string> keywords)
    {
        if (keywords.Count == 0)
            return true;

        if (MatchesProfileText(candidate.Designation, keywords) || MatchesProfileText(candidate.Department, keywords))
            return true;

        if (candidate.Skills == null || candidate.Skills.Count == 0)
            return false;

        var skillNamesCombined = string.Join(" ", candidate.Skills.Select(s => s.SkillName)).ToLowerInvariant();

        foreach (var keyword in keywords)
        {
            if (skillNamesCombined.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                IsPartialMatch(skillNamesCombined, keyword))
            {
                return true;
            }

            if (KeywordToSkillCategories.TryGetValue(keyword, out var categories) &&
                candidate.Skills.Any(s => categories.Contains(s.Category, StringComparer.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    public string? ResolvePrimaryMatchingSkill(AiSkillMatchCandidateContext candidate, List<string> keywords)
    {
        if (candidate.Skills == null || candidate.Skills.Count == 0)
            return null;

        foreach (var keyword in keywords)
        {
            var direct = candidate.Skills.FirstOrDefault(s =>
                s.SkillName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                keyword.Contains(s.SkillName, StringComparison.OrdinalIgnoreCase));
            if (direct is not null)
                return direct.SkillName;

            if (KeywordToSkillCategories.TryGetValue(keyword, out var categories))
            {
                var categorySkill = candidate.Skills.FirstOrDefault(s =>
                    categories.Contains(s.Category, StringComparer.OrdinalIgnoreCase));
                if (categorySkill is not null)
                    return categorySkill.SkillName;
            }
        }

        return candidate.Skills[0].SkillName;
    }

    private static bool MatchesProfileText(string? value, List<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var normalized = value.Replace('_', ' ').ToLowerInvariant();
        return keywords.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPartialMatch(string source, string keyword)
    {
        return source.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(word => word.StartsWith(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsCommonWord(string word)
    {
        var common = new[]
        {
            "the", "and", "for", "with", "from", "your", "this", "that",
            "need", "want", "looking", "engineer", "developer", "resource", "senior", "junior", "team"
        };
        return common.Contains(word, StringComparer.OrdinalIgnoreCase);
    }
}
