namespace Server.Models.DTOs.Projects;

public class ManagerProjectResourceDto
{
    public long AllocationId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public DateOnly AllocationStartDate { get; set; }
    public DateOnly AllocationEndDate { get; set; }
}
