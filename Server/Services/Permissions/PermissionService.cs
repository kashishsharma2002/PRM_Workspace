using Server.Common.Permissions;
using Server.Common.Roles;
using Server.Exceptions;
using Server.Models.DTOs.Roles;
using Server.Repositories.Permissions;

namespace Server.Services.Permissions;

public class PermissionService(IPermissionRepository permissionRepository) : IPermissionService
{
    public async Task<RoleListResponseDto> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await permissionRepository.GetAllRolesAsync(cancellationToken);
        var userCounts = await permissionRepository.GetUserCountsByRoleAsync(cancellationToken);

        var summaries = new List<RoleSummaryDto>();
        foreach (var role in roles)
        {
            userCounts.TryGetValue(role.Id, out var userCount);

            summaries.Add(new RoleSummaryDto
            {
                RoleName = role.RoleName,
                UserCount = userCount,
                PermissionCount = PermissionSeedData.GetPermissionCodesForRole(role.RoleName).Count
            });
        }

        return new RoleListResponseDto { Roles = summaries };
    }

    public async Task<RolePermissionsResponseDto> GetRolePermissionsAsync(
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var role = await GetRoleOrThrowAsync(roleName, cancellationToken);
        var assignedCodes = PermissionSeedData.GetPermissionCodesForRole(role.RoleName);

        var items = PermissionSeedData.AllPermissions
            .Where(definition => assignedCodes.Contains(PermissionSeedData.ToCode(definition)))
            .Select((definition, index) => new PermissionItemDto
            {
                Id = index + 1,
                Resource = definition.Resource,
                Action = definition.Action,
                Code = PermissionSeedData.ToCode(definition),
                Description = definition.Description
            })
            .ToList();

        return new RolePermissionsResponseDto
        {
            RoleName = role.RoleName,
            Permissions = items
        };
    }

    private async Task<Models.Entities.Role> GetRoleOrThrowAsync(string roleName, CancellationToken cancellationToken)
    {
        var normalized = roleName.Trim().ToUpperInvariant();
        if (!RoleConstants.All.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            throw new ValidationAppException("Invalid role.");

        return await permissionRepository.GetRoleByNameAsync(normalized, cancellationToken)
            ?? throw new NotFoundAppException("Role not found.");
    }
}
