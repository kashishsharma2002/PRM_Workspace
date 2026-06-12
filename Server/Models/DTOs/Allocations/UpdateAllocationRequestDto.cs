namespace Server.Models.DTOs.Allocations;

public class UpdateAllocationRequestDto
{
    public decimal? AllocationPercentage { get; set; }
    public DateOnly? AllocationStartDate { get; set; }
    public DateOnly? AllocationEndDate { get; set; }
}
