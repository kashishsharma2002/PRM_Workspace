namespace Server.Models.DTOs.Employees;

public class TeamDashboardDto
{
    public IReadOnlyList<TeamBenchEmployeeDto> BenchEmployees { get; set; } = [];
    public IReadOnlyList<TeamActiveEmployeeDto> ActiveEmployees { get; set; } = [];
    public int BenchCount { get; set; }
    public int PartialCount { get; set; }
}
