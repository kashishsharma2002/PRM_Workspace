namespace Server.Models.DTOs.Employees;

public class TeamMemberDetailDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public decimal TotalUtilizationPercentage { get; set; }
    public IReadOnlyList<EmployeeSkillDto> Skills { get; set; } = [];
    public IReadOnlyList<ActiveAllocationDto> ActiveAllocations { get; set; } = [];
    public IReadOnlyList<string> RecentActivityTags { get; set; } = [];
}
