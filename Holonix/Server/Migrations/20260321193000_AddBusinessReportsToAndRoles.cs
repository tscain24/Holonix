using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260321193000_AddBusinessReportsToAndRoles")]
    public partial class AddBusinessReportsToAndRoles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReportsToUserId",
                schema: "business",
                table: "BusinessUser",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUser_ReportsToUserId",
                schema: "business",
                table: "BusinessUser",
                column: "ReportsToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRole_Name",
                schema: "business",
                table: "BusinessRole",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessUser_Users_ReportsToUserId",
                schema: "business",
                table: "BusinessUser",
                column: "ReportsToUserId",
                principalSchema: "authentication",
                principalTable: "Users",
                principalColumn: "UsersId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessUser_Users_ReportsToUserId",
                schema: "business",
                table: "BusinessUser");

            migrationBuilder.DropIndex(
                name: "IX_BusinessUser_ReportsToUserId",
                schema: "business",
                table: "BusinessUser");

            migrationBuilder.DropIndex(
                name: "IX_BusinessRole_Name",
                schema: "business",
                table: "BusinessRole");

            migrationBuilder.DropColumn(
                name: "ReportsToUserId",
                schema: "business",
                table: "BusinessUser");
        }
    }
}
