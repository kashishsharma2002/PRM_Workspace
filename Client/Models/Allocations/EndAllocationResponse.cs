namespace Client.Models.Allocations;

public class EndAllocationResponse
{
    public long AllocationId { get; set; }
    public long EmployeeId { get; set; }
    public string EmploymentStatus { get; set; } = string.Empty;
    public DateOnly AllocationEndDate { get; set; }
}
