namespace Server.Models.DTOs.Timesheets;

public class TimesheetDetailDto
{
    public long Id { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalHours { get; set; }
    public List<TimesheetDetailLineItemDto> LineItems { get; set; } = [];
}

public class TimesheetDetailLineItemDto
{
    public string ProjectName { get; set; } = string.Empty;
    public decimal HoursLogged { get; set; }
    public List<string> ActivityTags { get; set; } = [];
}
