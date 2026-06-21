using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Roles;

public class RoleRepository(PrmDbContext context) : IRoleRepository
{
    public Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default) =>
        context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName, cancellationToken);

    public async Task<string?> GetRoleNameForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var roleName = await context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.RoleName)
            .FirstOrDefaultAsync(cancellationToken);

        return roleName;
    }

    public async Task<IReadOnlyDictionary<long, string>> GetRoleNamesForUsersAsync(
        IEnumerable<long> userIds,
        CancellationToken cancellationToken = default)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0)
            return new Dictionary<long, string>();

        var rows = await context.UserRoles
            .Where(ur => idList.Contains(ur.UserId))
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.RoleName })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.First().RoleName);
    }

    public Task<bool> UserHasRoleAsync(long userId, string roleName, CancellationToken cancellationToken = default) =>
        context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.RoleName)
            .AnyAsync(name => name == roleName, cancellationToken);

    public async Task AssignRoleAsync(
        long userId,
        long roleId,
        long? assignedByUserId,
        CancellationToken cancellationToken = default)
    {
        await context.UserRoles.AddAsync(new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedByUserId = assignedByUserId,
            AssignedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task ReplaceUserRoleAsync(
        long userId,
        long newRoleId,
        long? assignedByUserId,
        CancellationToken cancellationToken = default)
    {
        var existingRoles = await context.UserRoles
            .AsTracking()
            .Where(ur => ur.UserId == userId)
            .ToListAsync(cancellationToken);

        context.UserRoles.RemoveRange(existingRoles);

        await context.UserRoles.AddAsync(new UserRole
        {
            UserId = userId,
            RoleId = newRoleId,
            AssignedByUserId = assignedByUserId,
            AssignedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
