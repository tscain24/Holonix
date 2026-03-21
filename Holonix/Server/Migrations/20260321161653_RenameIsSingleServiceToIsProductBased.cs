using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class RenameIsSingleServiceToIsProductBased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsSingleService",
                schema: "business",
                table: "Business",
                newName: "IsProductBased");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsProductBased",
                schema: "business",
                table: "Business",
                newName: "IsSingleService");
        }
    }
}
