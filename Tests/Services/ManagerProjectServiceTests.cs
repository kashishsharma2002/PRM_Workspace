using Microsoft.Extensions.Logging;
using Moq;
using Server.Common.Roles;
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
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class ManagerProjectServiceTests
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

    private readonly long _managerAUserId = 1;
    private readonly long _managerBUserId = 2;
    private readonly long _managerAProjectId = 10;

    public ManagerProjectServiceTests()
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
            _auditServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetMyProjectsAsync_ReturnsOnlyOwnedProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = _managerAProjectId, ProjectName = "Project A", ManagerUserId = _managerAUserId, HealthStatus = "GREEN" },
            new() { Id = 11, ProjectName = "Project B", ManagerUserId = _managerAUserId, HealthStatus = "GREEN" }
        };

        _projectRepoMock.Setup(r => r.GetByManagerUserIdAsync(_managerAUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        // Act
        var result = await _projectService.GetMyProjectsAsync(_managerAUserId);

        // Assert
        Assert.Equal(2, result.Projects.Count);
        Assert.All(result.Projects, p => Assert.Contains(p.ProjectName, new[] { "Project A", "Project B" }));
    }

    [Fact]
    public async Task GetManagerProjectDetailAsync_OtherManagerProject_ThrowsNotFound()
    {
        // Arrange
        var project = new Project { Id = _managerAProjectId, ManagerUserId = _managerAUserId };
        _projectRepoMock.Setup(r => r.GetByIdAsync(_managerAProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundAppException>(
            () => _projectService.GetManagerProjectDetailAsync(_managerBUserId, _managerAProjectId));
    }

    [Fact]
    public async Task GetManagerProjectDetailAsync_OverdueMilestone_SetsRiskFlag()
    {
        // Arrange
        var project = new Project { Id = _managerAProjectId, ManagerUserId = _managerAUserId, ProjectCode = "PRJ-123", ProjectName = "Project A", HealthStatus = "GREEN", EndDate = new DateOnly(2026, 12, 31) };
        var milestones = new List<ProjectMilestone>
        {
            new() { ProjectId = _managerAProjectId, MilestoneTitle = "Backend API", DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)), MilestoneStatus = "IN_PROGRESS" }
        };

        _projectRepoMock.Setup(r => r.GetByIdAsync(_managerAProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _milestoneRepoMock.Setup(r => r.GetByProjectIdAsync(_managerAProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(milestones);
        _allocationRepoMock.Setup(r => r.GetActiveByProjectIdAsync(_managerAProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectAllocation>());
        _systemConfigServiceMock.Setup(r => r.GetMaxWeeklyHoursAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(40m);

        // Act
        var detail = await _projectService.GetManagerProjectDetailAsync(_managerAUserId, _managerAProjectId);

        // Assert
        Assert.Contains("OVERDUE_MILESTONE", detail.RiskFlags);
        Assert.Contains(detail.Milestones, m => m.IsOverdue);
    }
}
