using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessAddress",
                schema: "business",
                columns: table => new
                {
                    BusinessAddressId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessId = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    InactiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Address1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    State = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CountryId = table.Column<int>(type: "integer", nullable: true),
                    ZipCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessAddress", x => x.BusinessAddressId);
                    table.ForeignKey(
                        name: "FK_BusinessAddress_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "Business",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessAddress_Country_CountryId",
                        column: x => x.CountryId,
                        principalSchema: "reference",
                        principalTable: "Country",
                        principalColumn: "CountryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO business."BusinessAddress"
                    ("BusinessId", "IsPrimary", "InactiveDate", "Address1", "Address2", "City", "State", "CountryId", "ZipCode", "Latitude", "Longitude")
                SELECT
                    d."BusinessId",
                    TRUE,
                    NULL,
                    btrim(d."Address1"),
                    NULLIF(btrim(COALESCE(d."Address2", '')), ''),
                    btrim(d."City"),
                    btrim(d."State"),
                    d."CountryId",
                    btrim(d."ZipCode"),
                    d."Latitude",
                    d."Longitude"
                FROM business."BusinessDetails" d
                WHERE
                    d."Address1" IS NOT NULL AND btrim(d."Address1") <> '' AND
                    d."City" IS NOT NULL AND btrim(d."City") <> '' AND
                    d."State" IS NOT NULL AND btrim(d."State") <> '' AND
                    d."ZipCode" IS NOT NULL AND btrim(d."ZipCode") <> '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAddress_BusinessId",
                schema: "business",
                table: "BusinessAddress",
                column: "BusinessId",
                unique: true,
                filter: "\"IsPrimary\" = TRUE AND \"InactiveDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAddress_BusinessId_InactiveDate",
                schema: "business",
                table: "BusinessAddress",
                columns: new[] { "BusinessId", "InactiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAddress_CountryId",
                schema: "business",
                table: "BusinessAddress",
                column: "CountryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessAddress",
                schema: "business");
        }
    }
}
