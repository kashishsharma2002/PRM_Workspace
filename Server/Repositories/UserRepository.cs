using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class UserRepository(PrmDbContext context) : IUserRepository
{
    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<Employee?> GetEmployeeByUserIdAsync(long userId, CancellationToken cancellationToken = default) =>
        context.Employees.FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);
    }
}
