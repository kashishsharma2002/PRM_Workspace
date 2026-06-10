namespace Client.Models.Timesheets;

public class EmployeeWeekAllocation
{
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public decimal MaxHours { get; set; }
}
