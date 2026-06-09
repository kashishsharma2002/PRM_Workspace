using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Users;
using Server.Repositories;
using Server.Services;

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
        _userService = new UserService(
            _context,
            new UserRepository(_context),
            new EmployeeRepository(_context),
            new AuditLogRepository(_context));
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
            Role = "EMPLOYEE"
        });

        Assert.True(result.UserId > 0);
        Assert.True(result.EmployeeId > 0);
        Assert.Equal($"EMP-{result.UserId:D6}", result.EmployeeCode);

        var user = await _context.Users.FindAsync(result.UserId);
        var employee = await _context.Employees.FindAsync(result.EmployeeId);
        var auditCount = await _context.AuditLogs.CountAsync();

        Assert.NotNull(user);
        Assert.NotNull(employee);
        Assert.Equal("EMPLOYEE", user!.Role);
        Assert.True(user.ForcePasswordChange);
        Assert.Equal(user.Id, employee!.UserId);
        Assert.Equal("BENCH", employee.EmploymentStatus);
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
            Role = "MANAGER"
        };

        await _userService.CreateUserAccountAsync(1, request);

        var duplicate = new CreateUserRequestDto
        {
            FullName = "Second User",
            Email = "second@techserve.com",
            Username = "duplicate.user",
            TemporaryPassword = "Welcome2",
            Role = "EMPLOYEE"
        };

        await Assert.ThrowsAsync<ConflictAppException>(() =>
            _userService.CreateUserAccountAsync(1, duplicate));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
