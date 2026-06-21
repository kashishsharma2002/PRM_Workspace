using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Common;
using Server.Common.Roles;
using Server.Models.DTOs.Roles;
using Server.Services.Permissions;

namespace Server.Controllers.Roles;

[Authorize(Roles = RoleConstants.Admin)]
[ApiController]
[Route("api/roles")]
public class RolePermissionController(IPermissionService permissionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<RoleListResponseDto>>> GetRoles(
        CancellationToken cancellationToken)
    {
        var result = await permissionService.GetRolesAsync(cancellationToken);
        return Ok(ApiResponse<RoleListResponseDto>.Ok(result, "Roles retrieved."));
    }

    [HttpGet("{roleName}/permissions")]
    public async Task<ActionResult<ApiResponse<RolePermissionsResponseDto>>> GetRolePermissions(
        string roleName,
        CancellationToken cancellationToken)
    {
        var result = await permissionService.GetRolePermissionsAsync(roleName, cancellationToken);
        return Ok(ApiResponse<RolePermissionsResponseDto>.Ok(result, "Role capabilities retrieved."));
    }
}
