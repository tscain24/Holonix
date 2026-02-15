using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class SeedNflMlbTeams : Migration
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
                    { 31, "Arizona", 1, "Cardinals" },
                    { 32, "Atlanta", 1, "Falcons" },
                    { 33, "Baltimore", 1, "Ravens" },
                    { 34, "Buffalo", 1, "Bills" },
                    { 35, "Carolina", 1, "Panthers" },
                    { 36, "Chicago", 1, "Bears" },
                    { 37, "Cincinnati", 1, "Bengals" },
                    { 38, "Cleveland", 1, "Browns" },
                    { 39, "Dallas", 1, "Cowboys" },
                    { 40, "Denver", 1, "Broncos" },
                    { 41, "Detroit", 1, "Lions" },
                    { 42, "Green Bay", 1, "Packers" },
                    { 43, "Houston", 1, "Texans" },
                    { 44, "Indianapolis", 1, "Colts" },
                    { 45, "Jacksonville", 1, "Jaguars" },
                    { 46, "Kansas City", 1, "Chiefs" },
                    { 47, "Las Vegas", 1, "Raiders" },
                    { 48, "Los Angeles", 1, "Chargers" },
                    { 49, "Los Angeles", 1, "Rams" },
                    { 50, "Miami", 1, "Dolphins" },
                    { 51, "Minnesota", 1, "Vikings" },
                    { 52, "New England", 1, "Patriots" },
                    { 53, "New Orleans", 1, "Saints" },
                    { 54, "New York", 1, "Giants" },
                    { 55, "New York", 1, "Jets" },
                    { 56, "Philadelphia", 1, "Eagles" },
                    { 57, "Pittsburgh", 1, "Steelers" },
                    { 58, "San Francisco", 1, "49ers" },
                    { 59, "Seattle", 1, "Seahawks" },
                    { 60, "Tampa Bay", 1, "Buccaneers" },
                    { 61, "Tennessee", 1, "Titans" },
                    { 62, "Washington", 1, "Commanders" },
                    { 63, "Arizona", 2, "Diamondbacks" },
                    { 64, "Atlanta", 2, "Braves" },
                    { 65, "Baltimore", 2, "Orioles" },
                    { 66, "Boston", 2, "Red Sox" },
                    { 67, "Chicago", 2, "Cubs" },
                    { 68, "Chicago", 2, "White Sox" },
                    { 69, "Cincinnati", 2, "Reds" },
                    { 70, "Cleveland", 2, "Guardians" },
                    { 71, "Colorado", 2, "Rockies" },
                    { 72, "Detroit", 2, "Tigers" },
                    { 73, "Houston", 2, "Astros" },
                    { 74, "Kansas City", 2, "Royals" },
                    { 75, "Los Angeles", 2, "Angels" },
                    { 76, "Los Angeles", 2, "Dodgers" },
                    { 77, "Miami", 2, "Marlins" },
                    { 78, "Milwaukee", 2, "Brewers" },
                    { 79, "Minnesota", 2, "Twins" },
                    { 80, "New York", 2, "Mets" },
                    { 81, "New York", 2, "Yankees" },
                    { 82, "Oakland", 2, "Athletics" },
                    { 83, "Philadelphia", 2, "Phillies" },
                    { 84, "Pittsburgh", 2, "Pirates" },
                    { 85, "San Diego", 2, "Padres" },
                    { 86, "San Francisco", 2, "Giants" },
                    { 87, "Seattle", 2, "Mariners" },
                    { 88, "St. Louis", 2, "Cardinals" },
                    { 89, "Tampa Bay", 2, "Rays" },
                    { 90, "Texas", 2, "Rangers" },
                    { 91, "Toronto", 2, "Blue Jays" },
                    { 92, "Washington", 2, "Nationals" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 31);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 32);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 33);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 34);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 35);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 36);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 37);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 38);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 39);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 40);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 41);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 42);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 43);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 44);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 45);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 46);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 47);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 48);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 49);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 50);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 51);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 52);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 53);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 54);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 55);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 56);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 57);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 58);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 59);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 60);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 61);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 62);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 63);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 64);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 65);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 66);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 67);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 68);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 69);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 70);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 71);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 72);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 73);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 74);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 75);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 76);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 77);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 78);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 79);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 80);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 81);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 82);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 83);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 84);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 85);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 86);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 87);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 88);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 89);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 90);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 91);

            migrationBuilder.DeleteData(
                schema: "sport",
                table: "Teams",
                keyColumn: "TeamId",
                keyValue: 92);
        }
    }
}

