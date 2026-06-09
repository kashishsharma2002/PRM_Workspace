namespace Server.Models.DTOs.Allocations;

public class EmployeeAllocationListResponseDto
{
    public List<EmployeeAllocationItemDto> Allocations { get; set; } = [];
    public decimal TotalUtilizationPercentage { get; set; }
}
