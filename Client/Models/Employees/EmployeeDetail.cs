namespace Client.Models.Employees;

public class EmployeeDetail
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
    public List<EmployeeSkillItem> Skills { get; set; } = [];
    public List<ActiveAllocationItem> ActiveAllocations { get; set; } = [];
}
