using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;

namespace Server.Seed;

public static class DatabaseSeeder
{
    private const string AdminUsername = "admin";

    public static async Task SeedAsync(PrmDbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(u => u.Username == AdminUsername, cancellationToken))
            return;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;

            context.Users.Add(new User
            {
                Username = AdminUsername,
                Email = "admin@lctechserve.com",
                FullName = "System Administrator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234@"),
                Role = "ADMIN",
                ForcePasswordChange = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
