using Server.Common.Roles;

namespace Server.Common.Permissions;

public static class PermissionSeedData
{
    public sealed record PermissionDefinition(string Resource, string Action, string Description);

    public static IReadOnlyList<PermissionDefinition> AllPermissions { get; } =
    [
        new(PermissionConstants.Users.Resource, PermissionConstants.Users.Read, "View user accounts"),
        new(PermissionConstants.Users.Resource, PermissionConstants.Users.Create, "Create user accounts"),
        new(PermissionConstants.Users.Resource, PermissionConstants.Users.Update, "Update user accounts"),
        new(PermissionConstants.Users.Resource, PermissionConstants.Users.Deactivate, "Deactivate user accounts"),
        new(PermissionConstants.Users.Resource, PermissionConstants.Users.ResetPassword, "Reset user passwords"),
        new(PermissionConstants.Users.Resource, PermissionConstants.Users.ChangeRole, "Change user roles"),
        new(PermissionConstants.Employees.Resource, PermissionConstants.Employees.Read, "View employees and resources"),
        new(PermissionConstants.Employees.Resource, PermissionConstants.Employees.Update, "Update employee profiles"),
        new(PermissionConstants.Employees.Resource, PermissionConstants.Employees.Deactivate, "Deactivate employees"),
        new(PermissionConstants.Employees.Resource, PermissionConstants.Employees.AssignManager, "Assign managers to resources"),
        new(PermissionConstants.Employees.Resource, PermissionConstants.Employees.ManageSkills, "Manage employee skills"),
        new(PermissionConstants.Projects.Resource, PermissionConstants.Projects.Read, "View projects"),
        new(PermissionConstants.Projects.Resource, PermissionConstants.Projects.Create, "Create projects"),
        new(PermissionConstants.Projects.Resource, PermissionConstants.Projects.Update, "Update projects"),
        new(PermissionConstants.Projects.Resource, PermissionConstants.Projects.ManageMilestones, "Manage project milestones"),
        new(PermissionConstants.Allocations.Resource, PermissionConstants.Allocations.Read, "View allocations"),
        new(PermissionConstants.Allocations.Resource, PermissionConstants.Allocations.Create, "Create allocations"),
        new(PermissionConstants.Allocations.Resource, PermissionConstants.Allocations.Update, "Update allocations"),
        new(PermissionConstants.Allocations.Resource, PermissionConstants.Allocations.End, "End allocations"),
        new(PermissionConstants.Timesheets.Resource, PermissionConstants.Timesheets.ReadOwn, "View own timesheets"),
        new(PermissionConstants.Timesheets.Resource, PermissionConstants.Timesheets.Submit, "Submit timesheets"),
        new(PermissionConstants.Timesheets.Resource, PermissionConstants.Timesheets.ReadTeam, "View team timesheets"),
        new(PermissionConstants.Timesheets.Resource, PermissionConstants.Timesheets.Review, "Review team timesheets"),
        new(PermissionConstants.SystemConfig.Resource, PermissionConstants.SystemConfig.Read, "View system configuration"),
        new(PermissionConstants.SystemConfig.Resource, PermissionConstants.SystemConfig.Update, "Update system configuration"),
        new(PermissionConstants.AiInsights.Resource, PermissionConstants.AiInsights.Read, "Use AI insights"),
        new(PermissionConstants.ActivityTags.Resource, PermissionConstants.ActivityTags.Read, "View activity tags"),
        new(PermissionConstants.ActivityTags.Resource, PermissionConstants.ActivityTags.Create, "Create activity tags"),
        new(PermissionConstants.Roles.Resource, PermissionConstants.Roles.Read, "View role capabilities"),
        new(PermissionConstants.AuditLogs.Resource, PermissionConstants.AuditLogs.Read, "View activity log")
    ];

    public static string ToCode(PermissionDefinition permission) =>
        PermissionConstants.Code(permission.Resource, permission.Action);

    public static IReadOnlySet<string> GetPermissionCodesForRole(string roleName)
    {
        if (roleName == RoleConstants.Admin)
            return AllPermissions.Select(ToCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roleName == RoleConstants.Manager)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PermissionConstants.Code(PermissionConstants.Employees.Resource, PermissionConstants.Employees.Read),
                PermissionConstants.Code(PermissionConstants.Projects.Resource, PermissionConstants.Projects.Read),
                PermissionConstants.Code(PermissionConstants.Projects.Resource, PermissionConstants.Projects.Update),
                PermissionConstants.Code(PermissionConstants.Allocations.Resource, PermissionConstants.Allocations.Read),
                PermissionConstants.Code(PermissionConstants.Allocations.Resource, PermissionConstants.Allocations.Create),
                PermissionConstants.Code(PermissionConstants.Allocations.Resource, PermissionConstants.Allocations.Update),
                PermissionConstants.Code(PermissionConstants.Allocations.Resource, PermissionConstants.Allocations.End),
                PermissionConstants.Code(PermissionConstants.Timesheets.Resource, PermissionConstants.Timesheets.ReadTeam),
                PermissionConstants.Code(PermissionConstants.Timesheets.Resource, PermissionConstants.Timesheets.Review),
                PermissionConstants.Code(PermissionConstants.AiInsights.Resource, PermissionConstants.AiInsights.Read)
            };
        }

        if (roleName == RoleConstants.Employee)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                PermissionConstants.Code(PermissionConstants.Timesheets.Resource, PermissionConstants.Timesheets.ReadOwn),
                PermissionConstants.Code(PermissionConstants.Timesheets.Resource, PermissionConstants.Timesheets.Submit),
                PermissionConstants.Code(PermissionConstants.ActivityTags.Resource, PermissionConstants.ActivityTags.Read),
                PermissionConstants.Code(PermissionConstants.ActivityTags.Resource, PermissionConstants.ActivityTags.Create)
            };
        }

        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
