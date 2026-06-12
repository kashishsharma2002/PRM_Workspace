namespace Server.Models.DTOs.Projects;

public class ManagerProjectDetailDto
{
    public long Id { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly EndDate { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public List<ManagerProjectMilestoneDto> Milestones { get; set; } = [];
    public List<ManagerProjectResourceDto> AllocatedResources { get; set; } = [];
    public List<string> RiskFlags { get; set; } = [];
}
