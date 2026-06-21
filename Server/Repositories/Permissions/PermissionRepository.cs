using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Permissions;

public class PermissionRepository(PrmDbContext context) : IPermissionRepository
{
    public async Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default) =>
        await context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<long, int>> GetUserCountsByRoleAsync(
        CancellationToken cancellationToken = default)
    {
        var counts = await context.UserRoles
            .AsNoTracking()
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.RoleId, x => x.Count);
    }

    public Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default) =>
        context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleName == roleName, cancellationToken);
}
