using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Server.Common;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Auth;
using Server.Models.Entities;

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

        var jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "TestSecretKeyForJwtTokenService1234567890",
            Issuer = "PRM.Test",
            Audience = "PRM.Test",
            ExpiryHours = 8
        });

        _authService = new AuthService(userRepo, new JwtTokenService(jwtSettings));

        var now = DateTime.UtcNow;
        _user = new User
        {
            Username = "test.user",
            Email = "test@techserve.com",
            FullName = "Test User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(CurrentPassword),
            Role = "EMPLOYEE",
            ForcePasswordChange = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.Add(_user);
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
        Assert.False(updatedUser!.ForcePasswordChange);
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
