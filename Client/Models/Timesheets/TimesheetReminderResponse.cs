namespace Client.Models.Timesheets;

public class TimesheetReminderResponse
{
    public bool ShowReminder { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public bool IsTimesheetFrozen { get; set; }
}
