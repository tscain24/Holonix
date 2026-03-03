using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class MoveBusinessServiceToBusinessSchemaAndAddLinkTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Business_Service_ServiceId",
                schema: "business",
                table: "Business");

            migrationBuilder.DropIndex(
                name: "IX_Business_ServiceId",
                schema: "business",
                table: "Business");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Service",
                schema: "Service",
                table: "Service");

            migrationBuilder.RenameTable(
                name: "Service",
                schema: "Service",
                newName: "BusineessService",
                newSchema: "business");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusineessService",
                schema: "business",
                table: "BusineessService",
                column: "ServiceId");

            migrationBuilder.CreateTable(
                name: "BusinessToService",
                schema: "business",
                columns: table => new
                {
                    BusinessToServiceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessToService", x => x.BusinessToServiceId);
                    table.ForeignKey(
                        name: "FK_BusinessToService_BusineessService_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "business",
                        principalTable: "BusineessService",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessToService_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "Business",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessToService_BusinessId",
                schema: "business",
                table: "BusinessToService",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessToService_ServiceId",
                schema: "business",
                table: "BusinessToService",
                column: "ServiceId");

            migrationBuilder.Sql("""
                INSERT INTO [business].[BusinessToService] ([BusinessId], [ServiceId])
                SELECT [BusinessId], [ServiceId]
                FROM [business].[Business]
                WHERE [ServiceId] > 0;
                """);

            migrationBuilder.DropColumn(
                name: "ServiceId",
                schema: "business",
                table: "Business");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessToService",
                schema: "business");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusineessService",
                schema: "business",
                table: "BusineessService");

            migrationBuilder.EnsureSchema(
                name: "Service");

            migrationBuilder.RenameTable(
                name: "BusineessService",
                schema: "business",
                newName: "Service",
                newSchema: "Service");

            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                schema: "business",
                table: "Business",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Service",
                schema: "Service",
                table: "Service",
                column: "ServiceId");

            migrationBuilder.Sql("""
                UPDATE b
                SET b.[ServiceId] = x.[ServiceId]
                FROM [business].[Business] b
                OUTER APPLY (
                    SELECT TOP(1) bs.[ServiceId]
                    FROM [business].[BusinessToService] bs
                    WHERE bs.[BusinessId] = b.[BusinessId]
                    ORDER BY bs.[BusinessToServiceId]
                ) x;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Business_ServiceId",
                schema: "business",
                table: "Business",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Business_Service_ServiceId",
                schema: "business",
                table: "Business",
                column: "ServiceId",
                principalSchema: "Service",
                principalTable: "Service",
                principalColumn: "ServiceId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
