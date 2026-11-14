using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261114053008_AddBookingPaymentDistributionTracking")]
public partial class AddBookingPaymentDistributionTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "StripeApplicationFeeId", schema: "job", table: "BookingPayment", type: "character varying(255)", maxLength: 255, nullable: true);
        migrationBuilder.AddColumn<string>(name: "StripeChargeId", schema: "job", table: "BookingPayment", type: "character varying(255)", maxLength: 255, nullable: true);
        migrationBuilder.AddColumn<string>(name: "StripeConnectedAccountId", schema: "job", table: "BookingPayment", type: "character varying(255)", maxLength: 255, nullable: true);
        migrationBuilder.AddColumn<string>(name: "StripeTransferId", schema: "job", table: "BookingPayment", type: "character varying(255)", maxLength: 255, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_BookingPayment_StripePaymentIntentId", schema: "job", table: "BookingPayment", column: "StripePaymentIntentId", unique: true, filter: "\"StripePaymentIntentId\" IS NOT NULL");
        migrationBuilder.CreateIndex(name: "IX_BookingPayment_StripeTransferId", schema: "job", table: "BookingPayment", column: "StripeTransferId", unique: true, filter: "\"StripeTransferId\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_BookingPayment_StripePaymentIntentId", schema: "job", table: "BookingPayment");
        migrationBuilder.DropIndex(name: "IX_BookingPayment_StripeTransferId", schema: "job", table: "BookingPayment");
        migrationBuilder.DropColumn(name: "StripeApplicationFeeId", schema: "job", table: "BookingPayment");
        migrationBuilder.DropColumn(name: "StripeChargeId", schema: "job", table: "BookingPayment");
        migrationBuilder.DropColumn(name: "StripeConnectedAccountId", schema: "job", table: "BookingPayment");
        migrationBuilder.DropColumn(name: "StripeTransferId", schema: "job", table: "BookingPayment");
    }
}
