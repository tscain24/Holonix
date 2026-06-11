using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSavedLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSavedLocation",
                schema: "authentication",
                columns: table => new
                {
                    UserSavedLocationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Address1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    State = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ZipCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InactiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSavedLocation", x => x.UserSavedLocationId);
                    table.ForeignKey(
                        name: "FK_UserSavedLocation_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "authentication",
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSavedLocation_UserId",
                schema: "authentication",
                table: "UserSavedLocation",
                column: "UserId",
                unique: true,
                filter: "\"IsPrimary\" = TRUE AND \"InactiveDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserSavedLocation_UserId_InactiveDate",
                schema: "authentication",
                table: "UserSavedLocation",
                columns: new[] { "UserId", "InactiveDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSavedLocation",
                schema: "authentication");
        }
    }
}
