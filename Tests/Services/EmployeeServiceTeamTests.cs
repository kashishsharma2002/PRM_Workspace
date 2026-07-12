using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Common.Allocations;
using Server.Exceptions;
using Server.Models.DTOs.Employees;
using Server.Models.Entities;
using Server.Repositories.Allocations;
using Server.Repositories.Employees;
using Server.Repositories.Projects;
using Server.Repositories.Timesheets;
using Server.Repositories.Users;
using Server.Services.Employees;
using Server.Services.Shared;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Services;

public class EmployeeServiceTeamTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<ISkillRepository> _skillRepoMock;
    private readonly Mock<IEmployeeSkillRepository> _employeeSkillRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly EmployeeTeamService _employeeTeamService;

    public EmployeeServiceTeamTests()
    {
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _skillRepoMock = new Mock<ISkillRepository>();
        _employeeSkillRepoMock = new Mock<IEmployeeSkillRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _timesheetRepoMock = new Mock<ITimesheetRepository>();
        _auditServiceMock = new Mock<IAuditService>();

        _employeeTeamService = new EmployeeTeamService(
            _employeeRepoMock.Object,
            _userRepoMock.Object,
            _skillRepoMock.Object,
            _employeeSkillRepoMock.Object,
            _allocationRepoMock.Object,
            _projectRepoMock.Object,
            _timesheetRepoMock.Object,
            _auditServiceMock.Object,
            new Mock<ILogger<EmployeeTeamService>>().Object);
    }

    [Fact]
    public async Task GetTeamDashboardAsync_SplitsBenchAndActive()
    {
        var managerUserId = 1;
        var team = new List<ResourceProfile>
        {
            new() { Id = 10, UserId = 101, ManagerId = managerUserId },
            new() { Id = 20, UserId = 102, ManagerId = managerUserId }
        };

        var usersDict = new Dictionary<long, User>
        {
            { 101, new User { Id = 101, FullName = "bench.user", Department = "DEV" } },
            { 102, new User { Id = 102, FullName = "active.user", Department = "DEV" } }
        };

        var activeAllocations = new List<ProjectAllocation>
        {
            new() { ResourceProfileId = 20, AllocationPercentage = 50, AllocationStatus = "ACTIVE" }
        };

        _employeeRepoMock.Setup(r => r.GetByManagerIdAsync(managerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _userRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersDict);
        _employeeSkillRepoMock.Setup(r => r.GetByUserIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserSkill>());
        _skillRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, Skill>());
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeAllocations);

        var dashboard = await _employeeTeamService.GetTeamDashboardAsync(managerUserId);

        Assert.Equal(1, dashboard.BenchCount);
        Assert.Single(dashboard.BenchEmployees);
        Assert.Equal(10, dashboard.BenchEmployees[0].Id);
        Assert.Single(dashboard.ActiveEmployees);
        Assert.Equal(20, dashboard.ActiveEmployees[0].Id);
        Assert.Equal(1, dashboard.PartialCount);
    }

    [Fact]
    public async Task GetTeamMemberDetailAsync_RejectsOutOfScopeEmployee()
    {
        var managerUserId = 1;
        var profile = new ResourceProfile { Id = 10, UserId = 101, ManagerId = 2 };

        _employeeRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _employeeTeamService.GetTeamMemberDetailAsync(managerUserId, 10));
    }
}
