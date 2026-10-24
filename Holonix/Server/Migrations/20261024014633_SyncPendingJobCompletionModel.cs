using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingJobCompletionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompletedByUserId",
                schema: "job",
                table: "Job",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedDateTime",
                schema: "job",
                table: "Job",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Job_CompletedByUserId",
                schema: "job",
                table: "Job",
                column: "CompletedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Job_Users_CompletedByUserId",
                schema: "job",
                table: "Job",
                column: "CompletedByUserId",
                principalSchema: "authentication",
                principalTable: "Users",
                principalColumn: "UsersId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Job_Users_CompletedByUserId",
                schema: "job",
                table: "Job");

            migrationBuilder.DropIndex(
                name: "IX_Job_CompletedByUserId",
                schema: "job",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "CompletedByUserId",
                schema: "job",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "CompletedDateTime",
                schema: "job",
                table: "Job");
        }
    }
}
