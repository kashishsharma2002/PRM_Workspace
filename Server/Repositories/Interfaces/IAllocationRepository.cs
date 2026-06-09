using Server.Models.Entities;

namespace Server.Repositories.Interfaces;

public interface IAllocationRepository
{
    Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProjectAllocation allocation, CancellationToken cancellationToken = default);
}
