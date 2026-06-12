namespace Client.Models.Employees;

public class TeamBenchEmployee
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Department { get; set; }
    public List<string> Skills { get; set; } = [];
}
