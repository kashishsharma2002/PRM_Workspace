namespace Server.Models.DTOs.Employees;

public class TeamBenchEmployeeDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public IReadOnlyList<string> Skills { get; set; } = [];
}
