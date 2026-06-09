using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class UserRepository(PrmDbContext context) : IUserRepository
{
    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyDictionary<long, User>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return new Dictionary<long, User>();

        var users = await context.Users.Where(u => idList.Contains(u.Id)).ToListAsync(cancellationToken);
        return users.ToDictionary(u => u.Id);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Users.OrderBy(u => u.Id).ToListAsync(cancellationToken);

    public Task<bool> ExistsByUsernameOrEmailAsync(string username, string email, CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(
            u => u.Username == username || u.Email == email,
            cancellationToken);

    public Task<Employee?> GetEmployeeByUserIdAsync(long userId, CancellationToken cancellationToken = default) =>
        context.Employees.FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await context.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
