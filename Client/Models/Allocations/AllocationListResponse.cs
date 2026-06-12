namespace Client.Models.Allocations;

public class AllocationListResponse
{
    public List<AllocationListItem> Allocations { get; set; } = [];
    public int TotalActiveCount { get; set; }
}
