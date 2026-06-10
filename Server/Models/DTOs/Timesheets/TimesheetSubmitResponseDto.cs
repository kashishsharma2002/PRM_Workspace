namespace Server.Models.DTOs.Timesheets;

public class TimesheetSubmitResponseDto
{
    public long TimesheetId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
}
