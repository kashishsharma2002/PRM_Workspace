using Moq;
using Server.Common.Roles;
using Server.Exceptions;
using Server.Models.Entities;
using Server.Repositories.Permissions;
using Server.Services.Permissions;
using Xunit;

namespace Tests.Services;

public class PermissionServiceTests
{
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly PermissionService _permissionService;

    public PermissionServiceTests()
    {
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _permissionService = new PermissionService(_permissionRepositoryMock.Object);
    }

    [Fact]
    public async Task GetRolePermissionsAsync_ReturnsOnlySeedDefinedCapabilities()
    {
        var managerRole = new Role { Id = 2, RoleName = RoleConstants.Manager };

        _permissionRepositoryMock.Setup(r => r.GetRoleByNameAsync(RoleConstants.Manager, It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerRole);

        var result = await _permissionService.GetRolePermissionsAsync(RoleConstants.Manager);

        Assert.Equal(RoleConstants.Manager, result.RoleName);
        Assert.Equal(10, result.Permissions.Count);
        Assert.DoesNotContain(result.Permissions, p => p.Code == "allocations:delete");
        Assert.DoesNotContain(result.Permissions, p => p.Code == "roles:manage_permissions");
    }

    [Fact]
    public async Task GetRolePermissionsAsync_InvalidRole_ThrowsValidation()
    {
        await Assert.ThrowsAsync<ValidationAppException>(() =>
            _permissionService.GetRolePermissionsAsync("INVALID"));
    }
}
