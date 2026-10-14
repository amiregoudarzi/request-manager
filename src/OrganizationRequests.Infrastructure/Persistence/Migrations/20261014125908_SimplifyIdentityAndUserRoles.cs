using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationRequests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyIdentityAndUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [UserClaims])
                    OR EXISTS (SELECT 1 FROM [UserLogins])
                    OR EXISTS (SELECT 1 FROM [UserTokens])
                    THROW 50000, 'Unused Identity tables contain data; migrate that data before removing the tables.', 1;
                """);

            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "Users",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Employee");

            migrationBuilder.Sql("""
                UPDATE account
                SET [role] = 'UnitReviewer'
                FROM [Users] AS account
                WHERE EXISTS (SELECT 1 FROM [UnitReviewers] AS reviewer WHERE reviewer.[user_id] = account.[id]);
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Role",
                table: "Users",
                sql: "[role] IN ('Employee','UnitReviewer')");

            migrationBuilder.DropTable(name: "UnitReviewers");
            migrationBuilder.DropTable(name: "UserClaims");
            migrationBuilder.DropTable(name: "UserLogins");
            migrationBuilder.DropTable(name: "UserTokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Role",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "UnitReviewers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    organizational_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitReviewers", x => x.id);
                    table.ForeignKey(
                        name: "FK_UnitReviewers_Employees_user_id_organizational_unit_id",
                        columns: x => new { x.user_id, x.organizational_unit_id },
                        principalTable: "Employees",
                        principalColumns: new[] { "user_id", "organizational_unit_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitReviewers_OrganizationalUnits_organizational_unit_id",
                        column: x => x.organizational_unit_id,
                        principalTable: "OrganizationalUnits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    claim_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    claim_value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    provider_key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    provider_display_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    login_provider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnitReviewers_organizational_unit_id",
                table: "UnitReviewers",
                column: "organizational_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_UnitReviewers_user_id_organizational_unit_id",
                table: "UnitReviewers",
                columns: new[] { "user_id", "organizational_unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_user_id",
                table: "UserClaims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_user_id",
                table: "UserLogins",
                column: "user_id");

            migrationBuilder.Sql("""
                INSERT INTO [UnitReviewers] ([id], [created_date_utc], [organizational_unit_id], [user_id])
                SELECT NEWID(), SYSUTCDATETIME(), employee.[organizational_unit_id], account.[id]
                FROM [Users] AS account
                INNER JOIN [Employees] AS employee ON employee.[user_id] = account.[id]
                WHERE account.[role] = 'UnitReviewer';
                """);

            migrationBuilder.DropColumn(name: "role", table: "Users");
        }
    }
}
