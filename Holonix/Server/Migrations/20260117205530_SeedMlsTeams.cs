using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class SeedMlsTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "sport",
                table: "Teams",
                columns: new[] { "TeamId", "LocationName", "SportId", "TeamName" },
                values: new object[,]
                {
                    { 93, "Atlanta", 4, "United" },
                    { 94, "Austin", 4, "FC" },
                    { 95, "Charlotte", 4, "FC" },
                    { 96, "Chicago", 4, "Fire" },
                    { 97, "Cincinnati", 4, "FC" },
                    { 98, "Colorado", 4, "Rapids" },
                    { 99, "Columbus", 4, "Crew" },
                    { 100, "Dallas", 4, "FC" },
                    { 101, "D.C. United", 4, "FC" },
                    { 102, "Houston", 4, "Dynamo" },
                    { 103, "Kansas City", 4, "Sporting" },
                    { 104, "Los Angeles", 4, "Galaxy" },
                    { 105, "Los Angeles", 4, "FC" },
                    { 106, "Miami", 4, "Inter" },
                    { 107, "Minnesota", 4, "United" },
                    { 108, "Montreal", 4, "CF" },
                    { 109, "Nashville", 4, "SC" },
                    { 110, "New England", 4, "Revolution" },
                    { 111, "New York City", 4, "FC" },
                    { 112, "New York", 4, "Red Bulls" },
                    { 113, "Orlando", 4, "City" },
                    { 114, "Philadelphia", 4, "Union" },
                    { 115, "Portland", 4, "Timbers" },
                    { 116, "Salt Lake", 4, "Real" },
                    { 117, "San Jose", 4, "Earthquakes" },
                    { 118, "Seattle", 4, "Sounders" },
                    { 119, "St. Louis", 4, "City" },
                    { 120, "Toronto", 4, "FC" },
                    { 121, "Vancouver", 4, "Whitecaps" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 93);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 94);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 95);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 96);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 97);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 98);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 99);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 100);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 101);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 102);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 103);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 104);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 105);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 106);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 107);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 108);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 109);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 110);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 111);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 112);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 113);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 114);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 115);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 116);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 117);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 118);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 119);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 120);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 121);
        }
    }
}

