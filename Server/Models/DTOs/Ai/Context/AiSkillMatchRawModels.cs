using Server.Models.Entities;

namespace Server.Models.DTOs.Ai.Context;

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
