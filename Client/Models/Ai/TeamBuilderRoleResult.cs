namespace Client.Models.Ai;

public class TeamBuilderRoleResult
{
    public string RoleTitle { get; set; } = string.Empty;
    public List<TeamBuilderSkillRequirement> RequiredSkills { get; set; } = [];
    public string Status { get; set; } = string.Empty;
    public string? AssignedEmployeeName { get; set; }
    public int? MatchScore { get; set; }
    public string? Reason { get; set; }
    public TeamBuilderGap? Gap { get; set; }
}
