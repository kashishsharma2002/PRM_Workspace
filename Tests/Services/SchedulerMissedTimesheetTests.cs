using Microsoft.EntityFrameworkCore;
using Tests.Helpers;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Server.Common;
using Server.Data;
using Server.Models.Entities;

namespace Tests;

public class SchedulerMissedTimesheetTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly TimesheetService _timesheetService;
    private readonly long _employeeWithAllocationId;
    private readonly long _employeeWithSubmissionId;
    private readonly DateOnly _lastWeek;

    public SchedulerMissedTimesheetTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        _lastWeek = WeekDateHelper.GetMostRecentCompletedWeekMonday();
        (_employeeWithAllocationId, _employeeWithSubmissionId) = SeedData();

        _timesheetService = new TimesheetService(
            _context,
            new TimesheetRepository(_context),
            new AllocationRepository(_context),
            new ProjectRepository(_context),
            new EmployeeRepository(_context),
            new UserRepository(_context),
            new ActivityTagRepository(_context),
            new SystemConfigRepository(_context),
            TestServiceFactory.CreateAuditService(_context),
            new MemoryCache(new MemoryCacheOptions()),
            TestServiceFactory.CreateLogger<TimesheetService>());
    }

    private (long missingId, long submittedId) SeedData()
    {
        var now = DateTime.UtcNow;
        var weekEnd = WeekDateHelper.GetWeekEnd(_lastWeek);

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

        var raviUser = new User
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
        var priyaUser = new User
        {
            Username = "priya.singh",
            Email = "priya@techserve.com",
            FullName = "Priya Singh",
            PasswordHash = "hash",
            Role = "EMPLOYEE",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.AddRange(raviUser, priyaUser);
        _context.SaveChanges();

        var ravi = new Employee
        {
            UserId = raviUser.Id,
            EmployeeCode = "EMP-000001",
            Designation = "Developer",
            Department = "Engineering",
            EmploymentStatus = "ACTIVE",
            ManagerId = manager.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        var priya = new Employee
        {
            UserId = priyaUser.Id,
            EmployeeCode = "EMP-000002",
            Designation = "Developer",
            Department = "Engineering",
            EmploymentStatus = "ACTIVE",
            ManagerId = manager.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Employees.AddRange(ravi, priya);
        _context.SaveChanges();

        var project = new Project
        {
            ProjectCode = "PRJ-000001",
            ProjectName = "Alpha Portal",
            StartDate = _lastWeek.AddMonths(-1),
            EndDate = _lastWeek.AddMonths(6),
            ProjectStatus = "ACTIVE",
            HealthStatus = "GREEN",
            ManagerUserId = manager.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Projects.Add(project);
        _context.SaveChanges();

        _context.ProjectAllocations.AddRange(
            new ProjectAllocation
            {
                EmployeeId = ravi.Id,
                ProjectId = project.Id,
                AllocationPercentage = 50m,
                AllocationStartDate = _lastWeek.AddMonths(-1),
                AllocationEndDate = weekEnd.AddMonths(3),
                AllocationStatus = "ACTIVE",
                AllocatedByManagerId = manager.Id,
                CreatedAt = now,
                UpdatedAt = now
            },
            new ProjectAllocation
            {
                EmployeeId = priya.Id,
                ProjectId = project.Id,
                AllocationPercentage = 50m,
                AllocationStartDate = _lastWeek.AddMonths(-1),
                AllocationEndDate = weekEnd.AddMonths(3),
                AllocationStatus = "ACTIVE",
                AllocatedByManagerId = manager.Id,
                CreatedAt = now,
                UpdatedAt = now
            });
        _context.SaveChanges();

        _context.Timesheets.Add(new Timesheet
        {
            EmployeeId = priya.Id,
            WeekStartDate = _lastWeek,
            Status = TimesheetConstants.StatusSubmitted,
            TotalHours = 20m,
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });
        _context.SaveChanges();

        return (ravi.Id, priya.Id);
    }

    [Fact]
    public async Task MarkMissedTimesheetsAsync_AllocationNoSubmission_InsertsMissed()
    {
        var created = await _timesheetService.MarkMissedTimesheetsAsync();

        Assert.Equal(1, created);

        var missed = _context.Timesheets.Single(t =>
            t.EmployeeId == _employeeWithAllocationId && t.WeekStartDate == _lastWeek);
        Assert.Equal(TimesheetConstants.StatusMissed, missed.Status);
        Assert.Equal(0m, missed.TotalHours);
    }

    [Fact]
    public async Task MarkMissedTimesheetsAsync_Rerun_IsIdempotent()
    {
        await _timesheetService.MarkMissedTimesheetsAsync();
        var secondRun = await _timesheetService.MarkMissedTimesheetsAsync();

        Assert.Equal(0, secondRun);
        Assert.Single(_context.Timesheets.Where(t =>
            t.EmployeeId == _employeeWithAllocationId && t.WeekStartDate == _lastWeek));
    }

    [Fact]
    public async Task MarkMissedTimesheetsAsync_ExistingSubmitted_Skips()
    {
        await _timesheetService.MarkMissedTimesheetsAsync();

        var submitted = _context.Timesheets.Single(t =>
            t.EmployeeId == _employeeWithSubmissionId && t.WeekStartDate == _lastWeek);
        Assert.Equal(TimesheetConstants.StatusSubmitted, submitted.Status);
    }

    [Fact]
    public async Task MarkMissedTimesheetsAsync_BatchExistsCheck_SkipsExisting()
    {
        var first = await _timesheetService.MarkMissedTimesheetsAsync();
        var second = await _timesheetService.MarkMissedTimesheetsAsync();

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(2, _context.Timesheets.Count(t => t.WeekStartDate == _lastWeek));
    }

    public void Dispose() => _context.Dispose();
}
