namespace Server.Models.Entities;

public class ProjectAllocation
{
    public long Id { get; set; }
    public long ResourceProfileId { get; set; }
    public long ProjectId { get; set; }
    public decimal AllocationPercentage { get; set; }
    public DateOnly AllocationStartDate { get; set; }
    public DateOnly AllocationEndDate { get; set; }
    public string AllocationStatus { get; set; } = "ACTIVE";
    public long AllocatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
