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

public class TimesheetReminderTests
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

    private readonly DateOnly _lastWeek;
    private readonly long _employeeWithAllocationId = 10;
    private readonly long _employeeWithSubmissionId = 20;
    private readonly long _employeeWithMissedId = 30;
    private readonly long _employeeNoAllocationId = 40;

    public TimesheetReminderTests()
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
    public async Task HasMissedTimesheetReminderAsync_AllocationNoRow_ReturnsTrue()
    {
        // Arrange
        var allocations = new List<ProjectAllocation> { new() { Id = 1, ResourceProfileId = _employeeWithAllocationId } };
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdForWeekAsync(_employeeWithAllocationId, _lastWeek, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);
        _timesheetRepoMock.Setup(r => r.HasSubmittedForWeekAsync(_employeeWithAllocationId, _lastWeek, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var showReminder = await _timesheetService.HasMissedTimesheetReminderAsync(_employeeWithAllocationId);

        // Assert
        Assert.True(showReminder);
    }

    [Fact]
    public async Task HasMissedTimesheetReminderAsync_AllocationSubmitted_ReturnsFalse()
    {
        // Arrange
        var allocations = new List<ProjectAllocation> { new() { Id = 2, ResourceProfileId = _employeeWithSubmissionId } };
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdForWeekAsync(_employeeWithSubmissionId, _lastWeek, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);
        _timesheetRepoMock.Setup(r => r.HasSubmittedForWeekAsync(_employeeWithSubmissionId, _lastWeek, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var showReminder = await _timesheetService.HasMissedTimesheetReminderAsync(_employeeWithSubmissionId);

        // Assert
        Assert.False(showReminder);
    }

    [Fact]
    public async Task HasMissedTimesheetReminderAsync_AllocationMissedOnly_ReturnsTrue()
    {
        // Arrange
        var allocations = new List<ProjectAllocation> { new() { Id = 3, ResourceProfileId = _employeeWithMissedId } };
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdForWeekAsync(_employeeWithMissedId, _lastWeek, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);
        _timesheetRepoMock.Setup(r => r.HasSubmittedForWeekAsync(_employeeWithMissedId, _lastWeek, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var showReminder = await _timesheetService.HasMissedTimesheetReminderAsync(_employeeWithMissedId);

        // Assert
        Assert.True(showReminder);
    }

    [Fact]
    public async Task HasMissedTimesheetReminderAsync_NoAllocation_ReturnsFalse()
    {
        // Arrange
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdForWeekAsync(_employeeNoAllocationId, _lastWeek, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectAllocation>());

        // Act
        var showReminder = await _timesheetService.HasMissedTimesheetReminderAsync(_employeeNoAllocationId);

        // Assert
        Assert.False(showReminder);
    }
}
