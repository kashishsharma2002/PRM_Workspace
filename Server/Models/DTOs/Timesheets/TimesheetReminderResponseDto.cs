namespace Server.Models.DTOs.Timesheets;

public class TimesheetReminderResponseDto
{
    public bool ShowReminder { get; set; }
    public DateOnly WeekStartDate { get; set; }
}
