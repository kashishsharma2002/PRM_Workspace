namespace Server.Models.DTOs.Employees;

public class ActiveAllocationDto
{
    public long AllocationId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public DateOnly AllocationStartDate { get; set; }
    public DateOnly AllocationEndDate { get; set; }
}
