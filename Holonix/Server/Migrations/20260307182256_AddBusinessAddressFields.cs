using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address1",
                schema: "business",
                table: "Business",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address2",
                schema: "business",
                table: "Business",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "business",
                table: "Business",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "business",
                table: "Business",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                schema: "business",
                table: "Business",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                schema: "business",
                table: "Business",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address1",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "Address2",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "State",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                schema: "business",
                table: "Business");
        }
    }
}
