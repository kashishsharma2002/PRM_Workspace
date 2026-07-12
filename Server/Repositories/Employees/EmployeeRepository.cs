using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Employees;

public class EmployeeRepository(PrmDbContext context) : IEmployeeRepository
{
    public Task<ResourceProfile?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default) =>
        context.ResourceProfiles.FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);

    public Task<ResourceProfile?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.ResourceProfiles
            .AsTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<long, ResourceProfile>> GetByIdsAsync(
        IEnumerable<long> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return new Dictionary<long, ResourceProfile>();

        var profiles = await context.ResourceProfiles
            .Where(r => idList.Contains(r.Id))
            .ToListAsync(cancellationToken);

        return profiles.ToDictionary(r => r.Id);
    }

    public async Task<IReadOnlyList<ResourceProfile>> GetAllAsync(
        string? resourceStatus,
        string? department,
        CancellationToken cancellationToken = default)
    {
        var query = context.ResourceProfiles.AsQueryable();

        if (!string.IsNullOrWhiteSpace(resourceStatus))
            query = query.Where(r => r.ResourceStatus == resourceStatus.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(department))
        {
            var dept = department.Trim().ToUpperInvariant();
            query = query.Where(r =>
                context.Users.Any(u => u.Id == r.UserId && u.Department == dept));
        }

        return await query.OrderBy(r => r.Id).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceProfile>> GetByManagerIdAsync(
        long managerId,
        CancellationToken cancellationToken = default) =>
        await context.ResourceProfiles
            .Where(r => r.ManagerId == managerId)
            .Join(
                context.Users,
                r => r.UserId,
                u => u.Id,
                (r, u) => new { Profile = r, User = u })
            .Where(x => x.User.IsActive)
            .Select(x => x.Profile)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ResourceProfile resourceProfile, CancellationToken cancellationToken = default)
    {
        await context.ResourceProfiles.AddAsync(resourceProfile, cancellationToken);
    }

    public Task UpdateAsync(ResourceProfile resourceProfile, CancellationToken cancellationToken = default)
    {
        context.ResourceProfiles.Update(resourceProfile);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<ResourceProfile>> GetResourceProfilesByUserIdsAsync(
        IEnumerable<long> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        return await context.ResourceProfiles
            .Where(rp => ids.Contains(rp.UserId))
            .ToListAsync(cancellationToken);
    }
}
