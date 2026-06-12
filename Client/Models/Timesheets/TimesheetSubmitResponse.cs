namespace Client.Models.Timesheets;

public class TimesheetSubmitResponse
{
    public long TimesheetId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
}
