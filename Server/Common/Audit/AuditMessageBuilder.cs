using System.Text.Json;
using Server.Common.Roles;

namespace Server.Common.Audit;

public static class AuditMessageBuilder
{
    public static string BuildRoleChangeSummary(string targetUserName, string? oldRole, string newRole) =>
        $"Changed {targetUserName}'s role from {FormatRole(oldRole)} to {FormatRole(newRole)}";

    public static string BuildLoginSummary(string userName, bool success) =>
        success
            ? $"{userName} signed in successfully"
            : $"Failed sign-in attempt for {userName}";

    public static string BuildPasswordChangeSummary(string userName) =>
        $"{userName} changed their password";

    public static string BuildManagerAssignmentSummary(string employeeName, string managerName) =>
        $"Assigned {managerName} as manager for {employeeName}";

    public static string BuildSkillChangeSummary(string employeeName, string action, string skillName) =>
        $"{action} skill '{skillName}' for {employeeName}";

    public static string BuildTimesheetActionSummary(string action, string employeeName, string weekLabel) =>
        $"{action} timesheet for {employeeName} — {weekLabel}";

    public static string BuildFromStoredValues(
        string entityName,
        string actionType,
        string? oldValuesJson,
        string? newValuesJson)
    {
        if (entityName == AuditEntityConstants.Users && actionType == AuditActionConstants.Update)
        {
            var oldRole = TryReadStringProperty(oldValuesJson, "role");
            var newRole = TryReadStringProperty(newValuesJson, "role");
            if (oldRole is not null && newRole is not null)
                return $"Changed user role from {FormatRole(oldRole)} to {FormatRole(newRole)}";
        }

        return $"{FormatEntity(entityName)} {FormatAction(actionType).ToLowerInvariant()}";
    }

    private static string? TryReadStringProperty(string? json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static string FormatRole(string? role) =>
        role?.ToUpperInvariant() switch
        {
            RoleConstants.Admin => "Admin",
            RoleConstants.Manager => "Manager",
            RoleConstants.Employee => "Employee/Resource",
            null or "" => "Unknown",
            _ => role
        };

    private static string FormatEntity(string entityName) =>
        entityName switch
        {
            AuditEntityConstants.Users => "User account",
            AuditEntityConstants.Employees => "Employee",
            AuditEntityConstants.Projects => "Project",
            AuditEntityConstants.ProjectAllocations => "Allocation",
            AuditEntityConstants.Timesheets => "Timesheet",
            AuditEntityConstants.SystemConfigurations => "System setting",
            AuditEntityConstants.ResourceProfiles => "Resource profile",
            AuditEntityConstants.Roles => "Role permissions",
            AuditEntityConstants.Auth => "Sign-in",
            _ => entityName
        };

    private static string FormatAction(string actionType) =>
        actionType switch
        {
            AuditActionConstants.Create => "Created",
            AuditActionConstants.Update => "Updated",
            AuditActionConstants.Deactivate => "Deactivated",
            AuditActionConstants.End => "Ended",
            AuditActionConstants.Login => "Signed in",
            AuditActionConstants.LoginFailed => "Failed sign-in",
            _ => actionType
        };
}
