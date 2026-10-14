using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationRequests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitEmployeesAndRequestReceiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnitReviewers_Users_user_id",
                table: "UnitReviewers");

            migrationBuilder.AddColumn<Guid>(
                name: "receiver_employee_id",
                table: "Requests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    organizational_unit_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    created_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.id);
                    table.UniqueConstraint("AK_Employees_id_organizational_unit_id", x => new { x.id, x.organizational_unit_id });
                    table.UniqueConstraint("AK_Employees_user_id_organizational_unit_id", x => new { x.user_id, x.organizational_unit_id });
                    table.ForeignKey(
                        name: "FK_Employees_OrganizationalUnits_organizational_unit_id",
                        column: x => x.organizational_unit_id,
                        principalTable: "OrganizationalUnits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Existing reviewer assignments are trusted membership data. Do not guess
            // a unit for ordinary accounts, or a receiver for existing requests.
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT [user_id] FROM [UnitReviewers]
                    GROUP BY [user_id] HAVING COUNT(*) > 1
                )
                    THROW 50000, 'Assign each reviewer to one employee unit before applying this migration.', 1;

                INSERT INTO [Employees] ([id], [user_id], [organizational_unit_id], [name], [created_date_utc])
                SELECT NEWID(), reviewer.[user_id], reviewer.[organizational_unit_id], account.[display_name], SYSUTCDATETIME()
                FROM [UnitReviewers] AS reviewer
                INNER JOIN [Users] AS account ON account.[id] = reviewer.[user_id];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Requests_receiver_employee_id_organizational_unit_id",
                table: "Requests",
                columns: new[] { "receiver_employee_id", "organizational_unit_id" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_organizational_unit_id_name_id",
                table: "Employees",
                columns: new[] { "organizational_unit_id", "name", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_user_id",
                table: "Employees",
                column: "user_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Employees_receiver_employee_id_organizational_unit_id",
                table: "Requests",
                columns: new[] { "receiver_employee_id", "organizational_unit_id" },
                principalTable: "Employees",
                principalColumns: new[] { "id", "organizational_unit_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UnitReviewers_Employees_user_id_organizational_unit_id",
                table: "UnitReviewers",
                columns: new[] { "user_id", "organizational_unit_id" },
                principalTable: "Employees",
                principalColumns: new[] { "user_id", "organizational_unit_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Employees_receiver_employee_id_organizational_unit_id",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_UnitReviewers_Employees_user_id_organizational_unit_id",
                table: "UnitReviewers");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Requests_receiver_employee_id_organizational_unit_id",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "receiver_employee_id",
                table: "Requests");

            migrationBuilder.AddForeignKey(
                name: "FK_UnitReviewers_Users_user_id",
                table: "UnitReviewers",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
