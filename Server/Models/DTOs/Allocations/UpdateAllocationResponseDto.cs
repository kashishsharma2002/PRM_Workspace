namespace Server.Models.DTOs.Allocations;

public class UpdateAllocationResponseDto
{
    public long AllocationId { get; set; }
    public long EmployeeId { get; set; }
    public decimal AllocationPercentage { get; set; }
    public DateOnly AllocationStartDate { get; set; }
    public DateOnly AllocationEndDate { get; set; }
    public string AllocationStatus { get; set; } = string.Empty;
    public string EmploymentStatus { get; set; } = string.Empty;
}
