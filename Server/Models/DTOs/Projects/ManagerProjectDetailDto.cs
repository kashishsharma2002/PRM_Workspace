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

public class ManagerProjectMilestoneDto
{
    public string MilestoneTitle { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public string MilestoneStatus { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}

public class ManagerProjectResourceDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public DateOnly AllocationStartDate { get; set; }
    public DateOnly AllocationEndDate { get; set; }
}
