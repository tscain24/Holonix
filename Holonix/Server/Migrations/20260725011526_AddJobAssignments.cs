using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddJobAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobAssignment",
                schema: "job",
                columns: table => new
                {
                    JobAssignmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobId = table.Column<long>(type: "bigint", nullable: false),
                    BusinessUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobAssignment", x => x.JobAssignmentId);
                    table.ForeignKey(
                        name: "FK_JobAssignment_BusinessUser_BusinessUserId",
                        column: x => x.BusinessUserId,
                        principalSchema: "business",
                        principalTable: "BusinessUser",
                        principalColumn: "BusinessUserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobAssignment_Job_JobId",
                        column: x => x.JobId,
                        principalSchema: "job",
                        principalTable: "Job",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignment_BusinessUserId",
                schema: "job",
                table: "JobAssignment",
                column: "BusinessUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobAssignment_JobId_BusinessUserId",
                schema: "job",
                table: "JobAssignment",
                columns: new[] { "JobId", "BusinessUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobAssignment",
                schema: "job");
        }
    }
}
