using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Common.Allocations;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Employees;
using Server.Models.Entities;
using Server.Repositories.Roles;
using Server.Repositories.Users;
using Server.Repositories.Employees;
using Server.Repositories.Allocations;
using Server.Repositories.Projects;
using Server.Repositories.Timesheets;
using Server.Services.Shared;
using Server.Services.Employees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class EmployeeServiceTeamTests
{
    private readonly Mock<IDbTransactionManager> _transactionManagerMock;
    private readonly Mock<IDbTransaction> _transactionMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IRoleRepository> _roleRepoMock;
    private readonly Mock<ISkillRepository> _skillRepoMock;
    private readonly Mock<IEmployeeSkillRepository> _employeeSkillRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<EmployeeService>> _loggerMock;
    private readonly EmployeeService _employeeService;

    public EmployeeServiceTeamTests()
    {
        _transactionMock = new Mock<IDbTransaction>();
        _transactionManagerMock = new Mock<IDbTransactionManager>();

        _transactionManagerMock.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _roleRepoMock = new Mock<IRoleRepository>();
        _skillRepoMock = new Mock<ISkillRepository>();
        _employeeSkillRepoMock = new Mock<IEmployeeSkillRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _timesheetRepoMock = new Mock<ITimesheetRepository>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<EmployeeService>>();

        _employeeService = new EmployeeService(
            _transactionManagerMock.Object,
            _employeeRepoMock.Object,
            _userRepoMock.Object,
            _roleRepoMock.Object,
            _skillRepoMock.Object,
            _employeeSkillRepoMock.Object,
            _allocationRepoMock.Object,
            _projectRepoMock.Object,
            _timesheetRepoMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetTeamDashboardAsync_SplitsBenchAndActive()
    {
        // Arrange
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

        // Act
        var dashboard = await _employeeService.GetTeamDashboardAsync(managerUserId);

        // Assert
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
        // Arrange
        var managerUserId = 1;
        var profile = new ResourceProfile { Id = 10, UserId = 101, ManagerId = 2 }; // manager ID mismatch

        _employeeRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _employeeService.GetTeamMemberDetailAsync(managerUserId, 10));
    }
}
