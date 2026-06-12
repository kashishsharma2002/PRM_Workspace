namespace Client.Models.Timesheets;

public class TimesheetSubmitRequest
{
    public DateOnly WeekStartDate { get; set; }
    public List<TimesheetLineItemRequest> LineItems { get; set; } = [];
    public string? Remarks { get; set; }
}
