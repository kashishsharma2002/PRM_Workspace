namespace Client.Models.Employees;

public class TeamDashboard
{
    public List<TeamBenchEmployee> BenchEmployees { get; set; } = [];
    public List<TeamActiveEmployee> ActiveEmployees { get; set; } = [];
    public int BenchCount { get; set; }
    public int PartialCount { get; set; }
}
