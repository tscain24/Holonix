using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorySearchText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            migrationBuilder.AddColumn<string>(
                name: "SearchText",
                schema: "service",
                table: "Category",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Embedding",
                schema: "service",
                table: "Category",
                type: "vector(1536)",
                nullable: true);

            migrationBuilder.Sql("UPDATE service.\"Category\" SET \"SearchText\" = \"Name\" WHERE \"SearchText\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Embedding",
                schema: "service",
                table: "Category");

            migrationBuilder.DropColumn(
                name: "SearchText",
                schema: "service",
                table: "Category");
        }
    }
}
