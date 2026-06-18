using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Server.Common;
using Server.Common.Emails;
using Server.Common.Projects;
using Server.Models.DTOs.Ai;
using Server.Models.Entities;
using Server.Repositories.Allocations;
using Server.Repositories.Emails;
using Server.Repositories.Projects;
using Server.Repositories.SystemConfig;
using Server.Repositories.Timesheets;
using Server.Repositories.Users;
using Server.Services.Ai.Abstractions;
using Server.Services.Emails;
using Server.Services.Projects;

namespace Tests.Services;

public class ProjectHealthServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepoMock = new();
    private readonly Mock<IMilestoneRepository> _milestoneRepoMock = new();
    private readonly Mock<IAllocationRepository> _allocationRepoMock = new();
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock = new();
    private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IEmailLogRepository> _emailLogRepoMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IAiIntegrationService> _aiServiceMock = new();

    private readonly ProjectHealthService _service;

    public ProjectHealthServiceTests()
    {
        _service = new ProjectHealthService(
            _projectRepoMock.Object,
            _milestoneRepoMock.Object,
            _allocationRepoMock.Object,
            _timesheetRepoMock.Object,
            _systemConfigRepoMock.Object,
            _userRepoMock.Object,
            _emailLogRepoMock.Object,
            _emailServiceMock.Object,
            _aiServiceMock.Object,
            NullLogger<ProjectHealthService>.Instance);
    }

    [Fact]
    public async Task ProcessProjectHealthNotificationsAsync_IncludesAvailabilityInResourceList()
    {
        var project = new Project
        {
            Id = 2,
            ProjectName = "Beta CRM",
            ManagerUserId = 10,
            HealthStatus = HealthStatusConstants.Green,
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(3)),
            ProjectStatus = ProjectStatusConstants.Active
        };

        var overdueMilestone = new ProjectMilestone
        {
            ProjectId = 2,
            MilestoneTitle = "UAT",
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7)),
            MilestoneStatus = MilestoneStatusConstants.NotStarted
        };

        _projectRepoMock.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([project]);
        _milestoneRepoMock.Setup(r => r.GetByProjectIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([overdueMilestone]);
        _systemConfigRepoMock.Setup(r => r.GetByKeyAsync(ConfigKeys.MaxWeeklyHours, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemConfiguration { ConfigKey = ConfigKeys.MaxWeeklyHours, ConfigValue = "40" });
        _allocationRepoMock.Setup(r => r.GetAllActiveForWeekAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ProjectAllocation
                {
                    ProjectId = 2,
                    ResourceProfileId = 1,
                    AllocationPercentage = 100,
                    AllocationStatus = "ACTIVE"
                }
            ]);
        _timesheetRepoMock.Setup(r => r.GetLoggedHoursByProjectForWeekAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, decimal> { [2] = 0m });
        _userRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 10, FullName = "Manager", Email = "mgr@test.com" });
        _emailLogRepoMock.Setup(r => r.WasSentForReferenceAsync(
                It.IsAny<string>(), EmailTypeConstants.ProjectAtRisk, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _aiServiceMock.Setup(a => a.GetRiskSummaryAsync(10, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiRiskSummaryResponseDto { Summary = "Risk", Recommendations = [] });
        _aiServiceMock.Setup(a => a.GetSkillMatchAsync(
                10,
                2,
                It.IsAny<string>(),
                It.Is<SkillMatchOptions>(o => o.ExcludeAllocatedToProjectId == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiSkillMatchResponseDto
            {
                ProjectId = 2,
                Matches =
                [
                    new AiSkillMatchItemDto
                    {
                        EmployeeName = "Priya Sharma",
                        SkillName = "React",
                        MatchScore = 88,
                        RemainingCapacityPercentage = 100
                    },
                    new AiSkillMatchItemDto
                    {
                        EmployeeName = "Ravi Kumar",
                        SkillName = ".NET",
                        MatchScore = 76,
                        RemainingCapacityPercentage = 40
                    }
                ]
            });

        Dictionary<string, string>? capturedPlaceholders = null;
        _emailServiceMock.Setup(e => e.SendNotificationAsync(
                It.IsAny<string>(),
                EmailTypeConstants.ProjectAtRisk,
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, Dictionary<string, string>, string, string, CancellationToken>(
                (_, _, placeholders, _, _, _) => capturedPlaceholders = placeholders)
            .ReturnsAsync(true);

        await _service.ProcessProjectHealthNotificationsAsync();

        Assert.NotNull(capturedPlaceholders);
        var resourceList = capturedPlaceholders!["ResourceList"];
        Assert.Contains("Priya Sharma (React, score 88) — Fully available", resourceList);
        Assert.Contains("Ravi Kumar (.NET, score 76) — 40% available", resourceList);
    }
}
