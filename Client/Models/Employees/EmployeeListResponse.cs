namespace Client.Models.Employees;

public class EmployeeListResponse
{
    public List<EmployeeListItem> Employees { get; set; } = [];
    public int Total { get; set; }
    public int AllocatedCount { get; set; }
    public int BenchCount { get; set; }
}
