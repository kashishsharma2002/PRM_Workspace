using Server.Models.Entities;

namespace Server.Models.DTOs.SkillMatching.Context;

public class AiSkillMatchRawData
{
    public List<User> Employees { get; set; } = [];
    public List<AiUserSkillRaw> UserSkills { get; set; } = [];
    public List<ResourceProfile> ResourceProfiles { get; set; } = [];
    public List<AiActiveAllocationRaw> ActiveAllocations { get; set; } = [];
}

public class AiUserSkillRaw
{
    public long UserId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ProficiencyLevel { get; set; } = string.Empty;
}

public class AiActiveAllocationRaw
{
    public long ProjectId { get; set; }
    public long ResourceProfileId { get; set; }
    public decimal AllocationPercentage { get; set; }
    public DateOnly AllocationStartDate { get; set; }
    public DateOnly AllocationEndDate { get; set; }
    public string ProjectName { get; set; } = string.Empty;
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

public class AiProjectSummaryContext
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
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
