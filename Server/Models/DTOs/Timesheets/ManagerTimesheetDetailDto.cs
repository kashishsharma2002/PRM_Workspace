namespace Server.Models.DTOs.Timesheets;

public class ManagerTimesheetDetailDto
{
    public long Id { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly WeekStartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public List<TimesheetDetailLineItemDto> LineItems { get; set; } = [];
}
