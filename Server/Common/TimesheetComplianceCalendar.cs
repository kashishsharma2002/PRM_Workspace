namespace Server.Common;

public sealed record TimesheetComplianceSchedule(
    DateOnly Deadline,
    DateOnly Reminder1,
    DateOnly Reminder2,
    DateOnly Freeze);

public static class TimesheetComplianceCalendar
{
    /// <summary>
    /// Builds the compliance schedule for a timesheet week.
    /// Deadline is N working days after the week ends (Sunday).
    /// Reminder 1 is the first working day after the deadline, then reminder 2, then freeze.
    /// </summary>
    public static TimesheetComplianceSchedule Build(DateOnly weekStartMonday, int deadlineWorkingDaysAfterWeekEnd)
    {
        var weekEnd = WeekDateHelper.GetWeekEnd(weekStartMonday);
        var deadline = WorkingDayHelper.AddWorkingDays(weekEnd, deadlineWorkingDaysAfterWeekEnd);
        var reminder1 = WorkingDayHelper.GetNextWorkingDay(deadline);
        var reminder2 = WorkingDayHelper.GetNextWorkingDay(reminder1);
        var freeze = WorkingDayHelper.GetNextWorkingDay(reminder2);

        return new TimesheetComplianceSchedule(deadline, reminder1, reminder2, freeze);
    }
}
