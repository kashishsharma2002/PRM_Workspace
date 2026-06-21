using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Common.Audit;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Users;
using Server.Models.Entities;
using Server.Repositories.Roles;
using Server.Repositories.Users;
using Server.Repositories.Employees;
using Server.Services.Shared;
using Server.Services.Users;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class UserServiceOperationsTests
{
    private readonly Mock<IDbTransactionManager> _transactionManagerMock;
    private readonly Mock<IDbTransaction> _transactionMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IRoleRepository> _roleRepoMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _userService;

    public UserServiceOperationsTests()
    {
        _transactionMock = new Mock<IDbTransaction>();
        _transactionManagerMock = new Mock<IDbTransactionManager>();

        _transactionManagerMock.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _userRepoMock = new Mock<IUserRepository>();
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _roleRepoMock = new Mock<IRoleRepository>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<UserService>>();

        _userService = new UserService(
            _transactionManagerMock.Object,
            _userRepoMock.Object,
            _employeeRepoMock.Object,
            _roleRepoMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task DeactivateUserAsync_SetsUserAndEmployeeInactive()
    {
        // Arrange
        var user = new User { Id = 10, Username = "test.user", IsActive = true };
        var profile = new ResourceProfile { Id = 20, UserId = 10, ResourceStatus = "ALLOCATED" };

        _userRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _employeeRepoMock.Setup(r => r.GetByUserIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        await _userService.DeactivateUserAsync(999, 10);

        // Assert
        Assert.False(user.IsActive);
        Assert.Equal("BENCH", profile.ResourceStatus);
        _userRepoMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _employeeRepoMock.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
        _auditServiceMock.Verify(a => a.LogDeactivateAsync(
            999,
            AuditEntityConstants.Users,
            10,
            It.IsAny<object>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateUserAsync_SelfDeactivate_ThrowsForbidden()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            _userService.DeactivateUserAsync(10, 10));
    }

    [Fact]
    public async Task ReactivateUserAsync_RestoresIsActive()
    {
        // Arrange
        var user = new User { Id = 10, Username = "test.user", IsActive = false };
        var profile = new ResourceProfile { Id = 20, UserId = 10, ResourceStatus = "BENCH" };

        _userRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _employeeRepoMock.Setup(r => r.GetByUserIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        await _userService.ReactivateUserAsync(999, 10);

        // Assert
        Assert.True(user.IsActive);
        _userRepoMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _employeeRepoMock.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
        _auditServiceMock.Verify(a => a.LogUpdateAsync(
            999,
            AuditEntityConstants.Users,
            10,
            It.IsAny<object>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_SetsForcePasswordChange()
    {
        // Arrange
        var user = new User { Id = 10, Username = "test.user", PasswordHash = "old" };
        _userRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _userService.ResetPasswordAsync(1, 10, new ResetPasswordRequestDto
        {
            NewTemporaryPassword = "NewPass99"
        });

        // Assert
        Assert.True(user.IsTemporaryPassword);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass99", user.PasswordHash));
        _auditServiceMock.Verify(a => a.LogUpdateAsync(
            1,
            AuditEntityConstants.Users,
            10,
            null,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = 10, Username = "user.a", IsActive = true },
            new() { Id = 20, Username = "user.b", IsActive = false }
        };
        _userRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        _roleRepoMock.Setup(r => r.GetRoleNamesForUsersAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, string>
            {
                [10] = "EMPLOYEE",
                [20] = "MANAGER"
            });

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        Assert.Equal(2, result.Total);
        Assert.Equal(1, result.ActiveCount);
        Assert.Equal(1, result.InactiveCount);
    }
}
