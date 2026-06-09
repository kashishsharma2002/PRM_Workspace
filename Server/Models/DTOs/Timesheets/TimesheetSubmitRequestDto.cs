namespace Server.Models.DTOs.Timesheets;

public class TimesheetSubmitRequestDto
{
    public DateOnly WeekStartDate { get; set; }
    public List<TimesheetLineItemRequestDto> LineItems { get; set; } = [];
    public string? Remarks { get; set; }
}
