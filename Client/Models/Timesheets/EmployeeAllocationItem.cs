namespace Client.Models.Timesheets;

public class EmployeeAllocationItem
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public DateOnly AllocationStartDate { get; set; }
    public DateOnly AllocationEndDate { get; set; }
    public string AllocationStatus { get; set; } = string.Empty;
}
