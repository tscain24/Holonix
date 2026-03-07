using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class RenameBusineessServiceToBusinessService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessToService_BusineessService_ServiceId",
                schema: "business",
                table: "BusinessToService");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusineessService",
                schema: "business",
                table: "BusineessService");

            migrationBuilder.RenameTable(
                name: "BusineessService",
                schema: "business",
                newName: "BusinessService",
                newSchema: "business");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessService",
                schema: "business",
                table: "BusinessService",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessToService_BusinessService_ServiceId",
                schema: "business",
                table: "BusinessToService",
                column: "ServiceId",
                principalSchema: "business",
                principalTable: "BusinessService",
                principalColumn: "ServiceId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessToService_BusinessService_ServiceId",
                schema: "business",
                table: "BusinessToService");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessService",
                schema: "business",
                table: "BusinessService");

            migrationBuilder.RenameTable(
                name: "BusinessService",
                schema: "business",
                newName: "BusineessService",
                newSchema: "business");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusineessService",
                schema: "business",
                table: "BusineessService",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessToService_BusineessService_ServiceId",
                schema: "business",
                table: "BusinessToService",
                column: "ServiceId",
                principalSchema: "business",
                principalTable: "BusineessService",
                principalColumn: "ServiceId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
