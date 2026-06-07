namespace Server.Models.Entities;

public class EmployeeSkill
{
    public long EmployeeId { get; set; }
    public long SkillId { get; set; }
    public string ProficiencyLevel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
