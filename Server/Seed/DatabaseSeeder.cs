using Microsoft.EntityFrameworkCore;
using Server.Common;
using Server.Common.Allocations;
using Server.Common.Projects;
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
        await SeedAdminAsync(context, cancellationToken);
        await SeedSampleUsersAsync(context, cancellationToken);
        await SeedSampleProjectsAsync(context, cancellationToken);
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
                Role = RoleConstants.Admin,
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
            var user = CreateUser(username, email, fullName, RoleConstants.Manager, now);
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);

            var employee = CreateEmployee(user.Id, null, $"EMP-{user.Id:D6}", "Management", "Delivery Manager", now);
            context.Employees.Add(employee);
            await context.SaveChangesAsync(cancellationToken);
            managerUserIds.Add(user.Id);
        }

        var employees = new[]
        {
            ("ravi.kumar", "ravi.kumar@techserve.com", "Ravi Kumar", "Backend", AllocationConstants.EmploymentStatusAllocated, managerUserIds[0]),
            ("priya.sharma", "priya.sharma@techserve.com", "Priya Sharma", "Frontend", AllocationConstants.EmploymentStatusBench, managerUserIds[0]),
            ("anil.mehta", "anil.mehta@techserve.com", "Anil Mehta", "DevOps", AllocationConstants.EmploymentStatusBench, managerUserIds[1]),
            ("sara.khan", "sara.khan@techserve.com", "Sara Khan", "QA", AllocationConstants.EmploymentStatusBench, managerUserIds[1])
        };

        foreach (var (username, email, fullName, department, status, managerId) in employees)
        {
            var user = CreateUser(username, email, fullName, RoleConstants.Employee, now);
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

    private static async Task SeedSampleProjectsAsync(PrmDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Projects.AnyAsync(p => p.ProjectName == "Alpha Portal", cancellationToken))
            return;

        var ankit = await context.Users.FirstOrDefaultAsync(u => u.Username == "ankit.shah", cancellationToken);
        var neha = await context.Users.FirstOrDefaultAsync(u => u.Username == "neha.joshi", cancellationToken);
        if (ankit is null || neha is null)
            return;

        var now = DateTime.UtcNow;
        var projects = new[]
        {
            ("Alpha Portal", "Customer web portal", new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), ProjectStatusConstants.Active, 120, ankit.Id),
            ("Beta CRM", "CRM modernization", new DateOnly(2026, 2, 1), new DateOnly(2026, 8, 15), ProjectStatusConstants.Active, 80, ankit.Id),
            ("Gamma Rewrite", "Legacy rewrite", new DateOnly(2026, 2, 1), new DateOnly(2026, 7, 1), ProjectStatusConstants.Active, 60, neha.Id),
            ("Delta Migrate", "Data migration", new DateOnly(2026, 4, 1), new DateOnly(2026, 9, 30), ProjectStatusConstants.Planned, 100, neha.Id)
        };

        var projectIds = new List<long>();
        foreach (var (name, desc, start, end, status, sp, managerId) in projects)
        {
            var project = new Project
            {
                ProjectCode = "TEMP",
                ProjectName = name,
                Description = desc,
                StartDate = start,
                EndDate = end,
                ProjectStatus = status,
                HealthStatus = HealthStatusConstants.Green,
                TotalStoryPoints = sp,
                ManagerUserId = managerId,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.Projects.Add(project);
            await context.SaveChangesAsync(cancellationToken);
            project.ProjectCode = $"PRJ-{project.Id:D6}";
            projectIds.Add(project.Id);
        }

        await context.SaveChangesAsync(cancellationToken);

        if (projectIds.Count >= 3)
        {
            context.ProjectMilestones.AddRange(
                new ProjectMilestone { ProjectId = projectIds[0], MilestoneTitle = "Design Complete", DueDate = new DateOnly(2026, 4, 1), StoryPoints = 20, MilestoneStatus = MilestoneStatusConstants.Done, SortOrder = 1, CreatedAt = now, UpdatedAt = now },
                new ProjectMilestone { ProjectId = projectIds[0], MilestoneTitle = "Backend API", DueDate = new DateOnly(2026, 4, 15), StoryPoints = 40, MilestoneStatus = MilestoneStatusConstants.InProgress, SortOrder = 2, CreatedAt = now, UpdatedAt = now },
                new ProjectMilestone { ProjectId = projectIds[0], MilestoneTitle = "Testing", DueDate = new DateOnly(2026, 4, 30), StoryPoints = 35, MilestoneStatus = MilestoneStatusConstants.NotStarted, SortOrder = 3, CreatedAt = now, UpdatedAt = now },
                new ProjectMilestone { ProjectId = projectIds[0], MilestoneTitle = "Go Live", DueDate = new DateOnly(2026, 5, 15), StoryPoints = 25, MilestoneStatus = MilestoneStatusConstants.NotStarted, SortOrder = 4, CreatedAt = now, UpdatedAt = now },
                new ProjectMilestone { ProjectId = projectIds[1], MilestoneTitle = "Requirements", DueDate = new DateOnly(2026, 3, 15), StoryPoints = 15, MilestoneStatus = MilestoneStatusConstants.Done, SortOrder = 1, CreatedAt = now, UpdatedAt = now },
                new ProjectMilestone { ProjectId = projectIds[2], MilestoneTitle = "Architecture", DueDate = new DateOnly(2026, 3, 1), StoryPoints = 10, MilestoneStatus = MilestoneStatusConstants.InProgress, SortOrder = 1, CreatedAt = now, UpdatedAt = now });

            await context.SaveChangesAsync(cancellationToken);
        }

        var ravi = await context.Employees
            .Join(context.Users, e => e.UserId, u => u.Id, (e, u) => new { e, u })
            .FirstOrDefaultAsync(x => x.u.Username == "ravi.kumar", cancellationToken);

        if (ravi is not null && projectIds.Count >= 2)
        {
            context.ProjectAllocations.AddRange(
                new ProjectAllocation
                {
                    EmployeeId = ravi.e.Id,
                    ProjectId = projectIds[0],
                    AllocationPercentage = 50,
                    AllocationStartDate = new DateOnly(2026, 3, 1),
                    AllocationEndDate = new DateOnly(2026, 6, 30),
                    AllocationStatus = AllocationStatusConstants.Active,
                    AllocatedByManagerId = ankit.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new ProjectAllocation
                {
                    EmployeeId = ravi.e.Id,
                    ProjectId = projectIds[1],
                    AllocationPercentage = 50,
                    AllocationStartDate = new DateOnly(2026, 4, 1),
                    AllocationEndDate = new DateOnly(2026, 7, 31),
                    AllocationStatus = AllocationStatusConstants.Active,
                    AllocatedByManagerId = ankit.Id,
                    CreatedAt = now,
                    UpdatedAt = now
                });

            await context.SaveChangesAsync(cancellationToken);
        }
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
