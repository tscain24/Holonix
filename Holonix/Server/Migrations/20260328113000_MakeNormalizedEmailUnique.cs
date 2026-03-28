using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260328113000_MakeNormalizedEmailUnique")]
    public partial class MakeNormalizedEmailUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                schema: "authentication",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "authentication",
                table: "Users",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                schema: "authentication",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "authentication",
                table: "Users",
                column: "NormalizedEmail");
        }
    }
}
