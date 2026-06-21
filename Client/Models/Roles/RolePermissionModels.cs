namespace Client.Models.Roles;

public class RoleSummary
{
    public string RoleName { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public int PermissionCount { get; set; }
}

public class RoleListResponse
{
    public List<RoleSummary> Roles { get; set; } = [];
}

public class PermissionItem
{
    public long Id { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class RolePermissionsResponse
{
    public string RoleName { get; set; } = string.Empty;
    public List<PermissionItem> Permissions { get; set; } = [];
}
