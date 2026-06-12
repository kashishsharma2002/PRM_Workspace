namespace Server.Models.DTOs.Ai;

public class TeamBuilderRoleResultDto
{
    public string RoleTitle { get; set; } = string.Empty;
    public List<TeamBuilderSkillRequirementDto> RequiredSkills { get; set; } = [];
    public string Status { get; set; } = string.Empty;
    public string? AssignedEmployeeName { get; set; }
    public int? MatchScore { get; set; }
    public string? Reason { get; set; }
    public TeamBuilderGapDto? Gap { get; set; }
}
