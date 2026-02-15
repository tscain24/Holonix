using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sport");

            migrationBuilder.CreateTable(
                name: "Sports",
                schema: "sport",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sports", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "sport",
                table: "Sports",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "NFL" },
                    { 2, "MLB" },
                    { 3, "NBA" },
                    { 4, "MLS" },
                    { 5, "NCAA Football" },
                    { 6, "NCAA Basketball" },
                    { 7, "NCAA Baseball" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sports",
                schema: "sport");
        }
    }
}

