using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Common.Audit;
using Server.Common.Roles;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Users;
using Server.Models.Entities;
using Server.Repositories.Roles;
using Server.Repositories.Users;
using Server.Repositories.Employees;
using Server.Services.Shared;
using Server.Services.Users;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class UserServiceCreateTests
{
    private readonly Mock<IDbTransactionManager> _transactionManagerMock;
    private readonly Mock<IDbTransaction> _transactionMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IRoleRepository> _roleRepoMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _userService;

    public UserServiceCreateTests()
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
    public async Task CreateUserAccountAsync_CreatesUserAndEmployeeAtomically()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            FullName = "Priya Sharma",
            Email = "priya.sharma@example.com",
            Username = "priya.sharma",
            TemporaryPassword = "Welcome1",
            Role = "EMPLOYEE",
            Department = DepartmentConstants.SoftwareDevelopment,
            Designation = DesignationConstants.Jse
        };

        _userRepoMock.Setup(u => u.ExistsByUsernameOrEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _roleRepoMock.Setup(r => r.GetByNameAsync("EMPLOYEE", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = 3, RoleName = RoleConstants.Employee });

        _userRepoMock.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, c) => u.Id = 10);

        _employeeRepoMock.Setup(e => e.AddAsync(It.IsAny<ResourceProfile>(), It.IsAny<CancellationToken>()))
            .Callback<ResourceProfile, CancellationToken>((e, c) => e.Id = 20);

        // Act
        var result = await _userService.CreateUserAccountAsync(1, request);

        // Assert
        Assert.Equal(10, result.UserId);
        Assert.Equal(20, result.EmployeeId);
        Assert.Equal("EMP-000010", result.EmployeeCode);

        _userRepoMock.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _employeeRepoMock.Verify(e => e.AddAsync(It.IsAny<ResourceProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _roleRepoMock.Verify(r => r.AssignRoleAsync(10, 3, 1, It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditServiceMock.Verify(a => a.LogCreateAsync(
            1,
            AuditEntityConstants.Users,
            10,
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateUserAccountAsync_DuplicateUsername_ThrowsConflict()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            FullName = "First User",
            Email = "duplicate.user@example.com",
            Username = "duplicate.user",
            TemporaryPassword = "Welcome1",
            Role = "MANAGER",
            Department = DepartmentConstants.Management,
            Designation = DesignationConstants.DeliveryManager
        };

        _userRepoMock.Setup(u => u.ExistsByUsernameOrEmailAsync("duplicate.user", "duplicate.user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictAppException>(() =>
            _userService.CreateUserAccountAsync(1, request));

        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
