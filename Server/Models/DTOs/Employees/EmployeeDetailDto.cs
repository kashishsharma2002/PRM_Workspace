namespace Server.Models.DTOs.Employees;

public class EmployeeDetailDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public long? ManagerUserId { get; set; }
    public IReadOnlyList<EmployeeSkillDto> Skills { get; set; } = [];
    public IReadOnlyList<ActiveAllocationDto> ActiveAllocations { get; set; } = [];
}

public class EmployeeSkillDto
{
    public long SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ProficiencyLevel { get; set; } = string.Empty;
}

public class ActiveAllocationDto
{
    public long AllocationId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public DateOnly AllocationEndDate { get; set; }
}
