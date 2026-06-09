using Microsoft.EntityFrameworkCore;
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
        await SeedAdminAsync(context, cancellationToken);
        await SeedSampleUsersAsync(context, cancellationToken);
    }

    private static async Task SeedAdminAsync(PrmDbContext context, CancellationToken cancellationToken)
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
                Email = AdminEmail,
                FullName = "System Administrator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
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

    private static async Task SeedSampleUsersAsync(PrmDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(u => u.Username == "ankit.shah", cancellationToken))
            return;

        var now = DateTime.UtcNow;
        var managers = new[]
        {
            ("ankit.shah", "ankit.shah@techserve.com", "Ankit Shah"),
            ("neha.joshi", "neha.joshi@techserve.com", "Neha Joshi")
        };

        var managerUserIds = new List<long>();
        foreach (var (username, email, fullName) in managers)
        {
            var user = CreateUser(username, email, fullName, "MANAGER", now);
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);

            var employee = CreateEmployee(user.Id, null, $"EMP-{user.Id:D6}", "Management", "Delivery Manager", now);
            context.Employees.Add(employee);
            await context.SaveChangesAsync(cancellationToken);
            managerUserIds.Add(user.Id);
        }

        var employees = new[]
        {
            ("ravi.kumar", "ravi.kumar@techserve.com", "Ravi Kumar", "Backend", "ALLOCATED", managerUserIds[0]),
            ("priya.sharma", "priya.sharma@techserve.com", "Priya Sharma", "Frontend", "BENCH", managerUserIds[0]),
            ("anil.mehta", "anil.mehta@techserve.com", "Anil Mehta", "DevOps", "BENCH", managerUserIds[1]),
            ("sara.khan", "sara.khan@techserve.com", "Sara Khan", "QA", "BENCH", managerUserIds[1])
        };

        foreach (var (username, email, fullName, department, status, managerId) in employees)
        {
            var user = CreateUser(username, email, fullName, "EMPLOYEE", now);
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);

            var employee = CreateEmployee(user.Id, managerId, $"EMP-{user.Id:D6}", department, "Consultant", now);
            employee.EmploymentStatus = status;
            context.Employees.Add(employee);
            await context.SaveChangesAsync(cancellationToken);
        }

        await SeedSampleSkillsAsync(context, cancellationToken);
    }

    private static async Task SeedSampleSkillsAsync(PrmDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Skills.AnyAsync(cancellationToken))
            return;

        var now = DateTime.UtcNow;
        var skills = new[]
        {
            ("Java", "BACKEND"),
            ("Spring Boot", "BACKEND"),
            ("React", "FRONTEND"),
            ("Docker", "DEVOPS")
        };

        foreach (var (name, category) in skills)
        {
            context.Skills.Add(new Skill
            {
                SkillName = name,
                Category = category,
                IsActive = true,
                CreatedAt = now
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        var ravi = await context.Employees
            .Join(context.Users, e => e.UserId, u => u.Id, (e, u) => new { e, u })
            .FirstOrDefaultAsync(x => x.u.Username == "ravi.kumar", cancellationToken);

        if (ravi is null) return;

        var java = await context.Skills.FirstAsync(s => s.SkillName == "Java", cancellationToken);
        var spring = await context.Skills.FirstAsync(s => s.SkillName == "Spring Boot", cancellationToken);

        context.EmployeeSkills.AddRange(
            new EmployeeSkill { EmployeeId = ravi.e.Id, SkillId = java.Id, ProficiencyLevel = "INTERMEDIATE", CreatedAt = now },
            new EmployeeSkill { EmployeeId = ravi.e.Id, SkillId = spring.Id, ProficiencyLevel = "ADVANCED", CreatedAt = now });

        await context.SaveChangesAsync(cancellationToken);
    }

    private static User CreateUser(string username, string email, string fullName, string role, DateTime now) =>
        new()
        {
            Username = username,
            Email = email,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Welcome1"),
            Role = role,
            ForcePasswordChange = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

    private static Employee CreateEmployee(
        long userId,
        long? managerId,
        string employeeCode,
        string department,
        string designation,
        DateTime now) =>
        new()
        {
            UserId = userId,
            ManagerId = managerId,
            EmployeeCode = employeeCode,
            Department = department,
            Designation = designation,
            EmploymentStatus = "BENCH",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
}
