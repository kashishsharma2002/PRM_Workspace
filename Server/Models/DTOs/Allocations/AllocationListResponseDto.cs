namespace Server.Models.DTOs.Allocations;

public class AllocationListResponseDto
{
    public IReadOnlyList<AllocationListItemDto> Allocations { get; set; } = [];
    public int TotalActiveCount { get; set; }
}
