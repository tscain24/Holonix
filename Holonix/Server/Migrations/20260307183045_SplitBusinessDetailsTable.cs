using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class SplitBusinessDetailsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessDetails",
                schema: "business",
                columns: table => new
                {
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    State = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    BusinessIconBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerJobPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessDetails", x => x.BusinessId);
                    table.ForeignKey(
                        name: "FK_BusinessDetails_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "Business",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [business].[BusinessDetails] (
                    [BusinessId],
                    [Description],
                    [Address1],
                    [Address2],
                    [City],
                    [State],
                    [Country],
                    [ZipCode],
                    [BusinessIconBase64],
                    [OwnerJobPercentage]
                )
                SELECT
                    [BusinessId],
                    [Description],
                    [Address1],
                    [Address2],
                    [City],
                    [State],
                    [Country],
                    [ZipCode],
                    [BusinessIconBase64],
                    [OwnerJobPercentage]
                FROM [business].[Business];
                """);

            migrationBuilder.DropColumn(
                name: "Address1",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "Address2",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "BusinessIconBase64",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "OwnerJobPercentage",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "State",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                schema: "business",
                table: "Business");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address1",
                schema: "business",
                table: "Business",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address2",
                schema: "business",
                table: "Business",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessIconBase64",
                schema: "business",
                table: "Business",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "business",
                table: "Business",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "business",
                table: "Business",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "business",
                table: "Business",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OwnerJobPercentage",
                schema: "business",
                table: "Business",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "State",
                schema: "business",
                table: "Business",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                schema: "business",
                table: "Business",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE b
                SET
                    b.[Description] = d.[Description],
                    b.[Address1] = d.[Address1],
                    b.[Address2] = d.[Address2],
                    b.[City] = d.[City],
                    b.[State] = d.[State],
                    b.[Country] = d.[Country],
                    b.[ZipCode] = d.[ZipCode],
                    b.[BusinessIconBase64] = d.[BusinessIconBase64],
                    b.[OwnerJobPercentage] = d.[OwnerJobPercentage]
                FROM [business].[Business] b
                INNER JOIN [business].[BusinessDetails] d
                    ON d.[BusinessId] = b.[BusinessId];
                """);

            migrationBuilder.DropTable(
                name: "BusinessDetails",
                schema: "business");
        }
    }
}
