using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Server.Common;
using Server.Common.Allocations;
using Tests.Helpers;
using Server.Data;
using Server.Exceptions;
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
        TestDataHelper.SeedRolesAsync(_context).GetAwaiter().GetResult();

        var userRepo = new UserRepository(_context);
        var employeeRepo = new EmployeeRepository(_context);
        var skillRepo = new SkillRepository(_context);
        var employeeSkillRepo = new EmployeeSkillRepository(_context);
        var allocationRepo = new AllocationRepository(_context);
        var projectRepo = new ProjectRepository(_context);
        var timesheetRepo = new TimesheetRepository(_context);
        var auditService = TestServiceFactory.CreateAuditService(_context);
        var roleRepo = TestServiceFactory.CreateRoleRepository(_context);

        _userService = new UserService(
            _context,
            userRepo,
            employeeRepo,
            roleRepo,
            auditService,
            TestServiceFactory.CreateLogger<UserService>());
        _employeeService = new EmployeeService(
            _context,
            employeeRepo,
            userRepo,
            roleRepo,
            skillRepo,
            employeeSkillRepo,
            allocationRepo,
            projectRepo,
            timesheetRepo,
            auditService,
            TestServiceFactory.CreateLogger<EmployeeService>());
    }

    [Fact]
    public async Task GetTeamDashboardAsync_SplitsBenchAndActive()
    {
        var managerUserId = await CreateManagerAsync();
        var benchEmployeeId = await CreateTeamEmployeeAsync(managerUserId, "bench.user", hasAllocation: false);
        var activeEmployeeId = await CreateTeamEmployeeAsync(managerUserId, "active.user", hasAllocation: true);

        var dashboard = await _employeeService.GetTeamDashboardAsync(managerUserId);

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
        var managerUserId = await CreateManagerAsync();
        var otherManagerUserId = await CreateManagerAsync("other.mgr");
        var employeeId = await CreateTeamEmployeeAsync(otherManagerUserId, "other.team", hasAllocation: false);

        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _employeeService.GetTeamMemberDetailAsync(managerUserId, employeeId));
    }

    private async Task<long> CreateManagerAsync(string username = "team.mgr")
    {
        var result = await _userService.CreateUserAccountAsync(1, new CreateUserRequestDto
        {
            FullName = "Team Manager",
            Email = $"{username}@techserve.com",
            Username = username,
            TemporaryPassword = "Welcome1",
            Role = "MANAGER",
            Department = DepartmentConstants.Management,
            Designation = DesignationConstants.DeliveryManager
        });

        var now = DateTime.UtcNow;
        _context.ResourceProfiles.Add(new ResourceProfile
        {
            UserId = result.UserId,
            ResourceStatus = ResourceStatusConstants.Bench,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _context.SaveChangesAsync();

        return result.UserId;
    }

    private async Task<long> CreateTeamEmployeeAsync(long managerUserId, string username, bool hasAllocation)
    {
        var result = await _userService.CreateUserAccountAsync(1, new CreateUserRequestDto
        {
            FullName = username,
            Email = $"{username}@techserve.com",
            Username = username,
            TemporaryPassword = "Welcome1",
            Role = "EMPLOYEE",
            Department = DepartmentConstants.SoftwareDevelopment,
            Designation = DesignationConstants.SoftwareEngineer
        });

        var profile = await _context.ResourceProfiles.FirstAsync(e => e.UserId == result.UserId);
        profile.ManagerId = managerUserId;
        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (hasAllocation)
        {
            var now = DateTime.UtcNow;
            var project = new Project
            {
                ProjectCode = $"PRJ-{profile.Id}",
                ProjectName = $"Project {username}",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31),
                ProjectStatus = "ACTIVE",
                ManagerUserId = managerUserId,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            _context.ProjectAllocations.Add(new ProjectAllocation
            {
                ResourceProfileId = profile.Id,
                ProjectId = project.Id,
                AllocationPercentage = 50,
                AllocationStartDate = new DateOnly(2026, 3, 1),
                AllocationEndDate = new DateOnly(2026, 12, 31),
                AllocationStatus = AllocationStatusConstants.Active,
                AllocatedByUserId = managerUserId,
                CreatedAt = now,
                UpdatedAt = now
            });
            await _context.SaveChangesAsync();
        }

        return profile.Id;
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
