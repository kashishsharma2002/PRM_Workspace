using Server.Models.Entities;

namespace Server.Repositories.Interfaces;

public interface IAllocationRepository
{
    Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectAllocation>> GetAllAsync(long? employeeId, long? projectId, string? status, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProjectAllocation allocation, CancellationToken cancellationToken = default);
    Task AddAsync(ProjectAllocation allocation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
