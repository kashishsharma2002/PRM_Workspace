namespace Server.Models.DTOs.Allocations;

public class CreateAllocationResponseDto
{
    public long AllocationId { get; set; }
    public long EmployeeId { get; set; }
    public long ProjectId { get; set; }
    public decimal AllocationPercentage { get; set; }
    public string AllocationStatus { get; set; } = string.Empty;
    public string EmploymentStatus { get; set; } = string.Empty;
}
