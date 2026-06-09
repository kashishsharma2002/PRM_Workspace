using Server.Models.DTOs.Allocations;

namespace Server.Services.Interfaces;

public interface IAllocationService
{
    Task<AllocationListResponseDto> GetAllAllocationsAsync(long? employeeId, long? projectId, string? status, CancellationToken cancellationToken = default);
}
