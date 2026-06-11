using Server.Common.Roles;
using Server.Data;
using Server.Models.Entities;

namespace Tests.Helpers;

public static class TestDataHelper
{
    public static async Task SeedRolesAsync(PrmDbContext context)
    {
        if (context.Roles.Any())
            return;

        var now = DateTime.UtcNow;
        context.Roles.AddRange(
            new Role { RoleName = RoleConstants.Admin, CreatedAt = now },
            new Role { RoleName = RoleConstants.Manager, CreatedAt = now },
            new Role { RoleName = RoleConstants.Employee, CreatedAt = now });
        await context.SaveChangesAsync();
    }

    public static async Task<(User User, ResourceProfile Profile)> CreateUserWithProfileAsync(
        PrmDbContext context,
        string username,
        string role,
        long? managerId = null,
        string resourceStatus = "BENCH")
    {
        await SeedRolesAsync(context);
        var now = DateTime.UtcNow;
        var roleEntity = context.Roles.First(r => r.RoleName == role);

        var user = new User
        {
            Username = username,
            Email = $"{username}@test.com",
            FullName = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Welcome1"),
            IsTemporaryPassword = false,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = roleEntity.Id,
            AssignedAt = now
        });

        var profile = new ResourceProfile
        {
            UserId = user.Id,
            ManagerId = managerId,
            ResourceStatus = resourceStatus,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.ResourceProfiles.Add(profile);
        await context.SaveChangesAsync();

        return (user, profile);
    }
}
