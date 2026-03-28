using System;
using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260328114500_AddInactiveDateToUsersAndFilterEmailUniqueness")]
    public partial class AddInactiveDateToUsersAndFilterEmailUniqueness : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InactiveDate",
                schema: "authentication",
                table: "Users",
                type: "datetime2",
                nullable: true);

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
                filter: "[NormalizedEmail] IS NOT NULL AND [InactiveDate] IS NULL");
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
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "InactiveDate",
                schema: "authentication",
                table: "Users");
        }
    }
}
