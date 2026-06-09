namespace Client.HttpClients;

public class EmployeeListItem
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class EmployeeListResponse
{
    public List<EmployeeListItem> Employees { get; set; } = [];
    public int Total { get; set; }
    public int AllocatedCount { get; set; }
    public int BenchCount { get; set; }
}

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

public class EmployeeSkillItem
{
    public long SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ProficiencyLevel { get; set; } = string.Empty;
}

public class ActiveAllocationItem
{
    public long AllocationId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public DateOnly AllocationEndDate { get; set; }
}

public class UpdateEmployeeRequest
{
    public string? Department { get; set; }
    public string? Designation { get; set; }
}

public class AssignManagerRequest
{
    public long ManagerUserId { get; set; }
}

public class AddSkillRequest
{
    public string SkillName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ProficiencyLevel { get; set; } = string.Empty;
}

public class UpdateSkillProficiencyRequest
{
    public string ProficiencyLevel { get; set; } = string.Empty;
}
