using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Server.Common;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Timesheets;
using Server.Models.Entities;
using Server.Repositories;
using Server.Services;
using Tests.Helpers;

namespace Tests;

public class ManagerTimesheetServiceTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly TimesheetService _timesheetService;
    private readonly long _ankitUserId;
    private readonly long _nehaUserId;
    private readonly long _raviEmployeeId;
    private readonly long _anilEmployeeId;
    private readonly long _projectId;
    private readonly DateOnly _weekStart = new(2026, 5, 12);

    public ManagerTimesheetServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        (_ankitUserId, _nehaUserId, _raviEmployeeId, _anilEmployeeId, _projectId) = SeedData();

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

    private (long ankitId, long nehaId, long raviEmpId, long anilEmpId, long projectId) SeedData()
    {
        var now = DateTime.UtcNow;

        var ankit = new User
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
        var neha = new User
        {
            Username = "neha.joshi",
            Email = "neha@techserve.com",
            FullName = "Neha Joshi",
            PasswordHash = "hash",
            Role = "MANAGER",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.AddRange(ankit, neha);

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
        var anilUser = new User
        {
            Username = "anil.mehta",
            Email = "anil@techserve.com",
            FullName = "Anil Mehta",
            PasswordHash = "hash",
            Role = "EMPLOYEE",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.AddRange(raviUser, anilUser);
        _context.SaveChanges();

        var ravi = new Employee
        {
            UserId = raviUser.Id,
            ManagerId = ankit.Id,
            EmployeeCode = "EMP-000001",
            EmploymentStatus = "ALLOCATED",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var anil = new Employee
        {
            UserId = anilUser.Id,
            ManagerId = neha.Id,
            EmployeeCode = "EMP-000002",
            EmploymentStatus = "ALLOCATED",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Employees.AddRange(ravi, anil);

        var project = new Project
        {
            ProjectCode = "PRJ-000201",
            ProjectName = "Alpha Portal",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            ProjectStatus = "ACTIVE",
            ManagerUserId = ankit.Id,
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
                AllocationPercentage = 50,
                AllocationStartDate = new DateOnly(2026, 3, 1),
                AllocationEndDate = new DateOnly(2026, 6, 30),
                AllocationStatus = "ACTIVE",
                AllocatedByManagerId = ankit.Id,
                CreatedAt = now,
                UpdatedAt = now
            },
            new ProjectAllocation
            {
                EmployeeId = anil.Id,
                ProjectId = project.Id,
                AllocationPercentage = 50,
                AllocationStartDate = new DateOnly(2026, 3, 1),
                AllocationEndDate = new DateOnly(2026, 6, 30),
                AllocationStatus = "ACTIVE",
                AllocatedByManagerId = neha.Id,
                CreatedAt = now,
                UpdatedAt = now
            });

        _context.ActivityTags.Add(new ActivityTag
        {
            TagCode = "BACKEND_API",
            TagName = "Backend API Development",
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
        return (ankit.Id, neha.Id, ravi.Id, anil.Id, project.Id);
    }

    [Fact]
    public async Task GetTeamTimesheetsAsync_SubmittedTimesheet_ShowsLineItems()
    {
        var request = new TimesheetTestDataBuilder()
            .WithWeekStartDate(_weekStart)
            .WithProjectId(_projectId)
            .WithHoursPerProject(18)
            .WithActivityTagIds(1)
            .BuildSubmitRequest();

        await _timesheetService.SubmitTimesheetAsync(_raviEmployeeId, _ankitUserId, request);

        var result = await _timesheetService.GetTeamTimesheetsAsync(_ankitUserId, _weekStart);

        Assert.Single(result.Rows);
        Assert.Equal("Ravi Kumar", result.Rows[0].EmployeeName);
        Assert.Equal("Alpha Portal", result.Rows[0].ProjectName);
        Assert.Equal(18, result.Rows[0].HoursLogged);
        Assert.Equal("SUBMITTED", result.Rows[0].Status);
    }

    [Fact]
    public async Task GetTeamTimesheetsAsync_ReturnsOnlyTeamEmployees()
    {
        var request = new TimesheetTestDataBuilder()
            .WithWeekStartDate(_weekStart)
            .WithProjectId(_projectId)
            .WithHoursPerProject(10)
            .BuildSubmitRequest();

        await _timesheetService.SubmitTimesheetAsync(_raviEmployeeId, _ankitUserId, request);

        var anilRequest = new TimesheetSubmitRequestDto
        {
            WeekStartDate = _weekStart,
            LineItems =
            [
                new TimesheetLineItemRequestDto
                {
                    ProjectId = _projectId,
                    HoursLogged = 12,
                    ActivityTagIds = [1]
                }
            ]
        };
        await _timesheetService.SubmitTimesheetAsync(_anilEmployeeId, _nehaUserId, anilRequest);

        var ankitResult = await _timesheetService.GetTeamTimesheetsAsync(_ankitUserId, _weekStart);
        var nehaResult = await _timesheetService.GetTeamTimesheetsAsync(_nehaUserId, _weekStart);

        Assert.Single(ankitResult.Rows);
        Assert.Equal("Ravi Kumar", ankitResult.Rows[0].EmployeeName);
        Assert.Single(nehaResult.Rows);
        Assert.Equal("Anil Mehta", nehaResult.Rows[0].EmployeeName);
    }

    [Fact]
    public async Task GetTimesheetForManagerAsync_OtherTeam_ThrowsNotFound()
    {
        var request = new TimesheetTestDataBuilder()
            .WithWeekStartDate(_weekStart)
            .WithProjectId(_projectId)
            .WithHoursPerProject(10)
            .BuildSubmitRequest();

        var submitted = await _timesheetService.SubmitTimesheetAsync(_raviEmployeeId, _ankitUserId, request);

        await Assert.ThrowsAsync<NotFoundAppException>(
            () => _timesheetService.GetTimesheetForManagerAsync(_nehaUserId, submitted.TimesheetId));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
