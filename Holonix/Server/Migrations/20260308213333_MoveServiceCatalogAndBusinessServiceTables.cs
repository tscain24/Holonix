using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class MoveServiceCatalogAndBusinessServiceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "service");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessToService_BusinessService_ServiceId",
                schema: "business",
                table: "BusinessToService");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessToService_Business_BusinessId",
                schema: "business",
                table: "BusinessToService");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessService",
                schema: "business",
                table: "BusinessService");

            migrationBuilder.RenameTable(
                name: "BusinessService",
                schema: "business",
                newName: "Service",
                newSchema: "service");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Service",
                schema: "service",
                table: "Service",
                column: "ServiceId");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessToService",
                schema: "business",
                table: "BusinessToService");

            migrationBuilder.RenameTable(
                name: "BusinessToService",
                schema: "business",
                newName: "BusinessService",
                newSchema: "business");

            migrationBuilder.RenameColumn(
                name: "BusinessToServiceId",
                schema: "business",
                table: "BusinessService",
                newName: "BusinessServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_BusinessToService_ServiceId",
                schema: "business",
                table: "BusinessService",
                newName: "IX_BusinessService_ServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_BusinessToService_BusinessId",
                schema: "business",
                table: "BusinessService",
                newName: "IX_BusinessService_BusinessId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessService",
                schema: "business",
                table: "BusinessService",
                column: "BusinessServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessService_Business_BusinessId",
                schema: "business",
                table: "BusinessService",
                column: "BusinessId",
                principalSchema: "business",
                principalTable: "Business",
                principalColumn: "BusinessId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessService_Service_ServiceId",
                schema: "business",
                table: "BusinessService",
                column: "ServiceId",
                principalSchema: "service",
                principalTable: "Service",
                principalColumn: "ServiceId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessService_Business_BusinessId",
                schema: "business",
                table: "BusinessService");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessService_Service_ServiceId",
                schema: "business",
                table: "BusinessService");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessService",
                schema: "business",
                table: "BusinessService");

            migrationBuilder.RenameIndex(
                name: "IX_BusinessService_ServiceId",
                schema: "business",
                table: "BusinessService",
                newName: "IX_BusinessToService_ServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_BusinessService_BusinessId",
                schema: "business",
                table: "BusinessService",
                newName: "IX_BusinessToService_BusinessId");

            migrationBuilder.RenameColumn(
                name: "BusinessServiceId",
                schema: "business",
                table: "BusinessService",
                newName: "BusinessToServiceId");

            migrationBuilder.RenameTable(
                name: "BusinessService",
                schema: "business",
                newName: "BusinessToService",
                newSchema: "business");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessToService",
                schema: "business",
                table: "BusinessToService",
                column: "BusinessToServiceId");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Service",
                schema: "service",
                table: "Service");

            migrationBuilder.RenameTable(
                name: "Service",
                schema: "service",
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

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessToService_Business_BusinessId",
                schema: "business",
                table: "BusinessToService",
                column: "BusinessId",
                principalSchema: "business",
                principalTable: "Business",
                principalColumn: "BusinessId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
