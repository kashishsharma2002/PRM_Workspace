using Microsoft.EntityFrameworkCore;
using Server.Common.Allocations;
using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Allocations;

public class AllocationRepository(PrmDbContext context) : IAllocationRepository
{
    public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdAsync(long resourceProfileId, CancellationToken cancellationToken = default) =>
        await context.ProjectAllocations
            .Where(a => a.ResourceProfileId == resourceProfileId && a.AllocationStatus == AllocationStatusConstants.Active)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdsAsync(
        IEnumerable<long> resourceProfileIds,
        CancellationToken cancellationToken = default)
    {
        var ids = resourceProfileIds.ToList();
        if (ids.Count == 0)
            return [];

        return await context.ProjectAllocations
            .Where(a => ids.Contains(a.ResourceProfileId) && a.AllocationStatus == AllocationStatusConstants.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdForWeekAsync(
        long resourceProfileId,
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken cancellationToken = default) =>
        await context.ProjectAllocations
            .Where(a => a.ResourceProfileId == resourceProfileId
                && a.AllocationStatus == AllocationStatusConstants.Active
                && a.AllocationStartDate <= weekEnd
                && a.AllocationEndDate >= weekStart)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectAllocation>> GetByEmployeeIdAsync(long resourceProfileId, CancellationToken cancellationToken = default) =>
        await context.ProjectAllocations
            .Where(a => a.ResourceProfileId == resourceProfileId)
            .OrderByDescending(a => a.AllocationStartDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdsForWeekAsync(
        IEnumerable<long> resourceProfileIds,
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken cancellationToken = default)
    {
        var ids = resourceProfileIds.ToList();
        if (ids.Count == 0)
            return [];

        return await context.ProjectAllocations
            .Where(a => ids.Contains(a.ResourceProfileId)
                && a.AllocationStatus == AllocationStatusConstants.Active
                && a.AllocationStartDate <= weekEnd
                && a.AllocationEndDate >= weekStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectAllocation>> GetAllActiveForWeekAsync(
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken cancellationToken = default) =>
        await context.ProjectAllocations
            .Where(a => a.AllocationStatus == AllocationStatusConstants.Active
                && a.AllocationStartDate <= weekEnd
                && a.AllocationEndDate >= weekStart)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByProjectIdAsync(
        long projectId,
        CancellationToken cancellationToken = default) =>
        await context.ProjectAllocations
            .Where(a => a.ProjectId == projectId && a.AllocationStatus == AllocationStatusConstants.Active)
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

    public Task<ProjectAllocation?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.ProjectAllocations
            .AsTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProjectAllocation>> GetAllAsync(
        long? resourceProfileId,
        long? projectId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = context.ProjectAllocations.AsQueryable();

        if (resourceProfileId.HasValue)
            query = query.Where(a => a.ResourceProfileId == resourceProfileId.Value);

        if (projectId.HasValue)
            query = query.Where(a => a.ProjectId == projectId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.AllocationStatus == status.Trim().ToUpperInvariant());
        else
            query = query.Where(a => a.AllocationStatus == AllocationStatusConstants.Active);

        return await query.OrderBy(a => a.Id).ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(ProjectAllocation allocation, CancellationToken cancellationToken = default)
    {
        context.ProjectAllocations.Update(allocation);
        return Task.CompletedTask;
    }

    public async Task AddAsync(ProjectAllocation allocation, CancellationToken cancellationToken = default)
    {
        await context.ProjectAllocations.AddAsync(allocation, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
