using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddJobServiceLocationSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServiceAddress1",
                schema: "job",
                table: "Job",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceAddress2",
                schema: "job",
                table: "Job",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceCity",
                schema: "job",
                table: "Job",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceLatitude",
                schema: "job",
                table: "Job",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceLocationLabel",
                schema: "job",
                table: "Job",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceLongitude",
                schema: "job",
                table: "Job",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceState",
                schema: "job",
                table: "Job",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceZipCode",
                schema: "job",
                table: "Job",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceAddress1",
                schema: "job",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "ServiceAddress2",
                schema: "job",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "ServiceCity",
                schema: "job",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "ServiceLatitude",
                schema: "job",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "ServiceLocationLabel",
                schema: "job",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "ServiceLongitude",
                schema: "job",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "ServiceState",
                schema: "job",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "ServiceZipCode",
                schema: "job",
                table: "Job");
        }
    }
}
