namespace Client.Models.Allocations;

public class CreateAllocationRequest
{
    public long EmployeeId { get; set; }
    public long ProjectId { get; set; }
    public decimal AllocationPercentage { get; set; }
    public DateOnly AllocationStartDate { get; set; }
    public DateOnly AllocationEndDate { get; set; }
}
