using Server.Common;
using Server.Models.Entities;
using Server.Scheduler;

namespace Tests;

public class HealthFlagEvaluatorTests
{
    [Fact]
    public void MapToHealthStatus_ZeroFlags_ReturnsGreen()
    {
        Assert.Equal("GREEN", HealthFlagEvaluator.MapToHealthStatus([]));
    }

    [Fact]
    public void MapToHealthStatus_OneFlag_ReturnsAmber()
    {
        Assert.Equal("AMBER", HealthFlagEvaluator.MapToHealthStatus([ProjectConstants.FlagLowHours]));
    }

    [Fact]
    public void MapToHealthStatus_TwoFlags_ReturnsRed()
    {
        var flags = new[] { ProjectConstants.FlagLowHours, ProjectConstants.FlagOverdueMilestone };
        Assert.Equal("RED", HealthFlagEvaluator.MapToHealthStatus(flags));
    }

    [Fact]
    public void EvaluateFlags_OverdueMilestone_AddsFlag()
    {
        var today = new DateOnly(2026, 6, 9);
        var milestones = new List<ProjectMilestone>
        {
            new()
            {
                DueDate = today.AddDays(-1),
                MilestoneStatus = "IN_PROGRESS"
            }
        };

        var flags = HealthFlagEvaluator.EvaluateFlags(
            today.AddMonths(3), milestones, 40m, 40m, today);

        Assert.Contains(ProjectConstants.FlagOverdueMilestone, flags);
    }

    [Fact]
    public void EvaluateFlags_ApproachingDeadline_AddsFlag()
    {
        var today = new DateOnly(2026, 6, 9);
        var endDate = today.AddDays(14);
        var milestones = new List<ProjectMilestone>
        {
            new() { DueDate = endDate, MilestoneStatus = "NOT_STARTED" }
        };

        var flags = HealthFlagEvaluator.EvaluateFlags(endDate, milestones, 0m, 0m, today);

        Assert.Contains(ProjectConstants.FlagApproachingDeadline, flags);
    }
}
