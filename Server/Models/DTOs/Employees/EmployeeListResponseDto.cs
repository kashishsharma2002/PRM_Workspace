namespace Server.Models.DTOs.Employees;

public class EmployeeListResponseDto
{
    public IReadOnlyList<EmployeeListItemDto> Employees { get; set; } = [];
    public int Total { get; set; }
    public int AllocatedCount { get; set; }
    public int BenchCount { get; set; }
}
