using Server.Common;
using Server.Common.Projects;
using Server.Models.Entities;
using Server.Services.Projects;
using Server.Services.Projects.Abstractions;

namespace Tests;

public class ProjectHealthFlagEvaluatorTests
{
    private readonly IProjectHealthFlagEvaluator _evaluator = ProjectHealthFlagEvaluatorTestHelper.CreateEvaluator();

    [Fact]
    public void MapToHealthStatus_ZeroFlags_ReturnsGreen()
    {
        Assert.Equal(HealthStatusConstants.Green, _evaluator.MapToHealthStatus([]));
    }

    [Fact]
    public void MapToHealthStatus_OneFlag_ReturnsAmber()
    {
        Assert.Equal(HealthStatusConstants.Amber, _evaluator.MapToHealthStatus([ProjectConstants.FlagLowHours]));
    }

    [Fact]
    public void MapToHealthStatus_TwoFlags_ReturnsRed()
    {
        var flags = new[] { ProjectConstants.FlagLowHours, ProjectConstants.FlagOverdueMilestone };
        Assert.Equal(HealthStatusConstants.Red, _evaluator.MapToHealthStatus(flags));
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
                MilestoneStatus = MilestoneStatusConstants.InProgress
            }
        };

        var context = ProjectHealthEvaluationContext.Create(
            today.AddMonths(3),
            milestones,
            40m,
            40m,
            today);

        var flags = _evaluator.EvaluateFlags(context);

        Assert.Contains(ProjectConstants.FlagOverdueMilestone, flags);
    }

    [Fact]
    public void EvaluateFlags_ApproachingDeadline_AddsFlag()
    {
        var today = new DateOnly(2026, 6, 9);
        var endDate = today.AddDays(14);
        var milestones = new List<ProjectMilestone>
        {
            new() { DueDate = endDate, MilestoneStatus = MilestoneStatusConstants.NotStarted }
        };

        var context = ProjectHealthEvaluationContext.Create(
            endDate,
            milestones,
            0m,
            0m,
            today);

        var flags = _evaluator.EvaluateFlags(context);

        Assert.Contains(ProjectConstants.FlagApproachingDeadline, flags);
    }

    [Fact]
    public void EvaluateFlags_LowHours_AddsFlag()
    {
        var today = new DateOnly(2026, 6, 9);
        var context = ProjectHealthEvaluationContext.Create(
            today.AddMonths(3),
            [],
            expectedHours: 40m,
            loggedHours: 10m,
            today);

        var flags = _evaluator.EvaluateFlags(context);

        Assert.Contains(ProjectConstants.FlagLowHours, flags);
    }
}
