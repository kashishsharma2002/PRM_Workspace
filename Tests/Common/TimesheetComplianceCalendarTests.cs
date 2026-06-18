using Server.Common;
using Xunit;

namespace Tests.Common;

public class TimesheetComplianceCalendarTests
{
    [Fact]
    public void Build_SchedulesRemindersOnWorkingDaysAfterDeadline()
    {
        var weekStart = new DateOnly(2026, 6, 8);
        var schedule = TimesheetComplianceCalendar.Build(weekStart, 1);

        Assert.Equal(new DateOnly(2026, 6, 15), schedule.Deadline);
        Assert.Equal(new DateOnly(2026, 6, 16), schedule.Reminder1);
        Assert.Equal(new DateOnly(2026, 6, 17), schedule.Reminder2);
        Assert.Equal(new DateOnly(2026, 6, 18), schedule.Freeze);
    }
}
