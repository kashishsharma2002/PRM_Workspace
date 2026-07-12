namespace Server.Repositories.Employees;

public record EmployeeSkillDetailProjection(
    long UserId,
    string SkillName,
    string Category,
    string ProficiencyLevel);
