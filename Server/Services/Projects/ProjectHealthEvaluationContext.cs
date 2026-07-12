using Server.Common;
using Server.Models.Entities;

namespace Server.Services.Projects;

public sealed class ProjectHealthEvaluationContext
{
    public required DateOnly EndDate { get; init; }

    public required IReadOnlyList<ProjectMilestone> Milestones { get; init; }

    public required decimal ExpectedHours { get; init; }

    public required decimal LoggedHours { get; init; }

    public required DateOnly Today { get; init; }

    public decimal LowHoursThreshold { get; init; } = HealthThresholdDefaults.LowHoursRatio;

    public int ApproachingDeadlineDays { get; init; } = HealthThresholdDefaults.ApproachingDeadlineDays;

    public static ProjectHealthEvaluationContext Create(
        DateOnly endDate,
        IReadOnlyList<ProjectMilestone> milestones,
        decimal expectedHours,
        decimal loggedHours,
        DateOnly today,
        decimal? lowHoursThreshold = null,
        int? approachingDeadlineDays = null) =>
        new()
        {
            EndDate = endDate,
            Milestones = milestones,
            ExpectedHours = expectedHours,
            LoggedHours = loggedHours,
            Today = today,
            LowHoursThreshold = lowHoursThreshold ?? HealthThresholdDefaults.LowHoursRatio,
            ApproachingDeadlineDays = approachingDeadlineDays ?? HealthThresholdDefaults.ApproachingDeadlineDays
        };
}
