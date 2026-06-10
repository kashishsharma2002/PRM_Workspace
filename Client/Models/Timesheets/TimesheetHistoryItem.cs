namespace Client.Models.Timesheets;

public class TimesheetHistoryItem
{
    public long Id { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public decimal TotalHours { get; set; }
    public string Status { get; set; } = string.Empty;
}
