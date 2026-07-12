namespace Server.Models.DTOs.ProjectRisk.Context;

public class ProjectRiskContextModel
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ProjectStatus { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public List<ProjectRiskMilestoneContext> Milestones { get; set; } = [];
    public List<ProjectRiskAllocationContext> Allocations { get; set; } = [];
    public List<ProjectRiskTimesheetHoursContext> RecentLoggedHours { get; set; } = [];
}

public class ProjectRiskMilestoneContext
{
    public string Title { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? StoryPoints { get; set; }
    public bool IsOverdue { get; set; }
}

public class ProjectRiskAllocationContext
{
    public string EmployeeName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
}

public class ProjectRiskTimesheetHoursContext
{
    public string EmployeeName { get; set; } = string.Empty;
    public decimal HoursLogged { get; set; }
    public string? WorkDate { get; set; }
}
