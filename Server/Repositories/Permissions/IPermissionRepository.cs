using Server.Models.Entities;

namespace Server.Repositories.Permissions;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<long, int>> GetUserCountsByRoleAsync(CancellationToken cancellationToken = default);
    Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);
}
