namespace Server.Models.DTOs.Ai.Context;

using Server.Models.DTOs.SkillMatching.Context;

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
