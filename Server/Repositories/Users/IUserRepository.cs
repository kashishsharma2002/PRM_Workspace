using Server.Models.Entities;

namespace Server.Repositories.Users;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<long, User>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken = default);
    Task<ResourceProfile?> GetResourceProfileByUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetActiveUsersByRoleAsync(
        string roleName,
        CancellationToken cancellationToken = default);
}
