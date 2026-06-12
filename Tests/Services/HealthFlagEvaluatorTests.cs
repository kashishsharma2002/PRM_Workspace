using Server.Common;
using Server.Common.Projects;
using Server.Models.Entities;
using Server.Scheduler;

namespace Tests;

public class HealthFlagEvaluatorTests
{
  private const decimal DefaultLowHoursThreshold = HealthThresholdDefaults.LowHoursRatio;
  private const int DefaultApproachingDeadlineDays = HealthThresholdDefaults.ApproachingDeadlineDays;

    [Fact]
    public void MapToHealthStatus_ZeroFlags_ReturnsGreen()
    {
        Assert.Equal(HealthStatusConstants.Green, HealthFlagEvaluator.MapToHealthStatus([]));
    }

    [Fact]
    public void MapToHealthStatus_OneFlag_ReturnsAmber()
    {
        Assert.Equal(HealthStatusConstants.Amber, HealthFlagEvaluator.MapToHealthStatus([ProjectConstants.FlagLowHours]));
    }

    [Fact]
    public void MapToHealthStatus_TwoFlags_ReturnsRed()
    {
        var flags = new[] { ProjectConstants.FlagLowHours, ProjectConstants.FlagOverdueMilestone };
        Assert.Equal(HealthStatusConstants.Red, HealthFlagEvaluator.MapToHealthStatus(flags));
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

        var flags = HealthFlagEvaluator.EvaluateFlags(
            today.AddMonths(3),
            milestones,
            40m,
            40m,
            today,
            DefaultLowHoursThreshold,
            DefaultApproachingDeadlineDays);

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

        var flags = HealthFlagEvaluator.EvaluateFlags(
            endDate,
            milestones,
            0m,
            0m,
            today,
            DefaultLowHoursThreshold,
            DefaultApproachingDeadlineDays);

        Assert.Contains(ProjectConstants.FlagApproachingDeadline, flags);
    }
}
