namespace Client.Models.Timesheets;

public class TeamTimesheetRow
{
    public long? TimesheetId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public decimal HoursLogged { get; set; }
    public string Status { get; set; } = string.Empty;
}
