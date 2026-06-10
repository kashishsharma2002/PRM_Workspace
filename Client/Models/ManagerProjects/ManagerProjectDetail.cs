namespace Client.Models.ManagerProjects;

public class ManagerProjectDetail
{
    public long Id { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly EndDate { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public List<ManagerProjectMilestone> Milestones { get; set; } = [];
    public List<ManagerProjectResource> AllocatedResources { get; set; } = [];
    public List<string> RiskFlags { get; set; } = [];
}
