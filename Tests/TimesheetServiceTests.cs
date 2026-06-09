using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Server.Common;
using Server.Data;
using Server.Exceptions;
using Server.Models.Entities;
using Server.Repositories;
using Server.Services;
using Tests.Helpers;

namespace Tests;

public class TimesheetServiceTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly TimesheetService _timesheetService;
    private readonly long _employeeId;
    private readonly long _userId;
    private readonly long _projectId;
    private readonly DateOnly _weekStart = new(2026, 5, 12);

    public TimesheetServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        (_employeeId, _userId, _projectId) = SeedData();

        _timesheetService = new TimesheetService(
            _context,
            new TimesheetRepository(_context),
            new AllocationRepository(_context),
            new ProjectRepository(_context),
            new EmployeeRepository(_context),
            new UserRepository(_context),
            new ActivityTagRepository(_context),
            new SystemConfigRepository(_context),
            new AuditLogRepository(_context),
            new MemoryCache(new MemoryCacheOptions()));
    }

    private (long employeeId, long userId, long projectId) SeedData()
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

        _context.ActivityTags.Add(new ActivityTag
        {
            TagCode = "BACKEND_API",
            TagName = "Backend API Development",
            TagCategory = "Backend",
            IsActive = true,
            CreatedAt = now
        });

        _context.SystemConfigurations.Add(new SystemConfiguration
        {
            ConfigKey = ConfigKeys.MaxWeeklyHours,
            ConfigValue = "40",
            UpdatedAt = now
        });

        _context.SaveChanges();
        return (employee.Id, user.Id, project.Id);
    }

    [Fact]
    public async Task SubmitTimesheetAsync_ValidRequest_CreatesTimesheet()
    {
        var request = new TimesheetTestDataBuilder()
            .WithWeekStartDate(_weekStart)
            .WithProjectId(_projectId)
            .WithHoursPerProject(18)
            .WithActivityTagIds(1)
            .BuildSubmitRequest();

        var result = await _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request);

        Assert.True(result.TimesheetId > 0);
        Assert.Equal("SUBMITTED", result.Status);
        Assert.Equal(18, result.TotalHours);
    }

    [Fact]
    public async Task SubmitTimesheetAsync_DuplicateWeek_ThrowsConflict()
    {
        var request = new TimesheetTestDataBuilder()
            .WithWeekStartDate(_weekStart)
            .WithProjectId(_projectId)
            .WithHoursPerProject(10)
            .BuildSubmitRequest();

        await _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request);

        await Assert.ThrowsAsync<ConflictAppException>(
            () => _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request));
    }

    [Fact]
    public async Task SubmitTimesheetAsync_FutureWeek_ThrowsValidation()
    {
        var futureMonday = WeekDateHelper.GetCurrentWeekMonday().AddDays(7);
        var request = new TimesheetTestDataBuilder()
            .WithWeekStartDate(futureMonday)
            .WithProjectId(_projectId)
            .BuildSubmitRequest();

        await Assert.ThrowsAsync<ValidationAppException>(
            () => _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request));
    }

    [Fact]
    public async Task SubmitTimesheetAsync_HoursExceedAllocation_ThrowsValidation()
    {
        var request = new TimesheetTestDataBuilder()
            .WithWeekStartDate(_weekStart)
            .WithProjectId(_projectId)
            .WithHoursPerProject(25)
            .BuildSubmitRequest();

        var ex = await Assert.ThrowsAsync<ValidationAppException>(
            () => _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request));

        Assert.Contains("exceeds allocation cap", ex.Message);
    }

    [Fact]
    public async Task SubmitTimesheetAsync_TotalHoursExceedMaxWeekly_ThrowsValidation()
    {
        var allocation = await _context.ProjectAllocations.FirstAsync();
        allocation.AllocationPercentage = 100;
        await _context.SaveChangesAsync();

        var request = new TimesheetTestDataBuilder()
            .WithWeekStartDate(_weekStart)
            .WithProjectId(_projectId)
            .WithHoursPerProject(41)
            .BuildSubmitRequest();

        var ex = await Assert.ThrowsAsync<ValidationAppException>(
            () => _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request));

        Assert.Contains("maximum weekly limit", ex.Message);
    }

    [Fact]
    public async Task SubmitTimesheetAsync_UnallocatedProject_ThrowsValidation()
    {
        var request = new TimesheetTestDataBuilder()
            .WithWeekStartDate(_weekStart)
            .WithProjectId(9999)
            .WithHoursPerProject(10)
            .BuildSubmitRequest();

        var ex = await Assert.ThrowsAsync<ValidationAppException>(
            () => _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request));

        Assert.Contains("not allocated", ex.Message);
    }

    [Fact]
    public async Task GetMyTimesheetsAsync_ReturnsSubmittedHistory()
    {
        var request = new TimesheetTestDataBuilder()
            .WithWeekStartDate(_weekStart)
            .WithProjectId(_projectId)
            .WithHoursPerProject(15)
            .BuildSubmitRequest();

        await _timesheetService.SubmitTimesheetAsync(_employeeId, _userId, request);

        var history = await _timesheetService.GetMyTimesheetsAsync(_employeeId);

        Assert.Single(history);
        Assert.Equal(_weekStart, history[0].WeekStartDate);
        Assert.Equal("SUBMITTED", history[0].Status);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
