namespace Client.Models.Timesheets;

public class ManagerTimesheetDetail
{
    public long Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly WeekStartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public List<TimesheetDetailLineItem> LineItems { get; set; } = [];
}
