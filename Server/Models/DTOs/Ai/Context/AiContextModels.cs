namespace Server.Models.DTOs.Ai.Context;

public class AiRiskContextModel
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ProjectStatus { get; set; } = string.Empty;
    public string HealthStatus { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public List<AiMilestoneContext> Milestones { get; set; } = [];
    public List<AiAllocationContext> Allocations { get; set; } = [];
    public List<AiTimesheetHoursContext> RecentLoggedHours { get; set; } = [];
}

public class AiMilestoneContext
{
    public string Title { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? StoryPoints { get; set; }
    public bool IsOverdue { get; set; }
}

public class AiAllocationContext
{
    public long ProjectId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
}

public class AiTimesheetHoursContext
{
    public string EmployeeName { get; set; } = string.Empty;
    public decimal HoursLogged { get; set; }
    public string? WorkDate { get; set; }
}

public class AiSkillMatchContextModel
{
    public AiProjectSummaryContext Project { get; set; } = new();
    public List<AiSkillMatchCandidateContext> Candidates { get; set; } = [];
}

public class AiOrganizationalSkillMatchContextModel
{
    public List<AiSkillMatchCandidateContext> Candidates { get; set; } = [];
}

public class AiTeamBuilderContextModel
{
    public string ManagerRequirement { get; set; } = string.Empty;
    public List<AiSkillMatchCandidateContext> AssignableCandidates { get; set; } = [];
    public List<AiSkillMatchCandidateContext> AllCandidates { get; set; } = [];
}

public class AiTeamBuilderPromptContextModel
{
    public string ManagerRequirement { get; set; } = string.Empty;
    public List<AiTeamBuilderCandidatePromptContext> AssignableCandidates { get; set; } = [];
    public List<AiTeamBuilderCandidatePromptContext> AllCandidates { get; set; } = [];
}

public class AiTeamBuilderCandidatePromptContext
{
    public string FullName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public List<AiSkillContext> Skills { get; set; } = [];
    public decimal RemainingCapacityPercentage { get; set; }
    public List<AiAllocationContext> ActiveAllocations { get; set; } = [];
}

public class AiProjectSummaryContext
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AiSkillMatchCandidateContext
{
    public long EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public List<AiSkillContext> Skills { get; set; } = [];
    public List<AiAllocationContext> ActiveAllocations { get; set; } = [];
    public decimal TotalAllocatedPercentage { get; set; }
    public decimal RemainingCapacityPercentage { get; set; }
}

public class AiSkillContext
{
    public string SkillName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ProficiencyLevel { get; set; } = string.Empty;
}
