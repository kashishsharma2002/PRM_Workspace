namespace Server.Models.DTOs.Roles;

public class RoleSummaryDto
{
    public string RoleName { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public int PermissionCount { get; set; }
}

public class RoleListResponseDto
{
    public IReadOnlyList<RoleSummaryDto> Roles { get; set; } = [];
}

public class PermissionItemDto
{
    public long Id { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class RolePermissionsResponseDto
{
    public string RoleName { get; set; } = string.Empty;
    public IReadOnlyList<PermissionItemDto> Permissions { get; set; } = [];
}
