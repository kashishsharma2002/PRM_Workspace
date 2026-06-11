using Microsoft.EntityFrameworkCore;
using Tests.Helpers;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Server.Common;
using Server.Common.Roles;
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
        TestDataHelper.SeedRolesAsync(_context).GetAwaiter().GetResult();
        var now = DateTime.UtcNow;
        var weekEnd = WeekDateHelper.GetWeekEnd(_lastWeek);
        var managerRole = _context.Roles.First(r => r.RoleName == RoleConstants.Manager);
        var employeeRole = _context.Roles.First(r => r.RoleName == RoleConstants.Employee);

        var manager = new User
        {
            Username = "ankit.shah",
            Email = "ankit@techserve.com",
            FullName = "Ankit Shah",
            PasswordHash = "hash",
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
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.AddRange(raviUser, priyaUser);
        _context.SaveChanges();

        _context.UserRoles.AddRange(
            new UserRole { UserId = manager.Id, RoleId = managerRole.Id, AssignedAt = now },
            new UserRole { UserId = raviUser.Id, RoleId = employeeRole.Id, AssignedAt = now },
            new UserRole { UserId = priyaUser.Id, RoleId = employeeRole.Id, AssignedAt = now });

        var managerProfile = new ResourceProfile
        {
            UserId = manager.Id,
            ResourceStatus = ResourceStatusConstants.Bench,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.ResourceProfiles.Add(managerProfile);
        _context.SaveChanges();

        var ravi = new ResourceProfile
        {
            UserId = raviUser.Id,
            ManagerId = manager.Id,
            ResourceStatus = ResourceStatusConstants.Allocated,
            CreatedAt = now,
            UpdatedAt = now
        };
        var priya = new ResourceProfile
        {
            UserId = priyaUser.Id,
            ManagerId = manager.Id,
            ResourceStatus = ResourceStatusConstants.Allocated,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.ResourceProfiles.AddRange(ravi, priya);
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
                ResourceProfileId = ravi.Id,
                ProjectId = project.Id,
                AllocationPercentage = 50m,
                AllocationStartDate = _lastWeek.AddMonths(-1),
                AllocationEndDate = weekEnd.AddMonths(3),
                AllocationStatus = "ACTIVE",
                AllocatedByUserId = manager.Id,
                CreatedAt = now,
                UpdatedAt = now
            },
            new ProjectAllocation
            {
                ResourceProfileId = priya.Id,
                ProjectId = project.Id,
                AllocationPercentage = 50m,
                AllocationStartDate = _lastWeek.AddMonths(-1),
                AllocationEndDate = weekEnd.AddMonths(3),
                AllocationStatus = "ACTIVE",
                AllocatedByUserId = manager.Id,
                CreatedAt = now,
                UpdatedAt = now
            });
        _context.SaveChanges();

        _context.Timesheets.Add(new Timesheet
        {
            ResourceProfileId = priya.Id,
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
            t.ResourceProfileId == _employeeWithAllocationId && t.WeekStartDate == _lastWeek);
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
            t.ResourceProfileId == _employeeWithAllocationId && t.WeekStartDate == _lastWeek));
    }

    [Fact]
    public async Task MarkMissedTimesheetsAsync_ExistingSubmitted_Skips()
    {
        await _timesheetService.MarkMissedTimesheetsAsync();

        var submitted = _context.Timesheets.Single(t =>
            t.ResourceProfileId == _employeeWithSubmissionId && t.WeekStartDate == _lastWeek);
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
