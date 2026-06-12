namespace Client.Models.Employees;

public class EmployeeSkillItem
{
    public long SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ProficiencyLevel { get; set; } = string.Empty;
}
