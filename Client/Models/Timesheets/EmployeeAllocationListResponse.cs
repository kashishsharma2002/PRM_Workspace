namespace Client.Models.Timesheets;

public class EmployeeAllocationListResponse
{
    public List<EmployeeAllocationItem> Allocations { get; set; } = [];
    public decimal TotalUtilizationPercentage { get; set; }
}
