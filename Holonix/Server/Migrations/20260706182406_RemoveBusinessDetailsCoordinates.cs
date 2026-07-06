using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBusinessDetailsCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                schema: "business",
                table: "BusinessDetails");

            migrationBuilder.DropColumn(
                name: "Longitude",
                schema: "business",
                table: "BusinessDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                schema: "business",
                table: "BusinessDetails",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                schema: "business",
                table: "BusinessDetails",
                type: "numeric(9,6)",
                nullable: true);
        }
    }
}
