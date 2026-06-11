namespace Server.Models.Entities;

public class UserSkill
{
    public long UserId { get; set; }
    public long SkillId { get; set; }
    public string ProficiencyLevel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
