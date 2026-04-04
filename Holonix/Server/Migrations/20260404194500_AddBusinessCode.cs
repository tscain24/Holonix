using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260404194500_AddBusinessCode")]
    public partial class AddBusinessCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessCode",
                schema: "business",
                table: "Business",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.Sql(@"
WHILE EXISTS (
    SELECT 1
    FROM [business].[Business]
    WHERE [BusinessCode] IS NULL
)
BEGIN
    UPDATE TOP (1000) b
    SET [BusinessCode] = LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 10)
    FROM [business].[Business] AS b
    WHERE b.[BusinessCode] IS NULL;
END;

WHILE EXISTS (
    SELECT [BusinessCode]
    FROM [business].[Business]
    WHERE [BusinessCode] IS NOT NULL
    GROUP BY [BusinessCode]
    HAVING COUNT(*) > 1
)
BEGIN
    WITH DuplicateCodes AS (
        SELECT
            [BusinessId],
            ROW_NUMBER() OVER (PARTITION BY [BusinessCode] ORDER BY [BusinessId]) AS [RowNumber]
        FROM [business].[Business]
        WHERE [BusinessCode] IS NOT NULL
    )
    UPDATE b
    SET [BusinessCode] = LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 10)
    FROM [business].[Business] AS b
    INNER JOIN DuplicateCodes AS d ON d.[BusinessId] = b.[BusinessId]
    WHERE d.[RowNumber] > 1;
END;
");

            migrationBuilder.AlterColumn<string>(
                name: "BusinessCode",
                schema: "business",
                table: "Business",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Business_BusinessCode",
                schema: "business",
                table: "Business",
                column: "BusinessCode",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Business_BusinessCode",
                schema: "business",
                table: "Business");

            migrationBuilder.DropColumn(
                name: "BusinessCode",
                schema: "business",
                table: "Business");
        }
    }
}
