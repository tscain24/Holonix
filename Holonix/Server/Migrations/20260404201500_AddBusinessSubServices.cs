using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260404201500_AddBusinessSubServices")]
    public partial class AddBusinessSubServices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessSubService",
                schema: "business",
                columns: table => new
                {
                    BusinessSubServiceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InactiveDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSubService", x => x.BusinessSubServiceId);
                    table.ForeignKey(
                        name: "FK_BusinessSubService_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "Business",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessSubService_Service_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "service",
                        principalTable: "Service",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubService_BusinessId",
                schema: "business",
                table: "BusinessSubService",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubService_ServiceId",
                schema: "business",
                table: "BusinessSubService",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubService_BusinessId_ServiceId_Name",
                schema: "business",
                table: "BusinessSubService",
                columns: new[] { "BusinessId", "ServiceId", "Name" },
                unique: true,
                filter: "[InactiveDate] IS NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessSubService",
                schema: "business");
        }
    }
}
