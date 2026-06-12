using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Common.Roles;
using Server.Exceptions;
using Server.Models.DTOs.Auth;
using Server.Models.Entities;
using Server.Services.Auth;
using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IRoleRepository> _roleRepoMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    private readonly User _user;
    private const string CurrentPassword = "Welcome1";

    public AuthServiceTests()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _roleRepoMock = new Mock<IRoleRepository>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _userRepoMock.Object,
            _roleRepoMock.Object,
            _jwtTokenServiceMock.Object,
            _loggerMock.Object);

        _user = new User
        {
            Id = 1,
            Username = "test.user",
            Email = "test.user@example.com",
            FullName = "Test User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(CurrentPassword),
            IsTemporaryPassword = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFreshTokenWithoutForceFlag()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_user);
        _roleRepoMock.Setup(r => r.GetRoleNameForUserAsync(_user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RoleConstants.Employee);
        _jwtTokenServiceMock.Setup(s => s.CreateToken(_user, RoleConstants.Employee, null))
            .Returns(new LoginResponseDto { UserId = _user.Id, Token = "fresh_token", ForcePasswordChange = false });

        // Act
        var result = await _authService.ChangePasswordAsync(_user.Id, new ChangePasswordRequestDto
        {
            CurrentPassword = CurrentPassword,
            NewPassword = "NewPass1"
        });

        // Assert
        Assert.False(result.ForcePasswordChange);
        Assert.Equal("fresh_token", result.Token);
        Assert.Equal(_user.Id, result.UserId);

        _userRepoMock.Verify(r => r.UpdateAsync(It.Is<User>(u => !u.IsTemporaryPassword), It.IsAny<CancellationToken>()), Times.Once);
        _userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_Throws()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_user);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationAppException>(
            () => _authService.ChangePasswordAsync(_user.Id, new ChangePasswordRequestDto
            {
                CurrentPassword = "WrongPass1",
                NewPassword = "NewPass1"
            }));
    }
}
