using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Common.Audit;
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

public class TimesheetServiceTests
{
    private readonly Mock<IDbTransactionManager> _transactionManagerMock;
    private readonly Mock<IDbTransaction> _transactionMock;
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IActivityTagRepository> _activityTagRepoMock;
    private readonly Mock<ISystemConfigRepository> _systemConfigRepoMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<TimesheetService>> _loggerMock;
    private readonly TimesheetService _timesheetService;

    private readonly long _employeeId = 10;
    private readonly long _userId = 101;
    private readonly long _projectId = 200;
    private readonly DateOnly _weekStart = new(2026, 5, 12);

    public TimesheetServiceTests()
    {
        _transactionMock = new Mock<IDbTransaction>();
        _transactionManagerMock = new Mock<IDbTransactionManager>();

        _transactionManagerMock.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _timesheetRepoMock = new Mock<ITimesheetRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _activityTagRepoMock = new Mock<IActivityTagRepository>();
        _systemConfigRepoMock = new Mock<ISystemConfigRepository>();
        _auditServiceMock = new Mock<IAuditService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<TimesheetService>>();

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
            _memoryCache,
            _loggerMock.Object);
    }

    private void SetupDefaultConfigAndAllocation(decimal allocationPercentage = 50, string maxWeeklyHours = "40")
    {
        _employeeRepoMock.Setup(r => r.GetByIdAsync(_employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceProfile { Id = _employeeId, UserId = _userId, IsTimesheetFrozen = false });

        _systemConfigRepoMock.Setup(r => r.GetByKeyAsync("MaxWeeklyHours", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemConfiguration { ConfigKey = "MaxWeeklyHours", ConfigValue = maxWeeklyHours });

        var allocations = new List<ProjectAllocation>
        {
            new() { Id = 1, ResourceProfileId = _employeeId, ProjectId = _projectId, AllocationPercentage = allocationPercentage, AllocationStatus = "ACTIVE" }
        };
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdForWeekAsync(_employeeId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);
    }

    [Fact]
    public async Task SubmitTimesheetAsync_ValidRequest_CreatesTimesheet()
    {
        // Arrange
        SetupDefaultConfigAndAllocation(50, "40");
        var lineItems = new List<TimesheetLineItemRequestDto>
        {
            new() { ProjectId = _projectId, HoursLogged = 18, ActivityTagIds = [1], CustomTagText = "" }
        };
        var request = new TimesheetSubmitRequestDto { WeekStartDate = _weekStart, LineItems = lineItems, Remarks = "Test" };

        _timesheetRepoMock.Setup(r => r.HasSubmittedForWeekAsync(_employeeId, _weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _activityTagRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ActivityTag> { new() { Id = 1, TagCode = "DEV", TagName = "Dev" } });
        _timesheetRepoMock.Setup(r => r.AddAsync(It.IsAny<Timesheet>(), It.IsAny<CancellationToken>()))
            .Callback<Timesheet, CancellationToken>((t, c) => t.Id = 55);

        // Act
        var result = await _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request);

        // Assert
        Assert.Equal(55, result.TimesheetId);
        Assert.Equal("SUBMITTED", result.Status);
        Assert.Equal(18, result.TotalHours);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitTimesheetAsync_DuplicateWeek_ThrowsConflict()
    {
        // Arrange
        SetupDefaultConfigAndAllocation();
        var lineItems = new List<TimesheetLineItemRequestDto> { new() { ProjectId = _projectId, HoursLogged = 10 } };
        var request = new TimesheetSubmitRequestDto { WeekStartDate = _weekStart, LineItems = lineItems };

        _timesheetRepoMock.Setup(r => r.HasSubmittedForWeekAsync(_employeeId, _weekStart, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictAppException>(
            () => _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request));
    }

    [Fact]
    public async Task SubmitTimesheetAsync_FrozenAccount_ThrowsForbidden()
    {
        _employeeRepoMock.Setup(r => r.GetByIdAsync(_employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceProfile { Id = _employeeId, UserId = _userId, IsTimesheetFrozen = true });

        var request = new TimesheetSubmitRequestDto
        {
            WeekStartDate = _weekStart,
            LineItems = [new() { ProjectId = _projectId, HoursLogged = 10 }]
        };

        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request));
    }

    [Fact]
    public async Task SubmitTimesheetAsync_FutureWeek_ThrowsValidation()
    {
        // Arrange
        var futureMonday = WeekDateHelper.GetCurrentWeekMonday().AddDays(7);
        var lineItems = new List<TimesheetLineItemRequestDto> { new() { ProjectId = _projectId, HoursLogged = 10 } };
        var request = new TimesheetSubmitRequestDto { WeekStartDate = futureMonday, LineItems = lineItems };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationAppException>(
            () => _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request));
    }

    [Fact]
    public async Task SubmitTimesheetAsync_HoursExceedAllocation_ThrowsValidation()
    {
        // Arrange
        SetupDefaultConfigAndAllocation(50, "40"); // Max hours per project allocation (50% of 40) is 20
        var lineItems = new List<TimesheetLineItemRequestDto>
        {
            new() { ProjectId = _projectId, HoursLogged = 25 } // 25 exceeds 20
        };
        var request = new TimesheetSubmitRequestDto { WeekStartDate = _weekStart, LineItems = lineItems };

        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = _projectId, ProjectName = "Project A" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationAppException>(
            () => _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request));

        Assert.Contains("exceeds allocation cap", ex.Message);
    }

    [Fact]
    public async Task SubmitTimesheetAsync_TotalHoursExceedMaxWeekly_ThrowsValidation()
    {
        // Arrange
        SetupDefaultConfigAndAllocation(100, "40"); // 100% allocation, but request is 41 hours
        var lineItems = new List<TimesheetLineItemRequestDto>
        {
            new() { ProjectId = _projectId, HoursLogged = 41 }
        };
        var request = new TimesheetSubmitRequestDto { WeekStartDate = _weekStart, LineItems = lineItems };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationAppException>(
            () => _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request));

        Assert.Contains("maximum weekly limit", ex.Message);
    }

    [Fact]
    public async Task SubmitTimesheetAsync_UnallocatedProject_ThrowsValidation()
    {
        // Arrange
        SetupDefaultConfigAndAllocation(50, "40");
        var lineItems = new List<TimesheetLineItemRequestDto>
        {
            new() { ProjectId = 9999, HoursLogged = 10 } // Unallocated project ID
        };
        var request = new TimesheetSubmitRequestDto { WeekStartDate = _weekStart, LineItems = lineItems };

        _projectRepoMock.Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Project { Id = 9999, ProjectName = "Project X" });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationAppException>(
            () => _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request));

        Assert.Contains("not allocated", ex.Message);
    }

    [Fact]
    public async Task GetMyTimesheetsAsync_ReturnsSubmittedHistory()
    {
        // Arrange
        var timesheets = new List<Timesheet>
        {
            new() { Id = 1, ResourceProfileId = _employeeId, WeekStartDate = _weekStart, TotalHours = 15, Status = "SUBMITTED" }
        };
        _timesheetRepoMock.Setup(r => r.GetByEmployeeAsync(_employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timesheets);

        // Act
        var history = await _timesheetService.GetMyTimesheetsAsync(_employeeId);

        // Assert
        Assert.Single(history);
        Assert.Equal(_weekStart, history[0].WeekStartDate);
        Assert.Equal("SUBMITTED", history[0].Status);
    }
}
