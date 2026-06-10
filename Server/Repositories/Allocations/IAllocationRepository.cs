using Server.Models.Entities;

namespace Server.Repositories.Allocations;

public interface IAllocationRepository
{
    Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdsAsync(IEnumerable<long> employeeIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdForWeekAsync(
        long employeeId,
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdsForWeekAsync(
        IEnumerable<long> employeeIds,
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAllocation>> GetAllActiveForWeekAsync(
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAllocation>> GetActiveByProjectIdAsync(long projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAllocation>> GetByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default);
    Task<ProjectAllocation?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAllocation>> GetAllAsync(long? employeeId, long? projectId, string? status, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProjectAllocation allocation, CancellationToken cancellationToken = default);
    Task AddAsync(ProjectAllocation allocation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
