using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHealthThresholdConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM SYSTEM_CONFIGURATIONS
                WHERE config_key IN ('health_low_hours_threshold', 'health_approaching_deadline_days');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM SYSTEM_CONFIGURATIONS WHERE config_key = 'health_low_hours_threshold')
                INSERT INTO SYSTEM_CONFIGURATIONS (config_key, config_value, description, updated_at, updated_by_user_id)
                VALUES ('health_low_hours_threshold', '0.6', 'Ratio of expected hours below which LOW_HOURS flag is raised (0.01 to 1)', GETUTCDATE(), NULL);

                IF NOT EXISTS (SELECT 1 FROM SYSTEM_CONFIGURATIONS WHERE config_key = 'health_approaching_deadline_days')
                INSERT INTO SYSTEM_CONFIGURATIONS (config_key, config_value, description, updated_at, updated_by_user_id)
                VALUES ('health_approaching_deadline_days', '28', 'Days before project end date to flag APPROACHING_DEADLINE', GETUTCDATE(), NULL);
                """);
        }
    }
}
