namespace Client.Models.Employees;

public class EmployeeListItem
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
