using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class RenameOwnerJobPercentageToBusinessJobPercentage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OwnerJobPercentage",
                schema: "business",
                table: "BusinessDetails",
                newName: "BusinessJobPercentage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BusinessJobPercentage",
                schema: "business",
                table: "BusinessDetails",
                newName: "OwnerJobPercentage");
        }
    }
}
