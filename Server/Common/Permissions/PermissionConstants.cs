namespace Server.Common.Permissions;

public static class PermissionConstants
{
    public static string Code(string resource, string action) => $"{resource}:{action}";

    public static class Users
    {
        public const string Resource = "users";
        public const string Read = "read";
        public const string Create = "create";
        public const string Update = "update";
        public const string Deactivate = "deactivate";
        public const string ResetPassword = "reset_password";
        public const string ChangeRole = "change_role";
    }

    public static class Employees
    {
        public const string Resource = "employees";
        public const string Read = "read";
        public const string Update = "update";
        public const string Deactivate = "deactivate";
        public const string AssignManager = "assign_manager";
        public const string ManageSkills = "manage_skills";
    }

    public static class Projects
    {
        public const string Resource = "projects";
        public const string Read = "read";
        public const string Create = "create";
        public const string Update = "update";
        public const string ManageMilestones = "manage_milestones";
    }

    public static class Allocations
    {
        public const string Resource = "allocations";
        public const string Read = "read";
        public const string Create = "create";
        public const string Update = "update";
        public const string End = "end";
    }

    public static class Timesheets
    {
        public const string Resource = "timesheets";
        public const string ReadOwn = "read_own";
        public const string Submit = "submit";
        public const string ReadTeam = "read_team";
        public const string Review = "review";
    }

    public static class SystemConfig
    {
        public const string Resource = "system_config";
        public const string Read = "read";
        public const string Update = "update";
    }

    public static class AiInsights
    {
        public const string Resource = "ai_insights";
        public const string Read = "read";
    }

    public static class ActivityTags
    {
        public const string Resource = "activity_tags";
        public const string Read = "read";
        public const string Create = "create";
    }

    public static class Roles
    {
        public const string Resource = "roles";
        public const string Read = "read";
    }

    public static class AuditLogs
    {
        public const string Resource = "audit_logs";
        public const string Read = "read";
    }

}
