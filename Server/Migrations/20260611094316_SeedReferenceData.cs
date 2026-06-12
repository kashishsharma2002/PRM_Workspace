using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    public partial class SeedReferenceData : Migration
    {
        private static readonly DateTime SeedTimestamp = new(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc);

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ROLES",
                columns: new[] { "id", "role_name", "created_at" },
                values: new object[,]
                {
                    { 1L, "ADMIN", SeedTimestamp },
                    { 2L, "MANAGER", SeedTimestamp },
                    { 3L, "EMPLOYEE", SeedTimestamp }
                });

            migrationBuilder.InsertData(
                table: "ACTIVITY_TAGS",
                columns: new[] { "id", "tag_code", "tag_name", "tag_category", "is_active", "created_at" },
                values: new object[,]
                {
                    { 1L, "BACKEND_API", "Backend API Development", "Backend", true, SeedTimestamp },
                    { 2L, "MICROSERVICES", "Microservices / Architecture", "Backend", true, SeedTimestamp },
                    { 3L, "DATABASE", "Database Design & Queries", "Backend", true, SeedTimestamp },
                    { 4L, "WEBSOCKET", "WebSocket / Real-time Features", "Backend", true, SeedTimestamp },
                    { 5L, "FRONTEND", "Frontend Development", "Frontend", true, SeedTimestamp },
                    { 6L, "CODE_REVIEW", "Code Review / Mentoring", "General", true, SeedTimestamp },
                    { 7L, "BUG_FIX", "Bug Fixing", "General", true, SeedTimestamp },
                    { 8L, "DEVOPS", "DevOps / Deployment", "DevOps", true, SeedTimestamp },
                    { 9L, "TESTING", "Testing & QA", "QA", true, SeedTimestamp },
                    { 10L, "DOCUMENTATION", "Documentation", "General", true, SeedTimestamp },
                    { 11L, "OTHER", "Other", "Other", true, SeedTimestamp }
                });

            migrationBuilder.InsertData(
                table: "SYSTEM_CONFIGURATIONS",
                columns: new[] { "id", "config_key", "config_value", "description", "updated_at", "updated_by_user_id" },
                values: new object[,]
                {
                    { 1L, "llm_provider", "Gemini", "Active LLM provider: Gemini or Groq", SeedTimestamp, null },
                    { 2L, "llm_api_key", "", "Encrypted API key", SeedTimestamp, null },
                    { 3L, "scheduler_interval_hours", "4", "Background scheduler interval in hours", SeedTimestamp, null },
                    { 4L, "max_weekly_hours", "40", "Maximum billable hours per employee per week", SeedTimestamp, null }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            for (long id = 4L; id >= 1L; id--)
            {
                migrationBuilder.DeleteData(
                    table: "SYSTEM_CONFIGURATIONS",
                    keyColumn: "id",
                    keyValue: id);
            }

            for (long id = 11L; id >= 1L; id--)
            {
                migrationBuilder.DeleteData(
                    table: "ACTIVITY_TAGS",
                    keyColumn: "id",
                    keyValue: id);
            }

            for (long id = 3L; id >= 1L; id--)
            {
                migrationBuilder.DeleteData(
                    table: "ROLES",
                    keyColumn: "id",
                    keyValue: id);
            }
        }
    }
}
