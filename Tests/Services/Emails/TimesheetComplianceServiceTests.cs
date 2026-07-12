using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Common.Audit;
using Server.Common.Emails;
using Server.Models.DTOs.SystemConfig;
using Server.Models.Entities;
using Server.Repositories.Allocations;
using Server.Repositories.Emails;
using Server.Repositories.Employees;
using Server.Repositories.SystemConfig;
using Server.Repositories.Timesheets;
using Server.Repositories.Users;
using Server.Services.Compliance;
using Server.Models.Emails;
using Server.Services.Emails;
using Server.Services.Shared;
using Xunit;

namespace Tests.Services.Emails;

public class TimesheetComplianceServiceTests
{
    private readonly Mock<ISystemConfigRepository> _configRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IEmailLogRepository> _emailLogRepoMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly TimesheetComplianceService _service;

    public TimesheetComplianceServiceTests()
    {
        _configRepoMock = new Mock<ISystemConfigRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();
        _timesheetRepoMock = new Mock<ITimesheetRepository>();
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _emailLogRepoMock = new Mock<IEmailLogRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _auditServiceMock = new Mock<IAuditService>();

        _service = new TimesheetComplianceService(
            _configRepoMock.Object,
            _allocationRepoMock.Object,
            _timesheetRepoMock.Object,
            _employeeRepoMock.Object,
            _userRepoMock.Object,
            _emailLogRepoMock.Object,
            _emailServiceMock.Object,
            _auditServiceMock.Object,
            new Mock<ILogger<TimesheetComplianceService>>().Object);
    }

    [Fact]
    public async Task ProcessTimesheetComplianceAsync_SkipsDuplicateReminderEmails()
    {
        var lastWeek = WeekDateHelper.GetMostRecentCompletedWeekMonday();
        var schedule = TimesheetComplianceCalendar.Build(lastWeek, 1);
        if (DateOnly.FromDateTime(DateTime.Today) != schedule.Reminder1)
            return;

        SetupDeadlineConfig();
        var weekEnd = WeekDateHelper.GetWeekEnd(lastWeek).ToString("yyyy-MM-dd");

        _allocationRepoMock.Setup(r => r.GetAllActiveForWeekAsync(lastWeek, WeekDateHelper.GetWeekEnd(lastWeek), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectAllocation> { new() { ResourceProfileId = 1 } });

        _timesheetRepoMock.Setup(r => r.HasSubmittedForWeekAsync(1, lastWeek, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _employeeRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceProfile { Id = 1, UserId = 10 });

        _userRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 10, Email = "emp@example.com", FullName = "Employee" });

        _emailLogRepoMock.Setup(r => r.WasSentForReferenceAsync(
                "emp@example.com",
                EmailTypeConstants.TimesheetReminder1,
                $"WeekEnding:{weekEnd}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.ProcessTimesheetComplianceAsync();

        _emailServiceMock.Verify(
            e => e.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessTimesheetComplianceAsync_FreezesProfile_OnFreezeDay()
    {
        var lastWeek = WeekDateHelper.GetMostRecentCompletedWeekMonday();
        var schedule = TimesheetComplianceCalendar.Build(lastWeek, 1);
        if (DateOnly.FromDateTime(DateTime.Today) != schedule.Freeze)
            return;

        SetupDeadlineConfig();
        var weekEndDate = WeekDateHelper.GetWeekEnd(lastWeek);

        _allocationRepoMock.Setup(r => r.GetAllActiveForWeekAsync(lastWeek, weekEndDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectAllocation> { new() { ResourceProfileId = 1 } });

        _timesheetRepoMock.Setup(r => r.HasSubmittedForWeekAsync(1, lastWeek, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var profile = new ResourceProfile { Id = 1, UserId = 10, IsTimesheetFrozen = false, ManagerId = 20 };
        _employeeRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        _userRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 10, Email = "emp@example.com", FullName = "Employee" });
        _userRepoMock.Setup(r => r.GetByIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 20, Email = "mgr@example.com", FullName = "Manager" });

        _emailLogRepoMock.Setup(r => r.WasSentForReferenceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _emailServiceMock.Setup(e => e.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailSendResult { Success = true });

        await _service.ProcessTimesheetComplianceAsync();

        Assert.True(profile.IsTimesheetFrozen);
        _employeeRepoMock.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
        _employeeRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupDeadlineConfig()
    {
        _configRepoMock.Setup(r => r.GetByKeyAsync(ConfigKeys.TimesheetDeadlineDay, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemConfiguration
            {
                ConfigKey = ConfigKeys.TimesheetDeadlineDay,
                ConfigValue = EmailDefaults.TimesheetDeadlineWorkingDaysAfterWeekEnd.ToString()
            });
    }
}
