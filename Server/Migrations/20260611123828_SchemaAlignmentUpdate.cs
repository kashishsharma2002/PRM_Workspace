using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class SchemaAlignmentUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "USER_SKILLS",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    skill_id = table.Column<long>(type: "bigint", nullable: false),
                    proficiency_level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_SKILLS", x => new { x.user_id, x.skill_id });
                    table.ForeignKey(
                        name: "FK_USER_SKILLS_SKILLS_skill_id",
                        column: x => x.skill_id,
                        principalTable: "SKILLS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USER_SKILLS_USERS_user_id",
                        column: x => x.user_id,
                        principalTable: "USERS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO USER_SKILLS (user_id, skill_id, proficiency_level, created_at)
                SELECT rp.user_id, rps.skill_id, rps.proficiency_level, rps.created_at
                FROM RESOURCE_PROFILE_SKILLS rps
                INNER JOIN RESOURCE_PROFILES rp ON rp.id = rps.resource_profile_id
                """);

            migrationBuilder.DropTable(
                name: "RESOURCE_PROFILE_SKILLS");

            migrationBuilder.CreateIndex(
                name: "IX_USER_SKILLS_skill_id",
                table: "USER_SKILLS",
                column: "skill_id");

            migrationBuilder.DropForeignKey(
                name: "FK_RESOURCE_PROFILES_RESOURCE_PROFILES_manager_id",
                table: "RESOURCE_PROFILES");

            migrationBuilder.Sql("""
                UPDATE rp
                SET rp.manager_id = mp.user_id
                FROM RESOURCE_PROFILES rp
                INNER JOIN RESOURCE_PROFILES mp ON rp.manager_id = mp.id
                WHERE rp.manager_id IS NOT NULL
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_RESOURCE_PROFILES_USERS_manager_id",
                table: "RESOURCE_PROFILES",
                column: "manager_id",
                principalTable: "USERS",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<DateTime>(
                name: "completed_at",
                table: "PROJECT_MILESTONES",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "PROJECT_MILESTONES");

            migrationBuilder.DropForeignKey(
                name: "FK_RESOURCE_PROFILES_USERS_manager_id",
                table: "RESOURCE_PROFILES");

            migrationBuilder.Sql("""
                UPDATE rp
                SET rp.manager_id = mp.id
                FROM RESOURCE_PROFILES rp
                INNER JOIN RESOURCE_PROFILES mp ON rp.manager_id = mp.user_id
                WHERE rp.manager_id IS NOT NULL
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_RESOURCE_PROFILES_RESOURCE_PROFILES_manager_id",
                table: "RESOURCE_PROFILES",
                column: "manager_id",
                principalTable: "RESOURCE_PROFILES",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateTable(
                name: "RESOURCE_PROFILE_SKILLS",
                columns: table => new
                {
                    resource_profile_id = table.Column<long>(type: "bigint", nullable: false),
                    skill_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    proficiency_level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RESOURCE_PROFILE_SKILLS", x => new { x.resource_profile_id, x.skill_id });
                    table.ForeignKey(
                        name: "FK_RESOURCE_PROFILE_SKILLS_RESOURCE_PROFILES_resource_profile_id",
                        column: x => x.resource_profile_id,
                        principalTable: "RESOURCE_PROFILES",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RESOURCE_PROFILE_SKILLS_SKILLS_skill_id",
                        column: x => x.skill_id,
                        principalTable: "SKILLS",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO RESOURCE_PROFILE_SKILLS (resource_profile_id, skill_id, proficiency_level, created_at)
                SELECT rp.id, us.skill_id, us.proficiency_level, us.created_at
                FROM USER_SKILLS us
                INNER JOIN RESOURCE_PROFILES rp ON rp.user_id = us.user_id
                """);

            migrationBuilder.DropTable(
                name: "USER_SKILLS");

            migrationBuilder.CreateIndex(
                name: "IX_RESOURCE_PROFILE_SKILLS_skill_id",
                table: "RESOURCE_PROFILE_SKILLS",
                column: "skill_id");
        }
    }
}
