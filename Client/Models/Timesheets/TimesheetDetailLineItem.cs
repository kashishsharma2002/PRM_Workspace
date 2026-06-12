namespace Client.Models.Timesheets;

public class TimesheetDetailLineItem
{
    public string ProjectName { get; set; } = string.Empty;
    public decimal HoursLogged { get; set; }
    public List<string> ActivityTags { get; set; } = [];
}
