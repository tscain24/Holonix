using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSportsAndTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[sport].[Teams]', N'U') IS NOT NULL
                    DROP TABLE [sport].[Teams];
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[sport].[Sports]', N'U') IS NOT NULL
                    DROP TABLE [sport].[Sports];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sport");

            migrationBuilder.CreateTable(
                name: "Sports",
                schema: "sport",
                columns: table => new
                {
                    SportsId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sports", x => x.SportsId);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                schema: "sport",
                columns: table => new
                {
                    TeamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SportId = table.Column<int>(type: "int", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TeamName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.TeamId);
                    table.ForeignKey(
                        name: "FK_Teams_Sports_SportId",
                        column: x => x.SportId,
                        principalSchema: "sport",
                        principalTable: "Sports",
                        principalColumn: "SportsId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "sport",
                table: "Sports",
                columns: new[] { "SportsId", "Name" },
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
                    { 30, "Washington", 3, "Wizards" },
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
                    { 92, "Washington", 2, "Nationals" },
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

            migrationBuilder.CreateIndex(
                name: "IX_Teams_SportId",
                schema: "sport",
                table: "Teams",
                column: "SportId");
        }
    }
}
