using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class SeedRbacAndAuditSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "summary",
                table: "AUDIT_LOGS",
                type: "text",
                nullable: true);

            migrationBuilder.InsertData(
                table: "PERMISSIONS",
                columns: new[] { "id", "resource", "action", "description" },
                values: new object[,]
                {
                    { 1L, "users", "read", "View user accounts" },
                    { 2L, "users", "create", "Create user accounts" },
                    { 3L, "users", "update", "Update user accounts" },
                    { 4L, "users", "deactivate", "Deactivate user accounts" },
                    { 5L, "users", "reset_password", "Reset user passwords" },
                    { 6L, "users", "change_role", "Change user roles" },
                    { 7L, "employees", "read", "View employees and resources" },
                    { 8L, "employees", "update", "Update employee profiles" },
                    { 9L, "employees", "deactivate", "Deactivate employees" },
                    { 10L, "employees", "assign_manager", "Assign managers to resources" },
                    { 11L, "employees", "manage_skills", "Manage employee skills" },
                    { 12L, "projects", "read", "View projects" },
                    { 13L, "projects", "create", "Create projects" },
                    { 14L, "projects", "update", "Update projects" },
                    { 15L, "projects", "manage_milestones", "Manage project milestones" },
                    { 16L, "allocations", "read", "View allocations" },
                    { 17L, "allocations", "create", "Create allocations" },
                    { 18L, "allocations", "update", "Update allocations" },
                    { 19L, "allocations", "end", "End allocations" },
                    { 20L, "allocations", "delete", "Delete allocations" },
                    { 21L, "timesheets", "read_own", "View own timesheets" },
                    { 22L, "timesheets", "submit", "Submit timesheets" },
                    { 23L, "timesheets", "read_team", "View team timesheets" },
                    { 24L, "timesheets", "review", "Review team timesheets" },
                    { 25L, "system_config", "read", "View system configuration" },
                    { 26L, "system_config", "update", "Update system configuration" },
                    { 27L, "ai_insights", "read", "Use AI insights" },
                    { 28L, "activity_tags", "read", "View activity tags" },
                    { 29L, "activity_tags", "create", "Create activity tags" },
                    { 30L, "roles", "read", "View roles" },
                    { 31L, "roles", "manage_permissions", "Manage role permissions" },
                    { 32L, "audit_logs", "read", "View activity log" }
                });

            for (long permissionId = 1L; permissionId <= 32L; permissionId++)
            {
                migrationBuilder.InsertData(
                    table: "ROLE_PERMISSIONS",
                    columns: new[] { "role_id", "permission_id" },
                    values: new object[] { 1L, permissionId });
            }

            foreach (var permissionId in new long[] { 7L, 12L, 14L, 16L, 17L, 18L, 19L, 23L, 24L, 27L })
            {
                migrationBuilder.InsertData(
                    table: "ROLE_PERMISSIONS",
                    columns: new[] { "role_id", "permission_id" },
                    values: new object[] { 2L, permissionId });
            }

            foreach (var permissionId in new long[] { 21L, 22L, 28L, 29L })
            {
                migrationBuilder.InsertData(
                    table: "ROLE_PERMISSIONS",
                    columns: new[] { "role_id", "permission_id" },
                    values: new object[] { 3L, permissionId });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            for (long roleId = 3L; roleId >= 1L; roleId--)
            {
                migrationBuilder.Sql($"DELETE FROM ROLE_PERMISSIONS WHERE role_id = {roleId}");
            }

            for (long permissionId = 32L; permissionId >= 1L; permissionId--)
            {
                migrationBuilder.DeleteData(
                    table: "PERMISSIONS",
                    keyColumn: "id",
                    keyValue: permissionId);
            }

            migrationBuilder.DropColumn(
                name: "summary",
                table: "AUDIT_LOGS");
        }
    }
}
