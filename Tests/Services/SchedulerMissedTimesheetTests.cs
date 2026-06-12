using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.Roles;
using Server.Repositories.Users;
using Server.Repositories.Employees;
using Server.Repositories.Allocations;
using Server.Repositories.Projects;
using Server.Repositories.Timesheets;
using Server.Repositories.SystemConfig;
using Server.Services.Shared;
using Server.Services.Timesheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class SchedulerMissedTimesheetTests
{
    private readonly Mock<IDbTransactionManager> _transactionManagerMock;
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IActivityTagRepository> _activityTagRepoMock;
    private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly TimesheetService _timesheetService;

    private readonly long _employeeWithAllocationId = 10;
    private readonly long _employeeWithSubmissionId = 20;
    private readonly DateOnly _lastWeek;

    public SchedulerMissedTimesheetTests()
    {
        _lastWeek = WeekDateHelper.GetMostRecentCompletedWeekMonday();
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
        _systemConfigRepoMock = new Mock<ISystemConfigRepository>();
        _auditServiceMock = new Mock<IAuditService>();

        _timesheetService = new TimesheetService(
            _transactionManagerMock.Object,
            _timesheetRepoMock.Object,
            _allocationRepoMock.Object,
            _projectRepoMock.Object,
            _employeeRepoMock.Object,
            _userRepoMock.Object,
            _activityTagRepoMock.Object,
            _systemConfigRepoMock.Object,
            _auditServiceMock.Object,
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<ILogger<TimesheetService>>().Object);
    }

    [Fact]
    public async Task MarkMissedTimesheetsAsync_AllocationNoSubmission_InsertsMissed()
    {
        // Arrange
        var allocations = new List<ProjectAllocation>
        {
            new() { Id = 1, ResourceProfileId = _employeeWithAllocationId, ProjectId = 100 },
            new() { Id = 2, ResourceProfileId = _employeeWithSubmissionId, ProjectId = 100 }
        };

        _allocationRepoMock.Setup(r => r.GetAllActiveForWeekAsync(_lastWeek, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        // employeeWithSubmissionId has existing timesheet
        _timesheetRepoMock.Setup(r => r.GetEmployeeIdsWithTimesheetForWeekAsync(It.IsAny<IEnumerable<long>>(), _lastWeek, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { _employeeWithSubmissionId });

        // Act
        var created = await _timesheetService.MarkMissedTimesheetsAsync();

        // Assert
        Assert.Equal(1, created);
        _timesheetRepoMock.Verify(r => r.AddAsync(It.Is<Timesheet>(t => t.ResourceProfileId == _employeeWithAllocationId && t.Status == "MISSED"), It.IsAny<CancellationToken>()), Times.Once);
        _timesheetRepoMock.Verify(r => r.AddAsync(It.Is<Timesheet>(t => t.ResourceProfileId == _employeeWithSubmissionId), It.IsAny<CancellationToken>()), Times.Never);
        _timesheetRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkMissedTimesheetsAsync_ExistingMissed_Skips()
    {
        // Arrange
        var allocations = new List<ProjectAllocation>
        {
            new() { Id = 1, ResourceProfileId = _employeeWithAllocationId, ProjectId = 100 }
        };

        _allocationRepoMock.Setup(r => r.GetAllActiveForWeekAsync(_lastWeek, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        // Already has timesheet
        _timesheetRepoMock.Setup(r => r.GetEmployeeIdsWithTimesheetForWeekAsync(It.IsAny<IEnumerable<long>>(), _lastWeek, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { _employeeWithAllocationId });

        // Act
        var created = await _timesheetService.MarkMissedTimesheetsAsync();

        // Assert
        Assert.Equal(0, created);
        _timesheetRepoMock.Verify(r => r.AddAsync(It.IsAny<Timesheet>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
