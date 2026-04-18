using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260404233000_AddBusinessSubServiceMetadataAndAssignments")]
    public partial class AddBusinessSubServiceMetadataAndAssignments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "business",
                table: "BusinessSubService",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                schema: "business",
                table: "BusinessSubService",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "BusinessSubServiceAssignment",
                schema: "business",
                columns: table => new
                {
                    BusinessSubServiceAssignmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessSubServiceId = table.Column<long>(type: "bigint", nullable: false),
                    BusinessUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSubServiceAssignment", x => x.BusinessSubServiceAssignmentId);
                    table.ForeignKey(
                        name: "FK_BusinessSubServiceAssignment_BusinessSubService_BusinessSubServiceId",
                        column: x => x.BusinessSubServiceId,
                        principalSchema: "business",
                        principalTable: "BusinessSubService",
                        principalColumn: "BusinessSubServiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessSubServiceAssignment_BusinessUser_BusinessUserId",
                        column: x => x.BusinessUserId,
                        principalSchema: "business",
                        principalTable: "BusinessUser",
                        principalColumn: "BusinessUserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubServiceAssignment_BusinessSubServiceId",
                schema: "business",
                table: "BusinessSubServiceAssignment",
                column: "BusinessSubServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubServiceAssignment_BusinessUserId",
                schema: "business",
                table: "BusinessSubServiceAssignment",
                column: "BusinessUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubServiceAssignment_BusinessSubServiceId_BusinessUserId",
                schema: "business",
                table: "BusinessSubServiceAssignment",
                columns: new[] { "BusinessSubServiceId", "BusinessUserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessSubServiceAssignment",
                schema: "business");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "business",
                table: "BusinessSubService");

            migrationBuilder.DropColumn(
                name: "Price",
                schema: "business",
                table: "BusinessSubService");
        }
    }
}
