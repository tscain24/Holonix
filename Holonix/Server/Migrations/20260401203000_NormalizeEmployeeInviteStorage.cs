using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260401203000_NormalizeEmployeeInviteStorage")]
    public partial class NormalizeEmployeeInviteStorage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeInvite",
                schema: "business",
                columns: table => new
                {
                    EmployeeInviteId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    TargetEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedTargetEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TargetBusinessRoleId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeInvite", x => x.EmployeeInviteId);
                    table.ForeignKey(
                        name: "FK_EmployeeInvite_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "Business",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeInvite_BusinessRole_TargetBusinessRoleId",
                        column: x => x.TargetBusinessRoleId,
                        principalSchema: "business",
                        principalTable: "BusinessRole",
                        principalColumn: "BusinessRoleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeInvite_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalSchema: "authentication",
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeInviteWorkload",
                schema: "work",
                columns: table => new
                {
                    EmployeeInviteWorkloadId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkloadId = table.Column<long>(type: "bigint", nullable: false),
                    EmployeeInviteId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeInviteWorkload", x => x.EmployeeInviteWorkloadId);
                    table.ForeignKey(
                        name: "FK_EmployeeInviteWorkload_EmployeeInvite_EmployeeInviteId",
                        column: x => x.EmployeeInviteId,
                        principalSchema: "business",
                        principalTable: "EmployeeInvite",
                        principalColumn: "EmployeeInviteId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeInviteWorkload_Workload_WorkloadId",
                        column: x => x.WorkloadId,
                        principalSchema: "work",
                        principalTable: "Workload",
                        principalColumn: "WorkloadId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvite_BusinessId",
                schema: "business",
                table: "EmployeeInvite",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvite_NormalizedTargetEmail",
                schema: "business",
                table: "EmployeeInvite",
                column: "NormalizedTargetEmail");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvite_TargetBusinessRoleId",
                schema: "business",
                table: "EmployeeInvite",
                column: "TargetBusinessRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInvite_TargetUserId",
                schema: "business",
                table: "EmployeeInvite",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInviteWorkload_EmployeeInviteId",
                schema: "work",
                table: "EmployeeInviteWorkload",
                column: "EmployeeInviteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeInviteWorkload_WorkloadId",
                schema: "work",
                table: "EmployeeInviteWorkload",
                column: "WorkloadId",
                unique: true);

            migrationBuilder.Sql(@"
DECLARE @InviteMap TABLE (
    EmployeeInviteId BIGINT NOT NULL,
    WorkloadId BIGINT NOT NULL
);

MERGE [business].[EmployeeInvite] AS target
USING (
    SELECT
        w.[WorkloadId],
        w.[BusinessId],
        COALESCE(w.[TargetEmail], N'') AS [TargetEmail],
        COALESCE(w.[NormalizedTargetEmail], N'') AS [NormalizedTargetEmail],
        w.[TargetUserId],
        w.[TargetBusinessRoleId]
    FROM [work].[Workload] AS w
    INNER JOIN [work].[WorkloadType] AS wt ON w.[WorkloadTypeId] = wt.[WorkloadTypeId]
    WHERE wt.[Type] = N'Employee Invite'
) AS source
ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT ([BusinessId], [TargetEmail], [NormalizedTargetEmail], [TargetUserId], [TargetBusinessRoleId])
    VALUES (source.[BusinessId], source.[TargetEmail], source.[NormalizedTargetEmail], source.[TargetUserId], source.[TargetBusinessRoleId])
OUTPUT inserted.[EmployeeInviteId], source.[WorkloadId]
INTO @InviteMap ([EmployeeInviteId], [WorkloadId]);

INSERT INTO [work].[EmployeeInviteWorkload] ([WorkloadId], [EmployeeInviteId])
SELECT [WorkloadId], [EmployeeInviteId]
FROM @InviteMap;
");

            migrationBuilder.DropForeignKey(
                name: "FK_Workload_Business_BusinessId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropForeignKey(
                name: "FK_Workload_Users_TargetUserId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropForeignKey(
                name: "FK_Workload_BusinessRole_TargetBusinessRoleId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropIndex(
                name: "IX_Workload_BusinessId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropIndex(
                name: "IX_Workload_NormalizedTargetEmail",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropIndex(
                name: "IX_Workload_TargetBusinessRoleId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropIndex(
                name: "IX_Workload_TargetUserId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropColumn(
                name: "NormalizedTargetEmail",
                schema: "work",
                table: "Workload");

            migrationBuilder.DropColumn(
                name: "TargetBusinessRoleId",
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BusinessId",
                schema: "work",
                table: "Workload",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedTargetEmail",
                schema: "work",
                table: "Workload",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TargetBusinessRoleId",
                schema: "work",
                table: "Workload",
                type: "bigint",
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

            migrationBuilder.CreateIndex(
                name: "IX_Workload_BusinessId",
                schema: "work",
                table: "Workload",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Workload_NormalizedTargetEmail",
                schema: "work",
                table: "Workload",
                column: "NormalizedTargetEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Workload_TargetBusinessRoleId",
                schema: "work",
                table: "Workload",
                column: "TargetBusinessRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Workload_TargetUserId",
                schema: "work",
                table: "Workload",
                column: "TargetUserId");

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
                name: "FK_Workload_BusinessRole_TargetBusinessRoleId",
                schema: "work",
                table: "Workload",
                column: "TargetBusinessRoleId",
                principalSchema: "business",
                principalTable: "BusinessRole",
                principalColumn: "BusinessRoleId",
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

            migrationBuilder.Sql(@"
UPDATE w
SET
    w.[BusinessId] = ei.[BusinessId],
    w.[NormalizedTargetEmail] = ei.[NormalizedTargetEmail],
    w.[TargetBusinessRoleId] = ei.[TargetBusinessRoleId],
    w.[TargetEmail] = ei.[TargetEmail],
    w.[TargetUserId] = ei.[TargetUserId]
FROM [work].[Workload] AS w
INNER JOIN [work].[EmployeeInviteWorkload] AS eiw ON w.[WorkloadId] = eiw.[WorkloadId]
INNER JOIN [business].[EmployeeInvite] AS ei ON eiw.[EmployeeInviteId] = ei.[EmployeeInviteId];
");

            migrationBuilder.DropTable(
                name: "EmployeeInviteWorkload",
                schema: "work");

            migrationBuilder.DropTable(
                name: "EmployeeInvite",
                schema: "business");
        }
    }
}
