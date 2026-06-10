using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Employees;
using Server.Models.DTOs.Users;
using Server.Models.Entities;

namespace Tests;

public class EmployeeServiceTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly UserService _userService;
    private readonly EmployeeService _employeeService;

    public EmployeeServiceTests()
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
        var auditRepo = new AuditLogRepository(_context);

        _userService = new UserService(_context, userRepo, employeeRepo, auditRepo);
        var timesheetRepo = new TimesheetRepository(_context);

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

    private async Task<(long EmployeeId, long UserId)> CreateEmployeeAsync(string username = "emp.user")
    {
        var result = await _userService.CreateUserAccountAsync(1, new CreateUserRequestDto
        {
            FullName = "Test Employee",
            Email = $"{username}@techserve.com",
            Username = username,
            TemporaryPassword = "Welcome1",
            Role = "EMPLOYEE"
        });

        var employee = await _context.Employees.FirstAsync(e => e.UserId == result.UserId);
        return (employee.Id, result.UserId);
    }

    private async Task<long> CreateManagerAsync(string username = "mgr.user")
    {
        var result = await _userService.CreateUserAccountAsync(1, new CreateUserRequestDto
        {
            FullName = "Test Manager",
            Email = $"{username}@techserve.com",
            Username = username,
            TemporaryPassword = "Welcome1",
            Role = "MANAGER"
        });
        return result.UserId;
    }

    [Fact]
    public async Task UpdateEmployeeAsync_UpdatesDepartmentAndDesignation()
    {
        var (employeeId, _) = await CreateEmployeeAsync();

        await _employeeService.UpdateEmployeeAsync(employeeId, new UpdateEmployeeRequestDto
        {
            Department = "Backend",
            Designation = "Developer"
        });

        var employee = await _context.Employees.FindAsync(employeeId);
        Assert.NotNull(employee);
        Assert.Equal("Backend", employee!.Department);
        Assert.Equal("Developer", employee.Designation);
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_EndsAllocationsAndBlocksLogin()
    {
        var (employeeId, userId) = await CreateEmployeeAsync("deact.emp");
        var managerUserId = await CreateManagerAsync();

        var now = DateTime.UtcNow;
        var project = new Project
        {
            ProjectCode = "PRJ-001",
            ProjectName = "Alpha Portal",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
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
            EmployeeId = employeeId,
            ProjectId = project.Id,
            AllocationPercentage = 50,
            AllocationStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            AllocationEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)),
            AllocationStatus = "ACTIVE",
            AllocatedByManagerId = managerUserId,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _context.SaveChangesAsync();

        await _employeeService.DeactivateEmployeeAsync(999, employeeId);

        var employee = await _context.Employees.FindAsync(employeeId);
        var user = await _context.Users.FindAsync(userId);
        var allocation = await _context.ProjectAllocations.FirstAsync();
        var audit = await _context.AuditLogs.FirstAsync(a => a.EntityName == "EMPLOYEES");

        Assert.NotNull(employee);
        Assert.False(employee!.IsActive);
        Assert.Equal("BENCH", employee.EmploymentStatus);
        Assert.NotNull(user);
        Assert.False(user!.IsActive);
        Assert.Equal("ENDED", allocation.AllocationStatus);
        Assert.Equal("DEACTIVATE", audit.ActionType);
    }

    [Fact]
    public async Task AddSkillAsync_DuplicateSkill_ThrowsConflict()
    {
        var (employeeId, _) = await CreateEmployeeAsync("skill.emp");

        var request = new AddSkillRequestDto
        {
            SkillName = "Java",
            Category = "BACKEND",
            ProficiencyLevel = "INTERMEDIATE"
        };

        await _employeeService.AddSkillAsync(employeeId, request);

        await Assert.ThrowsAsync<ConflictAppException>(() =>
            _employeeService.AddSkillAsync(employeeId, request));
    }

    [Fact]
    public async Task AssignManagerAsync_InvalidManager_ThrowsValidation()
    {
        var (employeeId, userId) = await CreateEmployeeAsync("assign.emp");

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            _employeeService.AssignManagerAsync(employeeId, new AssignManagerRequestDto
            {
                ManagerUserId = userId
            }));
    }

    [Fact]
    public async Task AssignManagerAsync_ValidManager_SetsManagerId()
    {
        var (employeeId, _) = await CreateEmployeeAsync("assign2.emp");
        var managerUserId = await CreateManagerAsync("assign.mgr");

        await _employeeService.AssignManagerAsync(employeeId, new AssignManagerRequestDto
        {
            ManagerUserId = managerUserId
        });

        var employee = await _context.Employees.FindAsync(employeeId);
        Assert.NotNull(employee);
        Assert.Equal(managerUserId, employee!.ManagerId);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
