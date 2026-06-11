using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Server.Common;
using Server.Common.Allocations;
using Server.Common.Roles;
using Tests.Helpers;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Allocations;
using Server.Models.Entities;

namespace Tests;

public class AllocationServiceTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly AllocationService _allocationService;
    private readonly long _managerUserId;
    private readonly long _employeeId;
    private readonly long _projectId;

    public AllocationServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        (_managerUserId, _employeeId, _projectId) = SeedData();

        _allocationService = new AllocationService(
            _context,
            new AllocationRepository(_context),
            new EmployeeRepository(_context),
            new UserRepository(_context),
            new ProjectRepository(_context),
            TestServiceFactory.CreateResourceStatusService(_context),
            TestServiceFactory.CreateAuditService(_context),
            TestServiceFactory.CreateLogger<AllocationService>());
    }

    private (long ManagerUserId, long EmployeeId, long ProjectId) SeedData()
    {
        TestDataHelper.SeedRolesAsync(_context).GetAwaiter().GetResult();
        var now = DateTime.UtcNow;
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

        var user = new User
        {
            Username = "ravi.kumar",
            Email = "ravi@techserve.com",
            FullName = "Ravi Kumar",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        _context.UserRoles.AddRange(
            new UserRole { UserId = manager.Id, RoleId = managerRole.Id, AssignedAt = now },
            new UserRole { UserId = user.Id, RoleId = employeeRole.Id, AssignedAt = now });

        var managerProfile = new ResourceProfile
        {
            UserId = manager.Id,
            ResourceStatus = ResourceStatusConstants.Bench,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.ResourceProfiles.Add(managerProfile);
        _context.SaveChanges();

        var employee = new ResourceProfile
        {
            UserId = user.Id,
            ManagerId = manager.Id,
            ResourceStatus = ResourceStatusConstants.Bench,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.ResourceProfiles.Add(employee);
        _context.SaveChanges();

        var project = new Project
        {
            ProjectCode = "PRJ-000201",
            ProjectName = "Alpha Portal",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
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
            ResourceProfileId = employee.Id,
            ProjectId = project.Id,
            AllocationPercentage = 50,
            AllocationStartDate = new DateOnly(2026, 3, 1),
            AllocationEndDate = new DateOnly(2026, 6, 30),
            AllocationStatus = AllocationStatusConstants.Active,
            AllocatedByUserId = manager.Id,
            CreatedAt = now,
            UpdatedAt = now
        });
        _context.SaveChanges();

        return (manager.Id, employee.Id, project.Id);
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

    [Fact]
    public async Task CreateAllocationAsync_Success_SetsEmployeeAllocated()
    {
        var now = DateTime.UtcNow;
        var employeeRole = _context.Roles.First(r => r.RoleName == RoleConstants.Employee);
        var managerUserId = _managerUserId;

        var benchUser = new User
        {
            Username = "priya.sharma",
            Email = "priya@techserve.com",
            FullName = "Priya Sharma",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.Add(benchUser);
        await _context.SaveChangesAsync();

        _context.UserRoles.Add(new UserRole
        {
            UserId = benchUser.Id,
            RoleId = employeeRole.Id,
            AssignedAt = now
        });

        var benchEmployee = new ResourceProfile
        {
            UserId = benchUser.Id,
            ManagerId = managerUserId,
            ResourceStatus = ResourceStatusConstants.Bench,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.ResourceProfiles.Add(benchEmployee);
        await _context.SaveChangesAsync();

        var request = new CreateAllocationRequestDto
        {
            EmployeeId = benchEmployee.Id,
            ProjectId = _projectId,
            AllocationPercentage = 30,
            AllocationStartDate = new DateOnly(2026, 7, 1),
            AllocationEndDate = new DateOnly(2026, 9, 30)
        };

        var result = await _allocationService.CreateAllocationAsync(_managerUserId, request);

        Assert.True(result.AllocationId > 0);
        Assert.Equal(AllocationConstants.EmploymentStatusPartiallyAllocated, result.EmploymentStatus);

        var profile = await _context.ResourceProfiles.FindAsync(benchEmployee.Id);
        Assert.Equal(ResourceStatusConstants.PartiallyAllocated, profile!.ResourceStatus);
    }

    [Fact]
    public async Task CreateAllocationAsync_OverAllocation_Throws()
    {
        var request = new CreateAllocationRequestDto
        {
            EmployeeId = _employeeId,
            ProjectId = _projectId,
            AllocationPercentage = 60,
            AllocationStartDate = new DateOnly(2026, 3, 1),
            AllocationEndDate = new DateOnly(2026, 6, 30)
        };

        var ex = await Assert.ThrowsAsync<ValidationAppException>(
            () => _allocationService.CreateAllocationAsync(_managerUserId, request));

        Assert.Contains("110%", ex.Message);
        Assert.Contains("Maximum is 100%", ex.Message);
    }

    [Fact]
    public async Task CreateAllocationAsync_NotManagerTeam_Throws()
    {
        var now = DateTime.UtcNow;
        var managerRole = _context.Roles.First(r => r.RoleName == RoleConstants.Manager);
        var otherManager = new User
        {
            Username = "other.mgr",
            Email = "other@techserve.com",
            FullName = "Other Manager",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.Add(otherManager);
        await _context.SaveChangesAsync();

        _context.UserRoles.Add(new UserRole
        {
            UserId = otherManager.Id,
            RoleId = managerRole.Id,
            AssignedAt = now
        });
        _context.ResourceProfiles.Add(new ResourceProfile
        {
            UserId = otherManager.Id,
            ResourceStatus = ResourceStatusConstants.Bench,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _context.SaveChangesAsync();

        var request = new CreateAllocationRequestDto
        {
            EmployeeId = _employeeId,
            ProjectId = _projectId,
            AllocationPercentage = 20,
            AllocationStartDate = new DateOnly(2026, 7, 1),
            AllocationEndDate = new DateOnly(2026, 8, 31)
        };

        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _allocationService.CreateAllocationAsync(otherManager.Id, request));
    }

    [Fact]
    public async Task EndAllocationAsync_SetsBenchWhenNoOtherActive()
    {
        var allocation = await _context.ProjectAllocations.FirstAsync();
        var result = await _allocationService.EndAllocationAsync(_managerUserId, allocation.Id);

        var endedAllocation = await _context.ProjectAllocations.FindAsync(allocation.Id);
        Assert.Equal(AllocationStatusConstants.Ended, endedAllocation!.AllocationStatus);
        Assert.Equal(AllocationConstants.EmploymentStatusBench, result.EmploymentStatus);

        var profile = await _context.ResourceProfiles.FindAsync(_employeeId);
        Assert.Equal(ResourceStatusConstants.Bench, profile!.ResourceStatus);
    }

    [Fact]
    public async Task EndAllocationAsync_NotProjectOwner_Throws()
    {
        var now = DateTime.UtcNow;
        var managerRole = _context.Roles.First(r => r.RoleName == RoleConstants.Manager);
        var otherManager = new User
        {
            Username = "other.mgr2",
            Email = "other2@techserve.com",
            FullName = "Other Manager 2",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.Add(otherManager);
        await _context.SaveChangesAsync();

        _context.UserRoles.Add(new UserRole
        {
            UserId = otherManager.Id,
            RoleId = managerRole.Id,
            AssignedAt = now
        });
        await _context.SaveChangesAsync();

        var allocation = await _context.ProjectAllocations.FirstAsync();

        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _allocationService.EndAllocationAsync(otherManager.Id, allocation.Id));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
