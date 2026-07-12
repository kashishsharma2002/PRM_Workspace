using Server.Models.DTOs.Roles;

namespace Server.Services.Permissions;

public interface IPermissionService
{
    Task<RoleListResponseDto> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<RolePermissionsResponseDto> GetRolePermissionsAsync(
        string roleName,
        CancellationToken cancellationToken = default);
}
