namespace Server.Models.DTOs.Timesheets;

public class TimesheetDetailLineItemDto
{
    public string ProjectName { get; set; } = string.Empty;
    public decimal HoursLogged { get; set; }
    public List<string> ActivityTags { get; set; } = [];
}
