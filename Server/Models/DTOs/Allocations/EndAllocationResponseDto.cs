namespace Server.Models.DTOs.Allocations;

public class EndAllocationResponseDto
{
    public long AllocationId { get; set; }
    public long EmployeeId { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public DateOnly AllocationEndDate { get; set; }
}
