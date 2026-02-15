using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class SeedNbaTeams : Migration
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
                    { 1, "Atlanta", 3, "Hawks" },
                    { 2, "Boston", 3, "Celtics" },
                    { 3, "Brooklyn", 3, "Nets" },
                    { 4, "Charlotte", 3, "Hornets" },
                    { 5, "Chicago", 3, "Bulls" },
                    { 6, "Cleveland", 3, "Cavaliers" },
                    { 7, "Dallas", 3, "Mavericks" },
                    { 8, "Denver", 3, "Nuggets" },
                    { 9, "Detroit", 3, "Pistons" },
                    { 10, "Golden State", 3, "Warriors" },
                    { 11, "Houston", 3, "Rockets" },
                    { 12, "Indiana", 3, "Pacers" },
                    { 13, "Los Angeles", 3, "Clippers" },
                    { 14, "Los Angeles", 3, "Lakers" },
                    { 15, "Memphis", 3, "Grizzlies" },
                    { 16, "Miami", 3, "Heat" },
                    { 17, "Milwaukee", 3, "Bucks" },
                    { 18, "Minnesota", 3, "Timberwolves" },
                    { 19, "New Orleans", 3, "Pelicans" },
                    { 20, "New York", 3, "Knicks" },
                    { 21, "Oklahoma City", 3, "Thunder" },
                    { 22, "Orlando", 3, "Magic" },
                    { 23, "Philadelphia", 3, "76ers" },
                    { 24, "Phoenix", 3, "Suns" },
                    { 25, "Portland", 3, "Trail Blazers" },
                    { 26, "Sacramento", 3, "Kings" },
                    { 27, "San Antonio", 3, "Spurs" },
                    { 28, "Toronto", 3, "Raptors" },
                    { 29, "Utah", 3, "Jazz" },
                    { 30, "Washington", 3, "Wizards" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 6);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 7);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 13);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 14);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 15);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 16);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 17);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 18);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 19);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 20);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 21);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 22);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 23);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 24);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 25);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 26);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 27);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 28);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 29);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 30);
        }
    }
}

