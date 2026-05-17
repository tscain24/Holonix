using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessHoursJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessHoursJson",
                schema: "business",
                table: "BusinessDetails",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessHoursJson",
                schema: "business",
                table: "BusinessDetails");
        }
    }
}

