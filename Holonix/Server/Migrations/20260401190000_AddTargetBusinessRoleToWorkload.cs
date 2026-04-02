using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260401190000_AddTargetBusinessRoleToWorkload")]
    public partial class AddTargetBusinessRoleToWorkload : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TargetBusinessRoleId",
                schema: "work",
                table: "Workload",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workload_TargetBusinessRoleId",
                schema: "work",
                table: "Workload",
                column: "TargetBusinessRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workload_BusinessRole_TargetBusinessRoleId",
                schema: "work",
                table: "Workload",
                column: "TargetBusinessRoleId",
                principalSchema: "business",
                principalTable: "BusinessRole",
                principalColumn: "BusinessRoleId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workload_BusinessRole_TargetBusinessRoleId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropIndex(
                name: "IX_Workload_TargetBusinessRoleId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropColumn(
                name: "TargetBusinessRoleId",
                schema: "work",
                table: "Workload");
        }
    }
}
