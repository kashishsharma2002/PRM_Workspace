using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Server.Common;
using Server.Common.Roles;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Auth;
using Server.Models.Entities;
using Tests.Helpers;

namespace Tests;

public class AuthServiceTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly AuthService _authService;
    private readonly User _user;
    private const string CurrentPassword = "Welcome1";

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        var userRepo = new UserRepository(_context);
        var roleRepo = TestServiceFactory.CreateRoleRepository(_context);

        var jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "TestSecretKeyForJwtTokenService1234567890",
            Issuer = "PRM.Test",
            Audience = "PRM.Test",
            ExpiryHours = 8
        });

        _authService = new AuthService(
            userRepo,
            roleRepo,
            new JwtTokenService(jwtSettings),
            TestServiceFactory.CreateLogger<AuthService>());

        var now = DateTime.UtcNow;
        TestDataHelper.SeedRolesAsync(_context).GetAwaiter().GetResult();
        var roleEntity = _context.Roles.First(r => r.RoleName == RoleConstants.Employee);

        _user = new User
        {
            Username = "test.user",
            Email = "test@techserve.com",
            FullName = "Test User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(CurrentPassword),
            IsTemporaryPassword = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.Add(_user);
        _context.SaveChanges();

        _context.UserRoles.Add(new UserRole
        {
            UserId = _user.Id,
            RoleId = roleEntity.Id,
            AssignedAt = now
        });
        _context.SaveChanges();
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFreshTokenWithoutForceFlag()
    {
        var result = await _authService.ChangePasswordAsync(_user.Id, new ChangePasswordRequestDto
        {
            CurrentPassword = CurrentPassword,
            NewPassword = "NewPass1"
        });

        Assert.False(result.ForcePasswordChange);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal(_user.Id, result.UserId);

        var updatedUser = await _context.Users.FindAsync(_user.Id);
        Assert.False(updatedUser!.IsTemporaryPassword);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_Throws()
    {
        await Assert.ThrowsAsync<ValidationAppException>(
            () => _authService.ChangePasswordAsync(_user.Id, new ChangePasswordRequestDto
            {
                CurrentPassword = "WrongPass1",
                NewPassword = "NewPass1"
            }));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
