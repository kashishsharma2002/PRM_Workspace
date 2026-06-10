namespace Client.HttpClients;

public class ManagerProjectListItem
{
    public long Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly EndDate { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
}

public class ManagerProjectListResponse
{
    public List<ManagerProjectListItem> Projects { get; set; } = [];
}

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

public class ManagerProjectMilestone
{
    public string MilestoneTitle { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public string MilestoneStatus { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}

public class ManagerProjectResource
{
    public string EmployeeName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public DateOnly AllocationStartDate { get; set; }
    public DateOnly AllocationEndDate { get; set; }
}
