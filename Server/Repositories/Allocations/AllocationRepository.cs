using Microsoft.EntityFrameworkCore;
using Server.Common;
using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Allocations;

public class AllocationRepository(PrmDbContext context) : IAllocationRepository
{
    public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default) =>
        await context.ProjectAllocations
            .Where(a => a.EmployeeId == employeeId && a.AllocationStatus == TimesheetConstants.AllocationStatusActive)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdsAsync(
        IEnumerable<long> employeeIds,
        CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.ToList();
        if (ids.Count == 0)
            return [];

        return await context.ProjectAllocations
            .Where(a => ids.Contains(a.EmployeeId) && a.AllocationStatus == TimesheetConstants.AllocationStatusActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdForWeekAsync(
        long employeeId,
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken cancellationToken = default) =>
        await context.ProjectAllocations
            .Where(a => a.EmployeeId == employeeId
                && a.AllocationStatus == "ACTIVE"
                && a.AllocationStartDate <= weekEnd
                && a.AllocationEndDate >= weekStart)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectAllocation>> GetByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken = default) =>
        await context.ProjectAllocations
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.AllocationStartDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByEmployeeIdsForWeekAsync(
        IEnumerable<long> employeeIds,
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken cancellationToken = default)
    {
        var ids = employeeIds.ToList();
        if (ids.Count == 0)
            return [];

        return await context.ProjectAllocations
            .Where(a => ids.Contains(a.EmployeeId)
                && a.AllocationStatus == "ACTIVE"
                && a.AllocationStartDate <= weekEnd
                && a.AllocationEndDate >= weekStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectAllocation>> GetAllActiveForWeekAsync(
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken cancellationToken = default) =>
        await context.ProjectAllocations
            .Where(a => a.AllocationStatus == "ACTIVE"
                && a.AllocationStartDate <= weekEnd
                && a.AllocationEndDate >= weekStart)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectAllocation>> GetActiveByProjectIdAsync(
        long projectId,
        CancellationToken cancellationToken = default) =>
        await context.ProjectAllocations
            .Where(a => a.ProjectId == projectId && a.AllocationStatus == "ACTIVE")
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

    public Task<ProjectAllocation?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.ProjectAllocations.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ProjectAllocation>> GetAllAsync(
        long? employeeId,
        long? projectId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = context.ProjectAllocations.AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);

        if (projectId.HasValue)
            query = query.Where(a => a.ProjectId == projectId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.AllocationStatus == status.Trim().ToUpperInvariant());
        else
            query = query.Where(a => a.AllocationStatus == "ACTIVE");

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
