namespace Server.Models.DTOs.Employees;

public class EmployeeListItemDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
