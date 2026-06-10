using Server.Models.DTOs.Allocations;

namespace Server.Services.Allocations;

public interface IAllocationService
{
    Task<AllocationListResponseDto> GetAllAllocationsAsync(long? employeeId, long? projectId, string? status, CancellationToken cancellationToken = default);
    Task<EmployeeAllocationListResponseDto> GetMyAllocationsAsync(long employeeId, CancellationToken cancellationToken = default);
    Task<CreateAllocationResponseDto> CreateAllocationAsync(long managerUserId, CreateAllocationRequestDto request, CancellationToken cancellationToken = default);
    Task<EndAllocationResponseDto> EndAllocationAsync(long managerUserId, long allocationId, CancellationToken cancellationToken = default);
}
