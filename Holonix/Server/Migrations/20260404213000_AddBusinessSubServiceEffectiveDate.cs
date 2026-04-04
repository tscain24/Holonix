using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260404213000_AddBusinessSubServiceEffectiveDate")]
    public partial class AddBusinessSubServiceEffectiveDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveDate",
                schema: "business",
                table: "BusinessSubService",
                type: "date",
                nullable: false,
                defaultValueSql: "CAST(GETUTCDATE() AS date)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EffectiveDate",
                schema: "business",
                table: "BusinessSubService");
        }
    }
}
