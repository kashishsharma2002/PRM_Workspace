using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailNotificationInfrastructure : Migration
    {
        private static readonly DateTime SeedTimestamp = new(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_timesheet_frozen",
                table: "RESOURCE_PROFILES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EMAIL_LOGS",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    recipient = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    subject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    sent_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    entity_reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    processing_duration = table.Column<long>(type: "bigint", nullable: false),
                    correlation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMAIL_LOGS", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "EMAIL_TEMPLATES",
                columns: table => new
                {
                    template_key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    subject_template = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    body_template = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EMAIL_TEMPLATES", x => x.template_key);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EMAIL_LOGS_recipient_email_type_sent_time",
                table: "EMAIL_LOGS",
                columns: new[] { "recipient", "email_type", "sent_time" });

            migrationBuilder.InsertData(
                table: "SYSTEM_CONFIGURATIONS",
                columns: new[] { "id", "config_key", "config_value", "description", "updated_at", "updated_by_user_id" },
                values: new object[,]
                {
                    { 5L, "smtp_host", "", "SMTP server hostname", SeedTimestamp, null },
                    { 6L, "smtp_port", "587", "SMTP server port", SeedTimestamp, null },
                    { 7L, "smtp_username", "", "SMTP authentication username", SeedTimestamp, null },
                    { 8L, "smtp_password", "", "Encrypted SMTP password", SeedTimestamp, null },
                    { 9L, "smtp_ssl_enabled", "true", "Enable SSL/TLS for SMTP", SeedTimestamp, null },
                    { 10L, "smtp_from_email", "", "Sender email address", SeedTimestamp, null },
                    { 11L, "smtp_from_name", "PRM Notifications", "Sender display name", SeedTimestamp, null },
                    { 12L, "timesheet_compliance_deadline_day", "5", "Day of month for timesheet deadline", SeedTimestamp, null },
                    { 13L, "timesheet_compliance_reminder1_day", "3", "Day of month for first reminder", SeedTimestamp, null },
                    { 14L, "timesheet_compliance_reminder2_day", "4", "Day of month for second reminder", SeedTimestamp, null },
                    { 15L, "timesheet_compliance_freeze_day", "6", "Day of month to freeze non-compliant accounts", SeedTimestamp, null }
                });

            migrationBuilder.InsertData(
                table: "EMAIL_TEMPLATES",
                columns: new[] { "template_key", "subject_template", "body_template", "created_at", "updated_at" },
                values: new object[,]
                {
                    {
                        "TIMESHEET_REMINDER_1",
                        "Action Required: Timesheet Submission Reminder (Week Ending {{WeekEndDate}})",
                        "<p>Dear {{EmployeeName}},</p><p>This is a reminder to submit your timesheet for the week ending <strong>{{WeekEndDate}}</strong>.</p><p>Ensure you log in and submit your hours to prevent late compliance issues.</p><p>Best regards,<br/>PRM Compliance System</p>",
                        SeedTimestamp,
                        SeedTimestamp
                    },
                    {
                        "TIMESHEET_REMINDER_2",
                        "URGENT: Timesheet Submission Required (Week Ending {{WeekEndDate}})",
                        "<p>Dear {{EmployeeName}},</p><p>Your timesheet for the week ending <strong>{{WeekEndDate}}</strong> has not been received.</p><p>Please submit it immediately. Failure to complete this action will cause your access to be frozen.</p><p>Best regards,<br/>PRM Compliance System</p>",
                        SeedTimestamp,
                        SeedTimestamp
                    },
                    {
                        "TIMESHEET_FREEZE",
                        "CRITICAL: Timesheet Access Frozen (Week Ending {{WeekEndDate}})",
                        "<p>Dear {{EmployeeName}},</p><p>Your timesheet access has been <strong>frozen</strong> because your timesheet for the week ending <strong>{{WeekEndDate}}</strong> is missing.</p><p>Please contact your manager <strong>{{ManagerName}}</strong> to request restoration of timesheet privileges.</p><p>Best regards,<br/>PRM Compliance System</p>",
                        SeedTimestamp,
                        SeedTimestamp
                    },
                    {
                        "PROJECT_AT_RISK",
                        "Alert: Project [{{ProjectName}}] Health Transitioned to RED",
                        "<p>Dear {{ManagerName}},</p><p>The project <strong>{{ProjectName}}</strong> has transitioned to <strong>RED (At Risk)</strong> health status.</p><p><strong>AI Risk Mitigations:</strong><br/>{{RiskSummary}}</p><p><strong>Resource Recommendations:</strong><br/>{{ResourceList}}</p><p>Best regards,<br/>PRM Management System</p>",
                        SeedTimestamp,
                        SeedTimestamp
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EMAIL_LOGS");

            migrationBuilder.DropTable(
                name: "EMAIL_TEMPLATES");

            for (long id = 15L; id >= 5L; id--)
            {
                migrationBuilder.DeleteData(
                    table: "SYSTEM_CONFIGURATIONS",
                    keyColumn: "id",
                    keyValue: id);
            }

            migrationBuilder.DropColumn(
                name: "is_timesheet_frozen",
                table: "RESOURCE_PROFILES");
        }
    }
}
