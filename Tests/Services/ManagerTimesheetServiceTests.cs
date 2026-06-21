using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Common.Roles;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Timesheets;
using Server.Models.Entities;
using Server.Repositories.Roles;
using Server.Repositories.Users;
using Server.Repositories.Employees;
using Server.Repositories.Allocations;
using Server.Repositories.Projects;
using Server.Repositories.Timesheets;
using Server.Services.Shared;
using Server.Services.SystemConfig;
using Server.Services.Timesheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class ManagerTimesheetServiceTests
{
    private readonly Mock<IDbTransactionManager> _transactionManagerMock;
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IActivityTagRepository> _activityTagRepoMock;
    private readonly Mock<ISystemConfigService> _systemConfigServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly TimesheetService _timesheetService;

    private readonly long _managerAUserId = 1;
    private readonly long _managerBUserId = 2;
    private readonly long _employeeAProfileId = 10;
    private readonly long _projectId = 100;
    private readonly string _employeeAFullName = "Employee A";
    private readonly string _projectName = "Project A";
    private readonly DateOnly _weekStart = new(2026, 5, 12);

    public ManagerTimesheetServiceTests()
    {
        var transactionMock = new Mock<IDbTransaction>();
        _transactionManagerMock = new Mock<IDbTransactionManager>();
        _transactionManagerMock.Setup(m => m.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _timesheetRepoMock = new Mock<ITimesheetRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _activityTagRepoMock = new Mock<IActivityTagRepository>();
        _systemConfigServiceMock = new Mock<ISystemConfigService>();
        _auditServiceMock = new Mock<IAuditService>();
        var schedulerTimesheetServiceMock = new Mock<ISchedulerTimesheetService>();

        _timesheetService = new TimesheetService(
            _transactionManagerMock.Object,
            _timesheetRepoMock.Object,
            _allocationRepoMock.Object,
            _projectRepoMock.Object,
            _employeeRepoMock.Object,
            _userRepoMock.Object,
            _activityTagRepoMock.Object,
            _systemConfigServiceMock.Object,
            _auditServiceMock.Object,
            schedulerTimesheetServiceMock.Object,
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<ILogger<TimesheetService>>().Object);
    }

    [Fact]
    public async Task GetTeamTimesheetsAsync_SubmittedTimesheet_ShowsLineItems()
    {
        // Arrange
        var team = new List<ResourceProfile>
        {
            new() { Id = _employeeAProfileId, UserId = 101, ManagerId = _managerAUserId }
        };
        var users = new Dictionary<long, User>
        {
            { 101, new User { Id = 101, FullName = _employeeAFullName } }
        };
        var allocations = new List<ProjectAllocation>
        {
            new() { Id = 50, ResourceProfileId = _employeeAProfileId, ProjectId = _projectId, AllocationPercentage = 50, AllocationStatus = "ACTIVE" }
        };
        var timesheets = new List<Timesheet>
        {
            new() { Id = 500, ResourceProfileId = _employeeAProfileId, WeekStartDate = _weekStart, TotalHours = 18, Status = "SUBMITTED" }
        };
        var lineItems = new List<TimesheetLineItem>
        {
            new() { Id = 5000, TimesheetId = 500, ProjectId = _projectId, HoursLogged = 18 }
        };

        _employeeRepoMock.Setup(r => r.GetByManagerIdAsync(_managerAUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _userRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdsForWeekAsync(It.IsAny<IEnumerable<long>>(), _weekStart, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);
        _timesheetRepoMock.Setup(r => r.GetByEmployeeIdsAndWeekAsync(It.IsAny<IEnumerable<long>>(), _weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timesheets);
        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = _projectId, ProjectName = _projectName });
        _timesheetRepoMock.Setup(r => r.GetLineItemsByTimesheetIdAsync(500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lineItems);

        // Act
        var result = await _timesheetService.GetTeamTimesheetsAsync(_managerAUserId, _weekStart);

        // Assert
        Assert.Single(result.Rows);
        Assert.Equal(_employeeAFullName, result.Rows[0].EmployeeName);
        Assert.Equal(_projectName, result.Rows[0].ProjectName);
        Assert.Equal(18, result.Rows[0].HoursLogged);
        Assert.Equal("SUBMITTED", result.Rows[0].Status);
    }

    [Fact]
    public async Task GetTeamTimesheetsAsync_ReturnsOnlyTeamEmployees()
    {
        // Arrange
        var teamA = new List<ResourceProfile> { new() { Id = _employeeAProfileId, UserId = 101, ManagerId = _managerAUserId } };
        var usersA = new Dictionary<long, User> { { 101, new User { Id = 101, FullName = _employeeAFullName } } };
        var allocationsA = new List<ProjectAllocation> { new() { Id = 50, ResourceProfileId = _employeeAProfileId, ProjectId = _projectId, AllocationPercentage = 50 } };
        var timesheetsA = new List<Timesheet> { new() { Id = 500, ResourceProfileId = _employeeAProfileId, WeekStartDate = _weekStart, TotalHours = 10, Status = "SUBMITTED" } };
        var lineItemsA = new List<TimesheetLineItem> { new() { Id = 5000, TimesheetId = 500, ProjectId = _projectId, HoursLogged = 10 } };

        _employeeRepoMock.Setup(r => r.GetByManagerIdAsync(_managerAUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teamA);
        _userRepoMock.Setup(r => r.GetByIdsAsync(new List<long> { 101 }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersA);
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdsForWeekAsync(new List<long> { _employeeAProfileId }, _weekStart, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocationsA);
        _timesheetRepoMock.Setup(r => r.GetByEmployeeIdsAndWeekAsync(new List<long> { _employeeAProfileId }, _weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timesheetsA);
        _timesheetRepoMock.Setup(r => r.GetLineItemsByTimesheetIdAsync(500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lineItemsA);

        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = _projectId, ProjectName = _projectName });

        // Act
        var managerAResult = await _timesheetService.GetTeamTimesheetsAsync(_managerAUserId, _weekStart);

        // Assert
        Assert.Single(managerAResult.Rows);
        Assert.Equal(_employeeAFullName, managerAResult.Rows[0].EmployeeName);
    }

    [Fact]
    public async Task GetTeamTimesheetsAsync_ReturnsFrozenEmployees()
    {
        var team = new List<ResourceProfile>
        {
            new() { Id = _employeeAProfileId, UserId = 101, ManagerId = _managerAUserId, IsTimesheetFrozen = true },
            new() { Id = 11, UserId = 102, ManagerId = _managerAUserId, IsTimesheetFrozen = false }
        };
        var users = new Dictionary<long, User>
        {
            { 101, new User { Id = 101, FullName = _employeeAFullName } },
            { 102, new User { Id = 102, FullName = "Employee B" } }
        };

        _employeeRepoMock.Setup(r => r.GetByManagerIdAsync(_managerAUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);
        _userRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdsForWeekAsync(It.IsAny<IEnumerable<long>>(), _weekStart, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _timesheetRepoMock.Setup(r => r.GetByEmployeeIdsAndWeekAsync(It.IsAny<IEnumerable<long>>(), _weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _timesheetService.GetTeamTimesheetsAsync(_managerAUserId, _weekStart);

        Assert.Single(result.FrozenEmployees);
        Assert.Equal(_employeeAProfileId, result.FrozenEmployees[0].EmployeeId);
        Assert.Equal(_employeeAFullName, result.FrozenEmployees[0].EmployeeName);
    }

    [Fact]
    public async Task GetTimesheetForManagerAsync_OtherTeam_ThrowsNotFound()
    {
        // Arrange
        var timesheet = new Timesheet { Id = 500, ResourceProfileId = _employeeAProfileId };
        var profile = new ResourceProfile { Id = _employeeAProfileId, UserId = 101, ManagerId = _managerAUserId }; // Manager is A, but request will be from B

        _timesheetRepoMock.Setup(r => r.GetByIdForEmployeeCheckAsync(500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timesheet);
        _employeeRepoMock.Setup(r => r.GetByIdAsync(_employeeAProfileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundAppException>(
            () => _timesheetService.GetTimesheetForManagerAsync(_managerBUserId, 500));
    }
}
