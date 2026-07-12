namespace Server.Repositories.Roles;

public interface IRoleRepository
{
    Task<Models.Entities.Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<string?> GetRoleNameForUserAsync(long userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<long, string>> GetRoleNamesForUsersAsync(IEnumerable<long> userIds, CancellationToken cancellationToken = default);
    Task<bool> UserHasRoleAsync(long userId, string roleName, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(long userId, long roleId, long? assignedByUserId, CancellationToken cancellationToken = default);
    Task ReplaceUserRoleAsync(long userId, long newRoleId, long? assignedByUserId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
