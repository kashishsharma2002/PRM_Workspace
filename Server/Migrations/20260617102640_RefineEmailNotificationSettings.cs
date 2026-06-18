using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class RefineEmailNotificationSettings : Migration
    {
        private static readonly DateTime SeedTimestamp = new(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc);

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            for (long id = 11L; id >= 5L; id--)
            {
                migrationBuilder.DeleteData(
                    table: "SYSTEM_CONFIGURATIONS",
                    keyColumn: "id",
                    keyValue: id);
            }

            migrationBuilder.DeleteData(
                table: "SYSTEM_CONFIGURATIONS",
                keyColumn: "id",
                keyValue: 13L);

            migrationBuilder.DeleteData(
                table: "SYSTEM_CONFIGURATIONS",
                keyColumn: "id",
                keyValue: 14L);

            migrationBuilder.DeleteData(
                table: "SYSTEM_CONFIGURATIONS",
                keyColumn: "id",
                keyValue: 15L);

            migrationBuilder.UpdateData(
                table: "SYSTEM_CONFIGURATIONS",
                keyColumn: "id",
                keyValue: 12L,
                columns: new[] { "config_value", "description", "updated_at" },
                values: new object[] { "1", "Working days after week end (Sunday) when timesheet submission is due", SeedTimestamp });

            migrationBuilder.UpdateData(
                table: "EMAIL_TEMPLATES",
                keyColumn: "template_key",
                keyValue: "TIMESHEET_REMINDER_1",
                columns: new[] { "subject_template", "body_template", "updated_at" },
                values: new object[]
                {
                    "Action Required: Timesheet Submission Reminder (Week Ending {{WeekEndDate}})",
                    "<p>Dear {{EmployeeName}},</p><p>This is your first reminder to submit your timesheet for the week ending <strong>{{WeekEndDate}}</strong>.</p><p>Please log in and submit your hours as soon as possible.</p><p>Best regards,<br/>PRM Compliance System</p>",
                    SeedTimestamp
                });

            migrationBuilder.UpdateData(
                table: "EMAIL_TEMPLATES",
                keyColumn: "template_key",
                keyValue: "TIMESHEET_REMINDER_2",
                columns: new[] { "subject_template", "body_template", "updated_at" },
                values: new object[]
                {
                    "URGENT: Timesheet Submission Required (Week Ending {{WeekEndDate}})",
                    "<p>Dear {{EmployeeName}},</p><p>Your timesheet for the week ending <strong>{{WeekEndDate}}</strong> is still pending.</p><p>Submit it today to avoid having your timesheet access frozen.</p><p>Best regards,<br/>PRM Compliance System</p>",
                    SeedTimestamp
                });

            migrationBuilder.UpdateData(
                table: "EMAIL_TEMPLATES",
                keyColumn: "template_key",
                keyValue: "TIMESHEET_FREEZE",
                columns: new[] { "subject_template", "body_template", "updated_at" },
                values: new object[]
                {
                    "CRITICAL: Timesheet Access Frozen (Week Ending {{WeekEndDate}})",
                    "<p>Dear {{EmployeeName}},</p><p>Your timesheet access has been <strong>frozen</strong> because your timesheet for the week ending <strong>{{WeekEndDate}}</strong> was not submitted after two reminders.</p><p>You can still log in and view records, but you cannot create, update, or submit timesheet entries until your manager restores access.</p><p>Contact <strong>{{ManagerName}}</strong> for assistance.</p><p>Best regards,<br/>PRM Compliance System</p>",
                    SeedTimestamp
                });

            migrationBuilder.UpdateData(
                table: "EMAIL_TEMPLATES",
                keyColumn: "template_key",
                keyValue: "PROJECT_AT_RISK",
                columns: new[] { "subject_template", "body_template", "updated_at" },
                values: new object[]
                {
                    "Alert: Project {{ProjectName}} is At Risk",
                    "<p>Dear {{ManagerName}},</p><p>The project <strong>{{ProjectName}}</strong> has been marked <strong>{{HealthStatus}}</strong>.</p><p><strong>Key Milestones:</strong><br/>{{Milestones}}</p><p><strong>AI Risk Summary:</strong><br/>{{RiskSummary}}</p><p><strong>Suggested Resources:</strong><br/>{{ResourceList}}</p><p>Best regards,<br/>PRM Management System</p>",
                    SeedTimestamp
                });

            migrationBuilder.InsertData(
                table: "EMAIL_TEMPLATES",
                columns: new[] { "template_key", "subject_template", "body_template", "created_at", "updated_at" },
                values: new object[]
                {
                    "TIMESHEET_FREEZE_MANAGER",
                    "Action Required: Employee Timesheet Access Frozen — {{EmployeeName}}",
                    "<p>Dear {{ManagerName}},</p><p>Timesheet submission access has been <strong>frozen</strong> for <strong>{{EmployeeName}}</strong> because their timesheet for the week ending <strong>{{WeekEndDate}}</strong> was not submitted after two reminders.</p><p>The employee can still log in and view data, but cannot submit timesheets until you restore access from the manager dashboard.</p><p>Best regards,<br/>PRM Compliance System</p>",
                    SeedTimestamp,
                    SeedTimestamp
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EMAIL_TEMPLATES",
                keyColumn: "template_key",
                keyValue: "TIMESHEET_FREEZE_MANAGER");
        }
    }
}
