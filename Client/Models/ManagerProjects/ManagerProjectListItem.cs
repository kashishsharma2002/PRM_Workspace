namespace Client.Models.ManagerProjects;

public class ManagerProjectListItem
{
    public long Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public string ProjectStatus { get; set; } = string.Empty;
}
