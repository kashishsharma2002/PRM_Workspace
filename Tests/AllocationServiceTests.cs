using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories;
using Server.Services;

namespace Tests;

public class AllocationServiceTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly AllocationService _allocationService;

    public AllocationServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        SeedData();

        _allocationService = new AllocationService(
            new AllocationRepository(_context),
            new EmployeeRepository(_context),
            new UserRepository(_context),
            new ProjectRepository(_context));
    }

    private void SeedData()
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Username = "ravi.kumar",
            Email = "ravi@techserve.com",
            FullName = "Ravi Kumar",
            PasswordHash = "hash",
            Role = "EMPLOYEE",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        var employee = new Employee
        {
            UserId = user.Id,
            EmployeeCode = "EMP-000001",
            EmploymentStatus = "ALLOCATED",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Employees.Add(employee);

        var manager = new User
        {
            Username = "ankit.shah",
            Email = "ankit@techserve.com",
            FullName = "Ankit Shah",
            PasswordHash = "hash",
            Role = "MANAGER",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.Add(manager);
        _context.SaveChanges();

        var project = new Project
        {
            ProjectCode = "PRJ-000201",
            ProjectName = "Alpha Portal",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            ProjectStatus = "ACTIVE",
            ManagerUserId = manager.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Projects.Add(project);
        _context.SaveChanges();

        _context.ProjectAllocations.Add(new ProjectAllocation
        {
            EmployeeId = employee.Id,
            ProjectId = project.Id,
            AllocationPercentage = 50,
            AllocationStartDate = new DateOnly(2026, 3, 1),
            AllocationEndDate = new DateOnly(2026, 6, 30),
            AllocationStatus = "ACTIVE",
            AllocatedByManagerId = manager.Id,
            CreatedAt = now,
            UpdatedAt = now
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllAllocationsAsync_ReturnsJoinedNames()
    {
        var result = await _allocationService.GetAllAllocationsAsync(null, null, null);

        Assert.Single(result.Allocations);
        Assert.Equal("Ravi Kumar", result.Allocations[0].EmployeeName);
        Assert.Equal("Alpha Portal", result.Allocations[0].ProjectName);
        Assert.Equal(1, result.TotalActiveCount);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
