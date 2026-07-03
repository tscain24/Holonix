using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerBookingWorkloadsAndBookingPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingPayment",
                schema: "job",
                columns: table => new
                {
                    BookingPaymentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<long>(type: "bigint", nullable: false),
                    WorkloadId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentStatus = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    StripeCustomerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripeCheckoutSessionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripeSetupIntentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripePaymentMethodId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SetupCompletedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChargeScheduledForUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChargedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastPaymentFailureUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastPaymentFailureCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LastPaymentFailureMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingPayment", x => x.BookingPaymentId);
                    table.ForeignKey(
                        name: "FK_BookingPayment_Job_JobId",
                        column: x => x.JobId,
                        principalSchema: "job",
                        principalTable: "Job",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BookingPayment_Workload_WorkloadId",
                        column: x => x.WorkloadId,
                        principalSchema: "work",
                        principalTable: "Workload",
                        principalColumn: "WorkloadId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayment_JobId",
                schema: "job",
                table: "BookingPayment",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayment_StripeCheckoutSessionId",
                schema: "job",
                table: "BookingPayment",
                column: "StripeCheckoutSessionId",
                unique: true,
                filter: "\"StripeCheckoutSessionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BookingPayment_WorkloadId",
                schema: "job",
                table: "BookingPayment",
                column: "WorkloadId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingPayment",
                schema: "job");
        }
    }
}
