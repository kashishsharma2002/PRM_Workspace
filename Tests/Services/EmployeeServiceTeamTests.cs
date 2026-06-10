using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Server.Common;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Employees;
using Server.Models.DTOs.Users;
using Server.Models.Entities;

namespace Tests;

public class EmployeeServiceTeamTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly EmployeeService _employeeService;
    private readonly UserService _userService;

    public EmployeeServiceTeamTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);

        var userRepo = new UserRepository(_context);
        var employeeRepo = new EmployeeRepository(_context);
        var skillRepo = new SkillRepository(_context);
        var employeeSkillRepo = new EmployeeSkillRepository(_context);
        var allocationRepo = new AllocationRepository(_context);
        var projectRepo = new ProjectRepository(_context);
        var timesheetRepo = new TimesheetRepository(_context);
        var auditRepo = new AuditLogRepository(_context);

        _userService = new UserService(_context, userRepo, employeeRepo, auditRepo);
        _employeeService = new EmployeeService(
            _context,
            employeeRepo,
            userRepo,
            skillRepo,
            employeeSkillRepo,
            allocationRepo,
            projectRepo,
            timesheetRepo,
            auditRepo);
    }

    [Fact]
    public async Task GetTeamDashboardAsync_SplitsBenchAndActive()
    {
        var managerId = await CreateManagerAsync();
        var benchEmployeeId = await CreateTeamEmployeeAsync(managerId, "bench.user", hasAllocation: false);
        var activeEmployeeId = await CreateTeamEmployeeAsync(managerId, "active.user", hasAllocation: true);

        var dashboard = await _employeeService.GetTeamDashboardAsync(managerId);

        Assert.Equal(1, dashboard.BenchCount);
        Assert.Single(dashboard.BenchEmployees);
        Assert.Equal(benchEmployeeId, dashboard.BenchEmployees[0].Id);
        Assert.Single(dashboard.ActiveEmployees);
        Assert.Equal(activeEmployeeId, dashboard.ActiveEmployees[0].Id);
        Assert.Equal(1, dashboard.PartialCount);
    }

    [Fact]
    public async Task GetTeamMemberDetailAsync_RejectsOutOfScopeEmployee()
    {
        var managerId = await CreateManagerAsync();
        var otherManagerId = await CreateManagerAsync("other.mgr");
        var employeeId = await CreateTeamEmployeeAsync(otherManagerId, "other.team", hasAllocation: false);

        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _employeeService.GetTeamMemberDetailAsync(managerId, employeeId));
    }

    private async Task<long> CreateManagerAsync(string username = "team.mgr")
    {
        var result = await _userService.CreateUserAccountAsync(1, new CreateUserRequestDto
        {
            FullName = "Team Manager",
            Email = $"{username}@techserve.com",
            Username = username,
            TemporaryPassword = "Welcome1",
            Role = "MANAGER"
        });
        return result.UserId;
    }

    private async Task<long> CreateTeamEmployeeAsync(long managerId, string username, bool hasAllocation)
    {
        var result = await _userService.CreateUserAccountAsync(1, new CreateUserRequestDto
        {
            FullName = username,
            Email = $"{username}@techserve.com",
            Username = username,
            TemporaryPassword = "Welcome1",
            Role = "EMPLOYEE"
        });

        var employee = await _context.Employees.FirstAsync(e => e.UserId == result.UserId);
        employee.ManagerId = managerId;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (hasAllocation)
        {
            var now = DateTime.UtcNow;
            var project = new Project
            {
                ProjectCode = $"PRJ-{employee.Id}",
                ProjectName = $"Project {username}",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31),
                ProjectStatus = "ACTIVE",
                ManagerUserId = managerId,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            _context.ProjectAllocations.Add(new ProjectAllocation
            {
                EmployeeId = employee.Id,
                ProjectId = project.Id,
                AllocationPercentage = 50,
                AllocationStartDate = new DateOnly(2026, 3, 1),
                AllocationEndDate = new DateOnly(2026, 12, 31),
                AllocationStatus = TimesheetConstants.AllocationStatusActive,
                AllocatedByManagerId = managerId,
                CreatedAt = now,
                UpdatedAt = now
            });
            await _context.SaveChangesAsync();
        }

        return employee.Id;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
