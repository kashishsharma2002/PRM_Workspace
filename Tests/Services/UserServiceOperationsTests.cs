using Microsoft.EntityFrameworkCore;
using Tests.Helpers;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Users;

namespace Tests;

public class UserServiceOperationsTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly UserService _userService;

    public UserServiceOperationsTests()
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

    private async Task<long> CreateTestUserAsync(string username = "test.user")
    {
        var result = await _userService.CreateUserAccountAsync(1, new CreateUserRequestDto
        {
            FullName = "Test User",
            Email = $"{username}@techserve.com",
            Username = username,
            TemporaryPassword = "Welcome1",
            Role = "EMPLOYEE"
        });
        return result.UserId;
    }

    [Fact]
    public async Task DeactivateUserAsync_SetsUserAndEmployeeInactive()
    {
        var userId = await CreateTestUserAsync();

        await _userService.DeactivateUserAsync(999, userId);

        var user = await _context.Users.FindAsync(userId);
        var profile = await _context.ResourceProfiles.FirstAsync(e => e.UserId == userId);

        Assert.NotNull(user);
        Assert.False(user!.IsActive);
        Assert.Equal("BENCH", profile.ResourceStatus);
    }

    [Fact]
    public async Task DeactivateUserAsync_SelfDeactivate_ThrowsForbidden()
    {
        var userId = await CreateTestUserAsync();

        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            _userService.DeactivateUserAsync(userId, userId));
    }

    [Fact]
    public async Task ReactivateUserAsync_RestoresIsActive()
    {
        var userId = await CreateTestUserAsync();
        await _userService.DeactivateUserAsync(999, userId);

        await _userService.ReactivateUserAsync(999, userId);

        var user = await _context.Users.FindAsync(userId);
        var profile = await _context.ResourceProfiles.FirstAsync(e => e.UserId == userId);

        Assert.NotNull(user);
        Assert.True(user!.IsActive);
        Assert.Equal("BENCH", profile.ResourceStatus);
    }

    [Fact]
    public async Task ResetPasswordAsync_SetsForcePasswordChange()
    {
        var userId = await CreateTestUserAsync();

        await _userService.ResetPasswordAsync(1, userId, new ResetPasswordRequestDto
        {
            NewTemporaryPassword = "NewPass99"
        });

        var user = await _context.Users.FindAsync(userId);
        Assert.NotNull(user);
        Assert.True(user!.IsTemporaryPassword);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass99", user.PasswordHash));
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsCorrectCounts()
    {
        await CreateTestUserAsync("active.user");
        var inactiveId = await CreateTestUserAsync("inactive.user");
        await _userService.DeactivateUserAsync(999, inactiveId);

        var result = await _userService.GetAllUsersAsync();

        Assert.Equal(2, result.Total);
        Assert.Equal(1, result.ActiveCount);
        Assert.Equal(1, result.InactiveCount);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
