using Server.Models.DTOs.Ai.Context;
using Server.Services.Ai;

namespace Tests.AI;

public class ProjectHealthResourceFilterTests
{
    private readonly ProjectHealthResourceFilter _filter = new();

    [Fact]
    public void FilterForAtRiskEmail_ExcludesEmployeesAllocatedToProject()
    {
        var candidates = new List<AiSkillMatchCandidateContext>
        {
            new()
            {
                FullName = "On Project",
                RemainingCapacityPercentage = 50,
                ActiveAllocations =
                [
                    new AiAllocationContext { ProjectId = 2, AllocationPercentage = 50 }
                ]
            },
            new()
            {
                FullName = "Available Elsewhere",
                RemainingCapacityPercentage = 100,
                ActiveAllocations =
                [
                    new AiAllocationContext { ProjectId = 99, AllocationPercentage = 0 }
                ]
            }
        };

        var filtered = _filter.FilterForAtRiskEmail(candidates, projectId: 2);

        Assert.Single(filtered);
        Assert.Equal("Available Elsewhere", filtered[0].FullName);
    }

    [Fact]
    public void FilterForAtRiskEmail_ExcludesFullyAllocatedEmployees()
    {
        var candidates = new List<AiSkillMatchCandidateContext>
        {
            new()
            {
                FullName = "No Capacity",
                RemainingCapacityPercentage = 0,
                ActiveAllocations =
                [
                    new AiAllocationContext { ProjectId = 99, AllocationPercentage = 100 }
                ]
            },
            new()
            {
                FullName = "Bench",
                RemainingCapacityPercentage = 100,
                ActiveAllocations = []
            }
        };

        var filtered = _filter.FilterForAtRiskEmail(candidates, projectId: 2);

        Assert.Single(filtered);
        Assert.Equal("Bench", filtered[0].FullName);
    }

    [Fact]
    public void FilterForAtRiskEmail_KeepsPartiallyAvailable_NotOnProject()
    {
        var candidates = new List<AiSkillMatchCandidateContext>
        {
            new()
            {
                FullName = "Partial",
                RemainingCapacityPercentage = 40,
                ActiveAllocations =
                [
                    new AiAllocationContext { ProjectId = 5, AllocationPercentage = 60 }
                ]
            }
        };

        var filtered = _filter.FilterForAtRiskEmail(candidates, projectId: 2);

        Assert.Single(filtered);
        Assert.Equal(40, filtered[0].RemainingCapacityPercentage);
    }

    [Fact]
    public void FilterForAtRiskEmail_OrdersByRemainingCapacityDescending()
    {
        var candidates = new List<AiSkillMatchCandidateContext>
        {
            new() { FullName = "Low", RemainingCapacityPercentage = 20, ActiveAllocations = [] },
            new() { FullName = "High", RemainingCapacityPercentage = 100, ActiveAllocations = [] },
            new() { FullName = "Mid", RemainingCapacityPercentage = 60, ActiveAllocations = [] }
        };

        var filtered = _filter.FilterForAtRiskEmail(candidates, projectId: 2);

        Assert.Equal(["High", "Mid", "Low"], filtered.Select(c => c.FullName).ToList());
    }
}
