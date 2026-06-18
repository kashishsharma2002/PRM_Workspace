using Microsoft.Extensions.Logging;
using Server.Models.DTOs.Ai;
using Server.Models.DTOs.Ai.Context;
using Server.Services.Ai;
using Tests.Helpers;

namespace Tests.AI;

public class SkillMatchCandidateFilterTests
{
    private readonly SkillMatchCandidateFilter _filter;
    private readonly ILogger<SkillMatchCandidateFilter> _logger;

    public SkillMatchCandidateFilterTests()
    {
        _logger = TestServiceFactory.CreateLogger<SkillMatchCandidateFilter>();
        _filter = new SkillMatchCandidateFilter(_logger);
    }

    [Fact]
    public void FilterByCandidateSkills_PythonRequirement_IncludesPythonSkills()
    {
        var candidates = new List<AiSkillMatchCandidateContext>
        {
            new()
            {
                FullName = "Alice",
                Skills = new List<AiSkillContext>
                {
                    new() { SkillName = "Python", ProficiencyLevel = "ADVANCED" }
                }
            },
            new()
            {
                FullName = "Bob",
                Skills = new List<AiSkillContext>
                {
                    new() { SkillName = "Java", ProficiencyLevel = "INTERMEDIATE" }
                }
            }
        };

        var filtered = _filter.FilterByCandidateSkills("Python Developer", candidates);

        Assert.Single(filtered);
        Assert.Equal("Alice", filtered[0].FullName);
    }

    [Fact]
    public void FilterByCandidateSkills_NoMatches_ReturnsFullPool()
    {
        var candidates = new List<AiSkillMatchCandidateContext>
        {
            new()
            {
                FullName = "Alice",
                Skills = new List<AiSkillContext>
                {
                    new() { SkillName = "Ruby", ProficiencyLevel = "BEGINNER" }
                }
            }
        };

        var filtered = _filter.FilterByCandidateSkills("Python Database", candidates);

        Assert.Single(filtered);
    }

    [Fact]
    public void FilterByCandidateSkills_EmptyRequirement_ReturnsFullPool()
    {
        var candidates = new List<AiSkillMatchCandidateContext>
        {
            new() { FullName = "Alice", Skills = new List<AiSkillContext>() },
            new() { FullName = "Bob", Skills = new List<AiSkillContext>() }
        };

        var filtered = _filter.FilterByCandidateSkills(null, candidates);

        Assert.Equal(2, filtered.Count);
    }
}

public class SkillMatchRankerTests
{
    private readonly SkillMatchRanker _ranker;
    private readonly ILogger<SkillMatchRanker> _logger;

    public SkillMatchRankerTests()
    {
        _logger = TestServiceFactory.CreateLogger<SkillMatchRanker>();
        _ranker = new SkillMatchRanker(_logger);
    }

    [Fact]
    public void RankAndFilterMatches_HighProficiency_RanksAboveLow()
    {
        var candidatePool = new List<AiSkillMatchCandidateContext>
        {
            new()
            {
                FullName = "Alice",
                RemainingCapacityPercentage = 50,
                Skills = new List<AiSkillContext>
                {
                    new() { SkillName = "Python", ProficiencyLevel = "ADVANCED" }
                }
            },
            new()
            {
                FullName = "Bob",
                RemainingCapacityPercentage = 50,
                Skills = new List<AiSkillContext>
                {
                    new() { SkillName = "Python", ProficiencyLevel = "BEGINNER" }
                }
            }
        };

        var response = new AiSkillMatchResponseDto
        {
            ProjectId = 1,
            Matches = new List<AiSkillMatchItemDto>
            {
                new() { EmployeeName = "Alice", SkillName = "Python", MatchScore = 80 },
                new() { EmployeeName = "Bob", SkillName = "Python", MatchScore = 80 }
            }
        };

        var ranked = _ranker.RankAndFilterMatches(response, candidatePool);

        Assert.Equal("Alice", ranked.Matches[0].EmployeeName);
        Assert.Equal("Bob", ranked.Matches[1].EmployeeName);
        Assert.True(ranked.Matches[0].MatchScore > ranked.Matches[1].MatchScore);
    }

    [Fact]
    public void RankAndFilterMatches_HallucinatedEmployee_Dropped()
    {
        var candidatePool = new List<AiSkillMatchCandidateContext>
        {
            new() { FullName = "Alice", Skills = new List<AiSkillContext>() }
        };

        var response = new AiSkillMatchResponseDto
        {
            ProjectId = 1,
            Matches = new List<AiSkillMatchItemDto>
            {
                new() { EmployeeName = "FakeEmployee", SkillName = "Python", MatchScore = 95 }
            }
        };

        var ranked = _ranker.RankAndFilterMatches(response, candidatePool);

        Assert.Empty(ranked.Matches);
    }

    [Fact]
    public void RankAndFilterMatches_BelowThreshold_Filtered()
    {
        var candidatePool = new List<AiSkillMatchCandidateContext>
        {
            new()
            {
                FullName = "Alice",
                RemainingCapacityPercentage = 0,
                Skills = new List<AiSkillContext>()
            }
        };

        var response = new AiSkillMatchResponseDto
        {
            ProjectId = 1,
            Matches = new List<AiSkillMatchItemDto>
            {
                new() { EmployeeName = "Alice", SkillName = "Python", MatchScore = 30 }
            }
        };

        var ranked = _ranker.RankAndFilterMatches(response, candidatePool);

        Assert.Empty(ranked.Matches);
    }

    [Fact]
    public void RankAndFilterMatches_CopiesRemainingCapacityPercentage()
    {
        var candidatePool = new List<AiSkillMatchCandidateContext>
        {
            new()
            {
                FullName = "Alice",
                RemainingCapacityPercentage = 75,
                Skills = new List<AiSkillContext>
                {
                    new() { SkillName = "React", ProficiencyLevel = "ADVANCED" }
                }
            }
        };

        var response = new AiSkillMatchResponseDto
        {
            ProjectId = 1,
            Matches = new List<AiSkillMatchItemDto>
            {
                new() { EmployeeName = "Alice", SkillName = "React", MatchScore = 85 }
            }
        };

        var ranked = _ranker.RankAndFilterMatches(response, candidatePool);

        Assert.Single(ranked.Matches);
        Assert.Equal(75, ranked.Matches[0].RemainingCapacityPercentage);
    }
}
