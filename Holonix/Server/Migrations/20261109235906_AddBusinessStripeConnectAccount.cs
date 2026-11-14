using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessStripeConnectAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessPaymentAccount",
                schema: "business",
                columns: table => new
                {
                    BusinessId = table.Column<int>(type: "integer", nullable: false),
                    StripeConnectedAccountId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TransfersCapabilityStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PayoutsCapabilityStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessPaymentAccount", x => x.BusinessId);
                    table.ForeignKey(
                        name: "FK_BusinessPaymentAccount_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "Business",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPaymentAccount_StripeConnectedAccountId",
                schema: "business",
                table: "BusinessPaymentAccount",
                column: "StripeConnectedAccountId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessPaymentAccount",
                schema: "business");
        }
    }
}
