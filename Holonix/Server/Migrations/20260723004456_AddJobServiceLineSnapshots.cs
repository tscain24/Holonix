using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddJobServiceLineSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobServiceLine",
                schema: "job",
                columns: table => new
                {
                    JobServiceLineId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<long>(type: "bigint", nullable: false),
                    BusinessSubServiceId = table.Column<long>(type: "bigint", nullable: true),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobServiceLine", x => x.JobServiceLineId);
                    table.ForeignKey(
                        name: "FK_JobServiceLine_BusinessService_BusinessSubServiceId",
                        column: x => x.BusinessSubServiceId,
                        principalSchema: "business",
                        principalTable: "BusinessService",
                        principalColumn: "BusinessServiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobServiceLine_Job_JobId",
                        column: x => x.JobId,
                        principalSchema: "job",
                        principalTable: "Job",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobServiceLine_BusinessSubServiceId",
                schema: "job",
                table: "JobServiceLine",
                column: "BusinessSubServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_JobServiceLine_JobId_BusinessSubServiceId",
                schema: "job",
                table: "JobServiceLine",
                columns: new[] { "JobId", "BusinessSubServiceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobServiceLine",
                schema: "job");
        }
    }
}
