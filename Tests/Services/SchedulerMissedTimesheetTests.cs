using Moq;
using Server.Common;
using Server.Models.Entities;
using Server.Repositories.Allocations;
using Server.Repositories.Timesheets;
using Server.Services.Timesheets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Services;

public class SchedulerMissedTimesheetTests
{
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly SchedulerTimesheetService _schedulerTimesheetService;

    private readonly long _employeeWithAllocationId = 10;
    private readonly long _employeeWithSubmissionId = 20;
    private readonly DateOnly _lastWeek;

    public SchedulerMissedTimesheetTests()
    {
        _lastWeek = WeekDateHelper.GetMostRecentCompletedWeekMonday();

        _timesheetRepoMock = new Mock<ITimesheetRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();

        _schedulerTimesheetService = new SchedulerTimesheetService(
            _allocationRepoMock.Object,
            _timesheetRepoMock.Object);
    }

    [Fact]
    public async Task MarkMissedTimesheetsAsync_AllocationNoSubmission_InsertsMissed()
    {
        var allocations = new List<ProjectAllocation>
        {
            new() { Id = 1, ResourceProfileId = _employeeWithAllocationId, ProjectId = 100 },
            new() { Id = 2, ResourceProfileId = _employeeWithSubmissionId, ProjectId = 100 }
        };

        _allocationRepoMock.Setup(r => r.GetAllActiveForWeekAsync(_lastWeek, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        _timesheetRepoMock.Setup(r => r.GetEmployeeIdsWithTimesheetForWeekAsync(It.IsAny<IEnumerable<long>>(), _lastWeek, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { _employeeWithSubmissionId });

        var created = await _schedulerTimesheetService.MarkMissedTimesheetsAsync();

        Assert.Equal(1, created);
        _timesheetRepoMock.Verify(r => r.AddAsync(It.Is<Timesheet>(t => t.ResourceProfileId == _employeeWithAllocationId && t.Status == "MISSED"), It.IsAny<CancellationToken>()), Times.Once);
        _timesheetRepoMock.Verify(r => r.AddAsync(It.Is<Timesheet>(t => t.ResourceProfileId == _employeeWithSubmissionId), It.IsAny<CancellationToken>()), Times.Never);
        _timesheetRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkMissedTimesheetsAsync_ExistingMissed_Skips()
    {
        var allocations = new List<ProjectAllocation>
        {
            new() { Id = 1, ResourceProfileId = _employeeWithAllocationId, ProjectId = 100 }
        };

        _allocationRepoMock.Setup(r => r.GetAllActiveForWeekAsync(_lastWeek, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        _timesheetRepoMock.Setup(r => r.GetEmployeeIdsWithTimesheetForWeekAsync(It.IsAny<IEnumerable<long>>(), _lastWeek, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { _employeeWithAllocationId });

        var created = await _schedulerTimesheetService.MarkMissedTimesheetsAsync();

        Assert.Equal(0, created);
        _timesheetRepoMock.Verify(r => r.AddAsync(It.IsAny<Timesheet>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
