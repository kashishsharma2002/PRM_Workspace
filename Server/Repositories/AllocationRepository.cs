using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class AllocationRepository(PrmDbContext context) : IAllocationRepository
{
    public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default) =>
        await context.ProjectAllocations
            .Where(a => a.EmployeeId == employeeId && a.AllocationStatus == "ACTIVE")
            .ToListAsync(cancellationToken);

    public Task UpdateAsync(ProjectAllocation allocation, CancellationToken cancellationToken = default)
    {
        context.ProjectAllocations.Update(allocation);
        return Task.CompletedTask;
    }
}
