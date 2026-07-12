using Microsoft.EntityFrameworkCore;
using Server.Common;
using Server.Common.Roles;
using Server.Data;
using Server.Models.Entities;

namespace Server.Seed;

public static class DatabaseSeeder
{
    private const string AdminUsername = "admin";
    private const string AdminEmail = "admin@techserve.com";
    private const string AdminPassword = "Admin@1234";

    public static async Task SeedAsync(PrmDbContext context, CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(context, cancellationToken);
        await SeedAdminAsync(context, cancellationToken);
    }

    private static async Task SeedRolesAsync(PrmDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Roles.AnyAsync(cancellationToken))
            return;

        var now = DateTime.UtcNow;
        context.Roles.AddRange(
            new Role { RoleName = RoleConstants.Admin, CreatedAt = now },
            new Role { RoleName = RoleConstants.Manager, CreatedAt = now },
            new Role { RoleName = RoleConstants.Employee, CreatedAt = now });

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAdminAsync(PrmDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(u => u.Username == AdminUsername, cancellationToken))
            return;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var adminRole = await context.Roles.FirstAsync(r => r.RoleName == RoleConstants.Admin, cancellationToken);

            var user = new User
            {
                Username = AdminUsername,
                Email = AdminEmail,
                FullName = "System Administrator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
                Department = DepartmentConstants.HrOps,
                Designation = DesignationConstants.SystemAdministrator,
                IsTemporaryPassword = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);

            context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = adminRole.Id,
                AssignedAt = now
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
