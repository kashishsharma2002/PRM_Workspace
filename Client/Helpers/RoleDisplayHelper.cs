using Client.Common;

namespace Client.Helpers;

public static class RoleDisplayHelper
{
    public static string Format(string? role) =>
        role?.ToUpperInvariant() switch
        {
            RoleConstants.Admin => "Admin",
            RoleConstants.Manager => "Manager",
            RoleConstants.Employee => "Employee/Resource",
            null or "" => "Unknown",
            _ => role
        };

    public static string? ResolveSystemRoleChoice(string? choice) =>
        choice switch
        {
            MenuChoices.One => RoleConstants.Admin,
            MenuChoices.Two => RoleConstants.Manager,
            MenuChoices.Three => RoleConstants.Employee,
            _ => null
        };
}
