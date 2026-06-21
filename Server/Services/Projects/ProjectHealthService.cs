using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Common.Emails;
using Server.Common.Projects;
using Server.Common.Timesheets;
using Server.Models.DTOs.Ai;
using Server.Models.DTOs.Scheduler;
using Server.Repositories.Allocations;
using Server.Repositories.Emails;
using Server.Repositories.Projects;
using Server.Repositories.SystemConfig;
using Server.Repositories.Timesheets;
using Server.Repositories.Users;
using Server.Scheduler;
using Server.Services.Ai.Abstractions;
using Server.Services.Emails;
using Server.Services.SystemConfig;

namespace Server.Services.Projects;

public class ProjectHealthService(
    IProjectRepository projectRepository,
    IMilestoneRepository milestoneRepository,
    IAllocationRepository allocationRepository,
    ITimesheetRepository timesheetRepository,
    ISystemConfigService systemConfigService,
    IUserRepository userRepository,
    IEmailLogRepository emailLogRepository,
    IEmailService emailService,
    IAiIntegrationService aiIntegrationService,
    ILogger<ProjectHealthService> logger) : IProjectHealthService
{
    public async Task<SchedulerHealthResultDto> ProcessProjectHealthNotificationsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lastWeek = WeekDateHelper.GetMostRecentCompletedWeekMonday();
        var weekEnd = WeekDateHelper.GetWeekEnd(lastWeek);

        var projects = await projectRepository.GetActiveAsync(cancellationToken);
        var projectIds = projects.Select(p => p.Id).ToList();
        var milestones = await milestoneRepository.GetByProjectIdsAsync(projectIds, cancellationToken);
        var milestonesByProject = milestones.GroupBy(m => m.ProjectId).ToDictionary(g => g.Key, g => g.ToList());

        var maxWeeklyHours = await systemConfigService.GetMaxWeeklyHoursAsync(cancellationToken);
        var allocations = await allocationRepository.GetAllActiveForWeekAsync(lastWeek, weekEnd, cancellationToken);
        var allocationsByProject = allocations.GroupBy(a => a.ProjectId).ToDictionary(g => g.Key, g => g.ToList());
        var loggedHoursByProject = await timesheetRepository.GetLoggedHoursByProjectForWeekAsync(lastWeek, cancellationToken);

        var evaluatedCount = 0;
        var failedCount = 0;
        var emailsSent = 0;
        var emailsFailed = 0;
        var riskAnalysesCompleted = 0;
        var riskAnalysesFailed = 0;

        foreach (var project in projects)
        {
            try
            {
                var previousHealth = project.HealthStatus;
                var projectMilestones = milestonesByProject.GetValueOrDefault(project.Id, []);
                var projectAllocations = allocationsByProject.GetValueOrDefault(project.Id, []);
                var expectedHours = projectAllocations.Sum(a => a.AllocationPercentage / 100m * maxWeeklyHours);
                var loggedHours = loggedHoursByProject.GetValueOrDefault(project.Id, 0m);

                var flags = HealthFlagEvaluator.EvaluateFlags(
                    project.EndDate,
                    projectMilestones,
                    expectedHours,
                    loggedHours,
                    today,
                    HealthThresholdDefaults.LowHoursRatio,
                    HealthThresholdDefaults.ApproachingDeadlineDays);
                var newHealth = HealthFlagEvaluator.MapToHealthStatus(flags);

                if (!string.Equals(previousHealth, newHealth, StringComparison.OrdinalIgnoreCase))
                    await projectRepository.UpdateHealthStatusAsync(project.Id, newHealth, cancellationToken);

                if (!string.Equals(previousHealth, HealthStatusConstants.Red, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(newHealth, HealthStatusConstants.Red, StringComparison.OrdinalIgnoreCase))
                {
                    var notificationResult = await SendAtRiskNotificationAsync(
                        project, projectMilestones, newHealth, cancellationToken);
                    emailsSent += notificationResult.EmailsSent;
                    emailsFailed += notificationResult.EmailsFailed;
                    riskAnalysesCompleted += notificationResult.RiskAnalysesCompleted;
                    riskAnalysesFailed += notificationResult.RiskAnalysesFailed;
                }

                evaluatedCount++;
            }
            catch (Exception ex)
            {
                failedCount++;
                logger.LogWarning(ex, "Project health notification failed for project {ProjectId}", project.Id);
            }
        }

        if (evaluatedCount > 0)
            await projectRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Project health processed. Evaluated={Evaluated}, Failed={Failed}, EmailsSent={EmailsSent}, EmailsFailed={EmailsFailed}, RiskAnalysesCompleted={RiskCompleted}, RiskAnalysesFailed={RiskFailed}",
            evaluatedCount,
            failedCount,
            emailsSent,
            emailsFailed,
            riskAnalysesCompleted,
            riskAnalysesFailed);

        return new SchedulerHealthResultDto
        {
            EvaluatedCount = evaluatedCount,
            FailedCount = failedCount,
            EmailsSent = emailsSent,
            EmailsFailed = emailsFailed,
            RiskAnalysesCompleted = riskAnalysesCompleted,
            RiskAnalysesFailed = riskAnalysesFailed
        };
    }

    private sealed record AtRiskNotificationResult(
        int EmailsSent,
        int EmailsFailed,
        int RiskAnalysesCompleted,
        int RiskAnalysesFailed);

    private async Task<AtRiskNotificationResult> SendAtRiskNotificationAsync(
        Models.Entities.Project project,
        IReadOnlyList<Models.Entities.ProjectMilestone> milestones,
        string healthStatus,
        CancellationToken cancellationToken)
    {
        var manager = await userRepository.GetByIdAsync(project.ManagerUserId, cancellationToken);
        if (manager is null || string.IsNullOrWhiteSpace(manager.Email))
            return new AtRiskNotificationResult(0, 0, 0, 0);

        var entityReference = $"Project:{project.Id}";
        var alreadySent = await emailLogRepository.WasSentForReferenceAsync(
            manager.Email,
            EmailTypeConstants.ProjectAtRisk,
            entityReference,
            cancellationToken);
        if (alreadySent)
            return new AtRiskNotificationResult(0, 0, 0, 0);

        var (riskSummary, riskAnalysisSucceeded) = await BuildRiskSummaryAsync(project, cancellationToken);
        var resourceList = await BuildResourceListAsync(project, cancellationToken);
        var milestoneSummary = BuildMilestoneSummary(milestones);

        var placeholders = new Dictionary<string, string>
        {
            ["ManagerName"] = manager.FullName,
            ["ProjectName"] = project.ProjectName,
            ["HealthStatus"] = FormatHealthStatus(healthStatus),
            ["Milestones"] = milestoneSummary,
            ["RiskSummary"] = riskSummary,
            ["ResourceList"] = resourceList
        };

        var sent = await emailService.SendNotificationAsync(
            manager.Email,
            EmailTypeConstants.ProjectAtRisk,
            placeholders,
            entityReference,
            $"project-risk-{project.Id}-{DateTime.UtcNow:yyyyMMdd}",
            cancellationToken);

        return new AtRiskNotificationResult(
            EmailsSent: sent ? 1 : 0,
            EmailsFailed: sent ? 0 : 1,
            RiskAnalysesCompleted: riskAnalysisSucceeded ? 1 : 0,
            RiskAnalysesFailed: riskAnalysisSucceeded ? 0 : 1);
    }

    private static string FormatHealthStatus(string healthStatus) => healthStatus.ToUpperInvariant() switch
    {
        HealthStatusConstants.Red => "RED (At Risk)",
        HealthStatusConstants.Amber => "AMBER (Watch)",
        HealthStatusConstants.Green => "GREEN (Healthy)",
        _ => healthStatus
    };

    private static string BuildMilestoneSummary(IReadOnlyList<Models.Entities.ProjectMilestone> milestones)
    {
        if (milestones.Count == 0)
            return "No milestones defined for this project.";

        return string.Join(
            Environment.NewLine,
            milestones
                .OrderBy(m => m.DueDate)
                .Take(EmailDefaults.MaxMilestonesInHealthEmail)
                .Select(m =>
                {
                    var status = m.CompletedAt.HasValue ? "Completed" : "Pending";
                    return $"• {m.MilestoneTitle} — due {m.DueDate:yyyy-MM-dd} ({status})";
                }));
    }

    private async Task<(string Summary, bool RiskAnalysisSucceeded)> BuildRiskSummaryAsync(
        Models.Entities.Project project,
        CancellationToken cancellationToken)
    {
        try
        {
            var risk = await aiIntegrationService.GetRiskSummaryAsync(
                project.ManagerUserId, project.Id, cancellationToken);

            if (risk.Recommendations.Count == 0)
                return (risk.Summary, true);

            var recommendations = string.Join(Environment.NewLine, risk.Recommendations.Select(r => $"• {r}"));
            return ($"{risk.Summary}{Environment.NewLine}{Environment.NewLine}{recommendations}", true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI risk summary unavailable for project {ProjectId}", project.Id);
            return ("AI risk analysis is currently unavailable. Please review project milestones and resource utilization.", false);
        }
    }

    private async Task<string> BuildResourceListAsync(
        Models.Entities.Project project,
        CancellationToken cancellationToken)
    {
        try
        {
            var matches = await aiIntegrationService.GetSkillMatchAsync(
                project.ManagerUserId,
                project.Id,
                "Recommend unallocated available resources to mitigate project risks.",
                new SkillMatchOptions { ExcludeAllocatedToProjectId = project.Id },
                cancellationToken);

            if (matches.Matches.Count == 0)
                return "No resource recommendations available at this time.";

            return string.Join(
                Environment.NewLine,
                matches.Matches.Take(EmailDefaults.MaxResourceRecommendationsInHealthEmail).Select(m =>
                    $"• {m.EmployeeName} ({m.SkillName}, score {m.MatchScore}) — {FormatAvailability(m.RemainingCapacityPercentage)}"));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI resource recommendations unavailable for project {ProjectId}", project.Id);
            return "Resource recommendations are currently unavailable.";
        }
    }

    private static string FormatAvailability(decimal remainingCapacityPercentage) =>
        remainingCapacityPercentage >= AllocationConstants.MaxUtilizationPercentage
            ? "Fully available"
            : $"{remainingCapacityPercentage:0.#}% available";
}
