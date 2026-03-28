using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeInviteWorkloads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workload_WorkloadStatus_WorkloadStatusId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropIndex(
                name: "IX_WorkloadStatus_WorkloadTypeId",
                schema: "work",
                table: "WorkloadStatus");

            migrationBuilder.DropIndex(
                name: "IX_Workload_WorkloadStatusId",
                schema: "work",
                table: "Workload");

            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                schema: "work",
                table: "Workload",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                schema: "work",
                table: "Workload",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedTargetEmail",
                schema: "work",
                table: "Workload",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetEmail",
                schema: "work",
                table: "Workload",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetUserId",
                schema: "work",
                table: "Workload",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_WorkloadStatus_WorkloadStatusId_WorkloadTypeId",
                schema: "work",
                table: "WorkloadStatus",
                columns: new[] { "WorkloadStatusId", "WorkloadTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkloadType_Type",
                schema: "work",
                table: "WorkloadType",
                column: "Type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkloadStatus_WorkloadStatusId_WorkloadTypeId",
                schema: "work",
                table: "WorkloadStatus",
                columns: new[] { "WorkloadStatusId", "WorkloadTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkloadStatus_WorkloadTypeId_Status",
                schema: "work",
                table: "WorkloadStatus",
                columns: new[] { "WorkloadTypeId", "Status" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workload_BusinessId",
                schema: "work",
                table: "Workload",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Workload_CreatedByUserId",
                schema: "work",
                table: "Workload",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Workload_NormalizedTargetEmail",
                schema: "work",
                table: "Workload",
                column: "NormalizedTargetEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Workload_TargetUserId",
                schema: "work",
                table: "Workload",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Workload_WorkloadStatusId_WorkloadTypeId",
                schema: "work",
                table: "Workload",
                columns: new[] { "WorkloadStatusId", "WorkloadTypeId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Workload_Business_BusinessId",
                schema: "work",
                table: "Workload",
                column: "BusinessId",
                principalSchema: "business",
                principalTable: "Business",
                principalColumn: "BusinessId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Workload_Users_CreatedByUserId",
                schema: "work",
                table: "Workload",
                column: "CreatedByUserId",
                principalSchema: "authentication",
                principalTable: "Users",
                principalColumn: "UsersId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Workload_Users_TargetUserId",
                schema: "work",
                table: "Workload",
                column: "TargetUserId",
                principalSchema: "authentication",
                principalTable: "Users",
                principalColumn: "UsersId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Workload_WorkloadStatus_WorkloadStatusId_WorkloadTypeId",
                schema: "work",
                table: "Workload",
                columns: new[] { "WorkloadStatusId", "WorkloadTypeId" },
                principalSchema: "work",
                principalTable: "WorkloadStatus",
                principalColumns: new[] { "WorkloadStatusId", "WorkloadTypeId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workload_Business_BusinessId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropForeignKey(
                name: "FK_Workload_Users_CreatedByUserId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropForeignKey(
                name: "FK_Workload_Users_TargetUserId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropForeignKey(
                name: "FK_Workload_WorkloadStatus_WorkloadStatusId_WorkloadTypeId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropIndex(
                name: "IX_WorkloadType_Type",
                schema: "work",
                table: "WorkloadType");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_WorkloadStatus_WorkloadStatusId_WorkloadTypeId",
                schema: "work",
                table: "WorkloadStatus");

            migrationBuilder.DropIndex(
                name: "IX_WorkloadStatus_WorkloadStatusId_WorkloadTypeId",
                schema: "work",
                table: "WorkloadStatus");

            migrationBuilder.DropIndex(
                name: "IX_WorkloadStatus_WorkloadTypeId_Status",
                schema: "work",
                table: "WorkloadStatus");

            migrationBuilder.DropIndex(
                name: "IX_Workload_BusinessId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropIndex(
                name: "IX_Workload_CreatedByUserId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropIndex(
                name: "IX_Workload_NormalizedTargetEmail",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropIndex(
                name: "IX_Workload_TargetUserId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropIndex(
                name: "IX_Workload_WorkloadStatusId_WorkloadTypeId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropColumn(
                name: "NormalizedTargetEmail",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropColumn(
                name: "TargetEmail",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropColumn(
                name: "TargetUserId",
                schema: "work",
                table: "Workload");

            migrationBuilder.CreateIndex(
                name: "IX_WorkloadStatus_WorkloadTypeId",
                schema: "work",
                table: "WorkloadStatus",
                column: "WorkloadTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Workload_WorkloadStatusId",
                schema: "work",
                table: "Workload",
                column: "WorkloadStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workload_WorkloadStatus_WorkloadStatusId",
                schema: "work",
                table: "Workload",
                column: "WorkloadStatusId",
                principalSchema: "work",
                principalTable: "WorkloadStatus",
                principalColumn: "WorkloadStatusId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
