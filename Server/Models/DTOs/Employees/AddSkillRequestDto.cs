namespace Server.Models.DTOs.Employees;

public class AddSkillRequestDto
{
    public string SkillName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ProficiencyLevel { get; set; } = string.Empty;
}
