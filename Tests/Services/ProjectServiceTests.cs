using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Common.Projects;
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
using Server.Repositories.SystemConfig;
using Server.Services.Shared;
using Server.Services.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<IMilestoneRepository> _milestoneRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IRoleRepository> _roleRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock;
    private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<ProjectService>> _loggerMock;
    private readonly ProjectService _projectService;

    private readonly long _managerId = 20;
    private readonly long _projectId = 100;

    public ProjectServiceTests()
    {
        _projectRepoMock = new Mock<IProjectRepository>();
        _milestoneRepoMock = new Mock<IMilestoneRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _roleRepoMock = new Mock<IRoleRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _timesheetRepoMock = new Mock<ITimesheetRepository>();
        _systemConfigRepoMock = new Mock<ISystemConfigRepository>();
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
            _systemConfigRepoMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateProjectAsync_InvalidDateRange_ThrowsValidation()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ValidationAppException>(() =>
            _projectService.CreateProjectAsync(1, new CreateProjectRequestDto
            {
                ProjectName = "Test Project",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 5, 1),
                ProjectStatus = "ACTIVE",
                ManagerUserId = _managerId,
                TotalStoryPoints = 100
            }));
    }

    [Fact]
    public async Task CreateProjectAsync_CreatesWithProjectCode()
    {
        // Arrange
        var request = new CreateProjectRequestDto
        {
            ProjectName = "Project Alpha",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            ProjectStatus = "ACTIVE",
            ManagerUserId = _managerId,
            TotalStoryPoints = 120
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(_managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = _managerId, IsActive = true });
        _roleRepoMock.Setup(r => r.UserHasRoleAsync(_managerId, RoleConstants.Manager, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _projectRepoMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, c) => p.Id = 15);

        // Act
        var result = await _projectService.CreateProjectAsync(1, request);

        // Assert
        Assert.Equal(15, result.ProjectId);
        Assert.Equal("PRJ-000015", result.ProjectCode);
        _projectRepoMock.Verify(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
        _projectRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
        _projectRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AddMilestoneAsync_DueDateOutsideRange_ThrowsValidation()
    {
        // Arrange
        var project = new Project { Id = _projectId, StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 6, 30) };
        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationAppException>(() =>
            _projectService.AddMilestoneAsync(_projectId, new CreateMilestoneRequestDto
            {
                MilestoneTitle = "Late Milestone",
                DueDate = new DateOnly(2026, 12, 1),
                StoryPoints = 20
            }));
    }

    [Fact]
    public async Task GetAllProjectsAsync_StoryPointsDone_SumsDoneMilestones()
    {
        // Arrange
        var projects = new List<Project>
        {
            new() { Id = _projectId, ProjectName = "SP Test", ManagerUserId = _managerId, TotalStoryPoints = 100, EndDate = new DateOnly(2026, 12, 31) }
        };
        var milestones = new List<ProjectMilestone>
        {
            new() { Id = 1, ProjectId = _projectId, MilestoneTitle = "Done Work", StoryPoints = 20, MilestoneStatus = "DONE" },
            new() { Id = 2, ProjectId = _projectId, MilestoneTitle = "Pending Work", StoryPoints = 30, MilestoneStatus = "IN_PROGRESS" }
        };

        _projectRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);
        _userRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, User> { { _managerId, new User { Id = _managerId, FullName = "Mgr" } } });
        _milestoneRepoMock.Setup(r => r.GetByProjectIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(milestones);

        // Act
        var list = await _projectService.GetAllProjectsAsync();

        // Assert
        var item = list.Projects.First();
        Assert.Equal(20, item.StoryPointsDone);
        Assert.Equal(100, item.TotalStoryPoints);
    }
}
