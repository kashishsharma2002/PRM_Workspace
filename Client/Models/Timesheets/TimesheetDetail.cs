namespace Client.Models.Timesheets;

public class TimesheetDetail
{
    public long Id { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public List<TimesheetDetailLineItem> LineItems { get; set; } = [];
}
