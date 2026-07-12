using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Exceptions;
using Server.Models.DTOs.Projects;
using Server.Models.Entities;
using Server.Repositories.Roles;
using Server.Repositories.Users;
using Server.Repositories.Employees;
using Server.Repositories.Allocations;
using Server.Repositories.Projects;
using Server.Repositories.Timesheets;
using Server.Services.Projects;
using Server.Services.Shared;
using Server.Services.SystemConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class SchedulerHealthServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<IMilestoneRepository> _milestoneRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IRoleRepository> _roleRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock;
    private readonly Mock<ISystemConfigService> _systemConfigServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<ProjectService>> _loggerMock;
    private readonly ProjectService _projectService;

    private readonly long _cleanProjectId = 10;
    private readonly long _overdueProjectId = 20;
    private readonly long _multiFlagProjectId = 30;

    public SchedulerHealthServiceTests()
    {
        _projectRepoMock = new Mock<IProjectRepository>();
        _milestoneRepoMock = new Mock<IMilestoneRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _roleRepoMock = new Mock<IRoleRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _timesheetRepoMock = new Mock<ITimesheetRepository>();
        _systemConfigServiceMock = new Mock<ISystemConfigService>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<ProjectService>>();

        _projectService = new ProjectService(
            _projectRepoMock.Object,
            _milestoneRepoMock.Object,
            _userRepoMock.Object,
            _roleRepoMock.Object,
            _allocationRepoMock.Object,
            _employeeRepoMock.Object,
            _timesheetRepoMock.Object,
            _systemConfigServiceMock.Object,
            ProjectHealthFlagEvaluatorTestHelper.CreateEvaluator(),
            _auditServiceMock.Object,
            _loggerMock.Object);
    }

    private void SetupDefaultRepos(List<Project> activeProjects, List<ProjectMilestone> milestones, List<ProjectAllocation> allocations, Dictionary<long, decimal> loggedHours)
    {
        _projectRepoMock.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeProjects);
        _milestoneRepoMock.Setup(r => r.GetByProjectIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(milestones);
        _systemConfigServiceMock.Setup(r => r.GetMaxWeeklyHoursAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(40m);
        _allocationRepoMock.Setup(r => r.GetAllActiveForWeekAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);
        _timesheetRepoMock.Setup(r => r.GetLoggedHoursByProjectForWeekAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(loggedHours);
    }

    [Fact]
    public async Task EvaluateAllProjectsHealthAsync_NoFlags_StaysGreen()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = _cleanProjectId, ProjectName = "Clean Project", StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6)), ProjectStatus = "ACTIVE", HealthStatus = "GREEN" }
        };
        SetupDefaultRepos(projects, new List<ProjectMilestone>(), new List<ProjectAllocation>(), new Dictionary<long, decimal>());

        // Act
        await _projectService.EvaluateAllProjectsHealthAsync();

        // Assert
        _projectRepoMock.Verify(r => r.UpdateHealthStatusAsync(_cleanProjectId, "GREEN", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAllProjectsHealthAsync_OverdueMilestone_SetsAmber()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = _overdueProjectId, ProjectName = "Overdue Project", StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6)), ProjectStatus = "ACTIVE", HealthStatus = "GREEN" }
        };
        var milestones = new List<ProjectMilestone>
        {
            new() { ProjectId = _overdueProjectId, MilestoneTitle = "Late API", DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-3)), MilestoneStatus = "IN_PROGRESS" }
        };
        SetupDefaultRepos(projects, milestones, new List<ProjectAllocation>(), new Dictionary<long, decimal>());

        // Act
        await _projectService.EvaluateAllProjectsHealthAsync();

        // Assert
        _projectRepoMock.Verify(r => r.UpdateHealthStatusAsync(_overdueProjectId, "AMBER", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAllProjectsHealthAsync_MultipleFlags_SetsRed()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = _multiFlagProjectId, ProjectName = "Multi Flag Project", StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)), ProjectStatus = "ACTIVE", HealthStatus = "GREEN" }
        };
        var milestones = new List<ProjectMilestone>
        {
            new() { ProjectId = _multiFlagProjectId, MilestoneTitle = "Late Testing", DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), MilestoneStatus = "IN_PROGRESS" },
            new() { ProjectId = _multiFlagProjectId, MilestoneTitle = "Go Live", DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)), MilestoneStatus = "NOT_STARTED" }
        };
        var allocations = new List<ProjectAllocation>
        {
            new() { ResourceProfileId = 5, ProjectId = _multiFlagProjectId, AllocationPercentage = 100m }
        };
        SetupDefaultRepos(projects, milestones, allocations, new Dictionary<long, decimal>());

        // Act
        await _projectService.EvaluateAllProjectsHealthAsync();

        // Assert
        _projectRepoMock.Verify(r => r.UpdateHealthStatusAsync(_multiFlagProjectId, "RED", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAllProjectsHealthAsync_OneProjectFails_OthersStillUpdated()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = _cleanProjectId, ProjectName = "Clean Project", StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6)), ProjectStatus = "ACTIVE", HealthStatus = "GREEN" },
            new() { Id = _overdueProjectId, ProjectName = "Overdue Project", StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-2)), EndDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(6)), ProjectStatus = "ACTIVE", HealthStatus = "GREEN" }
        };
        SetupDefaultRepos(projects, new List<ProjectMilestone>(), new List<ProjectAllocation>(), new Dictionary<long, decimal>());

        _projectRepoMock.Setup(r => r.UpdateHealthStatusAsync(_overdueProjectId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated update failure."));

        // Act
        var result = await _projectService.EvaluateAllProjectsHealthAsync();

        // Assert
        Assert.Equal(1, result.EvaluatedCount);
        Assert.Equal(1, result.FailedCount);
        _projectRepoMock.Verify(r => r.UpdateHealthStatusAsync(_cleanProjectId, "GREEN", It.IsAny<CancellationToken>()), Times.Once);
    }
}
