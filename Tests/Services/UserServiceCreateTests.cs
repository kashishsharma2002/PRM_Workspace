using Microsoft.EntityFrameworkCore;
using Tests.Helpers;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Server.Common;
using Server.Common.Roles;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Users;

namespace Tests;

public class UserServiceCreateTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly UserService _userService;

    public UserServiceCreateTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        TestDataHelper.SeedRolesAsync(_context).GetAwaiter().GetResult();

        _userService = new UserService(
            _context,
            new UserRepository(_context),
            new EmployeeRepository(_context),
            TestServiceFactory.CreateRoleRepository(_context),
            TestServiceFactory.CreateAuditService(_context),
            TestServiceFactory.CreateLogger<UserService>());
    }

    [Fact]
    public async Task CreateUserAccountAsync_CreatesUserAndEmployeeAtomically()
    {
        var result = await _userService.CreateUserAccountAsync(1, new CreateUserRequestDto
        {
            FullName = "Priya Sharma",
            Email = "priya.sharma@techserve.com",
            Username = "priya.sharma",
            TemporaryPassword = "Welcome1",
            Role = "EMPLOYEE",
            Department = DepartmentConstants.SoftwareDevelopment,
            Designation = DesignationConstants.Jse
        });

        Assert.True(result.UserId > 0);
        Assert.True(result.EmployeeId > 0);
        Assert.Equal($"EMP-{result.UserId:D6}", result.EmployeeCode);

        var user = await _context.Users.FindAsync(result.UserId);
        var profile = await _context.ResourceProfiles.FindAsync(result.EmployeeId);
        var auditCount = await _context.AuditLogs.CountAsync();
        var role = await TestServiceFactory.CreateRoleRepository(_context)
            .GetRoleNameForUserAsync(result.UserId);

        Assert.NotNull(user);
        Assert.NotNull(profile);
        Assert.Equal(RoleConstants.Employee, role);
        Assert.True(user!.IsTemporaryPassword);
        Assert.Equal(user.Id, profile!.UserId);
        Assert.Equal("BENCH", profile.ResourceStatus);
        Assert.Equal(1, auditCount);
    }

    [Fact]
    public async Task CreateUserAccountAsync_DuplicateUsername_ThrowsConflict()
    {
        var request = new CreateUserRequestDto
        {
            FullName = "First User",
            Email = "first@techserve.com",
            Username = "duplicate.user",
            TemporaryPassword = "Welcome1",
            Role = "MANAGER",
            Department = DepartmentConstants.Management,
            Designation = DesignationConstants.DeliveryManager
        };

        await _userService.CreateUserAccountAsync(1, request);

        var duplicate = new CreateUserRequestDto
        {
            FullName = "Second User",
            Email = "second@techserve.com",
            Username = "duplicate.user",
            TemporaryPassword = "Welcome2",
            Role = "EMPLOYEE",
            Department = DepartmentConstants.Qa,
            Designation = DesignationConstants.SoftwareEngineer
        };

        await Assert.ThrowsAsync<ConflictAppException>(() =>
            _userService.CreateUserAccountAsync(1, duplicate));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
