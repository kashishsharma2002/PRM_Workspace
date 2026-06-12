namespace Server.Models.DTOs.Employees;

public class TeamActiveEmployeeDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public decimal AvailabilityPercentage { get; set; }
}
