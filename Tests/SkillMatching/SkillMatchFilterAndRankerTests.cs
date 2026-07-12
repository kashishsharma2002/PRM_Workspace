using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Models.DTOs.SkillMatching;
using Server.Models.DTOs.SkillMatching.Context;
using Server.Services.SkillMatching;
using Tests.Helpers;

namespace Tests.SkillMatching;

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
    public void FilterByCandidateSkills_DevOpsRequirement_IncludesDevOpsCategorySkills()
    {
        var candidates = new List<AiSkillMatchCandidateContext>
        {
            new()
            {
                FullName = "Rohan Desai",
                Skills = new List<AiSkillContext>
                {
                    new() { SkillName = "Docker", Category = EmployeeConstants.DevOpsCategory, ProficiencyLevel = "INTERMEDIATE" },
                    new() { SkillName = "SQL Server", Category = EmployeeConstants.BackendCategory, ProficiencyLevel = "INTERMEDIATE" }
                }
            },
            new()
            {
                FullName = "Java Dev",
                Skills = new List<AiSkillContext>
                {
                    new() { SkillName = "Java", Category = EmployeeConstants.BackendCategory, ProficiencyLevel = "BEGINNER" }
                }
            }
        };

        var filtered = _filter.FilterByCandidateSkills("I need a devops engineer", candidates);

        Assert.Single(filtered);
        Assert.Equal("Rohan Desai", filtered[0].FullName);
    }

    [Fact]
    public void FilterByCandidateSkills_MultiRoleRequirement_IncludesBothSkillPools()
    {
        var candidates = new List<AiSkillMatchCandidateContext>
        {
            new()
            {
                FullName = "Rohan Desai",
                Skills = new List<AiSkillContext>
                {
                    new() { SkillName = "Docker", Category = EmployeeConstants.DevOpsCategory, ProficiencyLevel = "INTERMEDIATE" }
                }
            },
            new()
            {
                FullName = "Isha Verma",
                Skills = new List<AiSkillContext>
                {
                    new() { SkillName = "Java", Category = EmployeeConstants.BackendCategory, ProficiencyLevel = "BEGINNER" }
                }
            }
        };

        var filtered = _filter.FilterByCandidateSkills("I need a devops engineer and 1 java developer", candidates);

        Assert.Equal(2, filtered.Count);
    }

    [Fact]
    public void FilterByCandidateSkills_NoMatches_ReturnsEmpty()
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

        Assert.Empty(filtered);
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
        var filterLogger = TestServiceFactory.CreateLogger<SkillMatchCandidateFilter>();
        var filter = new SkillMatchCandidateFilter(filterLogger);
        _ranker = new SkillMatchRanker(filter, _logger);
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

        var ranked = _ranker.RankAndFilterMatches(response, candidatePool, "Python Developer");

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

        var ranked = _ranker.RankAndFilterMatches(response, candidatePool, "Python Developer");

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

        var ranked = _ranker.RankAndFilterMatches(response, candidatePool, "Python Developer");

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

        var ranked = _ranker.RankAndFilterMatches(response, candidatePool, "React Developer");

        Assert.Single(ranked.Matches);
        Assert.Equal(75, ranked.Matches[0].RemainingCapacityPercentage);
    }

    [Fact]
    public void RankAndFilterMatches_DevOpsCategorySkill_Accepted()
    {
        var candidatePool = new List<AiSkillMatchCandidateContext>
        {
            new()
            {
                FullName = "Rohan Desai",
                RemainingCapacityPercentage = 100,
                Skills = new List<AiSkillContext>
                {
                    new() { SkillName = "Docker", Category = EmployeeConstants.DevOpsCategory, ProficiencyLevel = "INTERMEDIATE" }
                }
            }
        };

        var response = new AiSkillMatchResponseDto
        {
            ProjectId = 0,
            Matches = new List<AiSkillMatchItemDto>
            {
                new() { EmployeeName = "Rohan Desai", SkillName = "Devops", MatchScore = 95, Reason = "Docker proficiency" }
            }
        };

        var ranked = _ranker.RankAndFilterMatches(response, candidatePool, "I need a devops engineer");

        Assert.Single(ranked.Matches);
        Assert.Equal("Docker", ranked.Matches[0].SkillName);
    }

    [Fact]
    public void RankAndFilterMatches_NonMatchingSkill_Filtered()
    {
        var candidatePool = new List<AiSkillMatchCandidateContext>
        {
            new()
            {
                FullName = "Alice",
                RemainingCapacityPercentage = 100,
                Skills = new List<AiSkillContext>
                {
                    new() { SkillName = "Java", ProficiencyLevel = "ADVANCED" }
                }
            }
        };

        var response = new AiSkillMatchResponseDto
        {
            ProjectId = 1,
            Matches = new List<AiSkillMatchItemDto>
            {
                new() { EmployeeName = "Alice", SkillName = "DevOps", MatchScore = 95 }
            }
        };

        var ranked = _ranker.RankAndFilterMatches(response, candidatePool, "DevOps Engineer");

        Assert.Empty(ranked.Matches);
    }
}
