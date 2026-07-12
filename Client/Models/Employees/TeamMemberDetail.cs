namespace Client.Models.Employees;

public class TeamMemberDetail
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public bool IsTimesheetFrozen { get; set; }
    public decimal TotalUtilizationPercentage { get; set; }
    public List<EmployeeSkillItem> Skills { get; set; } = [];
    public List<ActiveAllocationItem> ActiveAllocations { get; set; } = [];
    public List<string> RecentActivityTags { get; set; } = [];
}
