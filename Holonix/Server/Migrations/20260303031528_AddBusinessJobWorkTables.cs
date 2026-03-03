using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessJobWorkTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "business");

            migrationBuilder.EnsureSchema(
                name: "job");

            migrationBuilder.EnsureSchema(
                name: "Service");

            migrationBuilder.EnsureSchema(
                name: "work");

            migrationBuilder.CreateTable(
                name: "BusinessRole",
                schema: "business",
                columns: table => new
                {
                    BusinessRoleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HierarchyNumber = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessRole", x => x.BusinessRoleId);
                });

            migrationBuilder.CreateTable(
                name: "JobRecurrence",
                schema: "job",
                columns: table => new
                {
                    JobRecurrenceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartEffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Interval = table.Column<int>(type: "int", nullable: false),
                    FrequencyId = table.Column<byte>(type: "tinyint", nullable: false),
                    EndEffectiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRecurrence", x => x.JobRecurrenceId);
                });

            migrationBuilder.CreateTable(
                name: "Service",
                schema: "Service",
                columns: table => new
                {
                    ServiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InactiveDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Service", x => x.ServiceId);
                });

            migrationBuilder.CreateTable(
                name: "WorkloadType",
                schema: "work",
                columns: table => new
                {
                    WorkloadTypeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkloadType", x => x.WorkloadTypeId);
                });

            migrationBuilder.CreateTable(
                name: "JobRecurrenceException",
                schema: "job",
                columns: table => new
                {
                    JobRecurrenceExceptionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExceptionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OverrideStartDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OverrideEndDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JobRecurrenceId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRecurrenceException", x => x.JobRecurrenceExceptionId);
                    table.ForeignKey(
                        name: "FK_JobRecurrenceException_JobRecurrence_JobRecurrenceId",
                        column: x => x.JobRecurrenceId,
                        principalSchema: "job",
                        principalTable: "JobRecurrence",
                        principalColumn: "JobRecurrenceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Business",
                schema: "business",
                columns: table => new
                {
                    BusinessId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsSingleService = table.Column<bool>(type: "bit", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    BusinessIconBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerJobPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Business", x => x.BusinessId);
                    table.ForeignKey(
                        name: "FK_Business_Service_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "Service",
                        principalTable: "Service",
                        principalColumn: "ServiceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkloadStatus",
                schema: "work",
                columns: table => new
                {
                    WorkloadStatusId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkloadTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkloadStatus", x => x.WorkloadStatusId);
                    table.ForeignKey(
                        name: "FK_WorkloadStatus_WorkloadType_WorkloadTypeId",
                        column: x => x.WorkloadTypeId,
                        principalSchema: "work",
                        principalTable: "WorkloadType",
                        principalColumn: "WorkloadTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessUser",
                schema: "business",
                columns: table => new
                {
                    BusinessUserId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessUser", x => x.BusinessUserId);
                    table.ForeignKey(
                        name: "FK_BusinessUser_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "Business",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessUser_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "authentication",
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Workload",
                schema: "work",
                columns: table => new
                {
                    WorkloadId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WorkloadTypeId = table.Column<long>(type: "bigint", nullable: false),
                    WorkloadStatusId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workload", x => x.WorkloadId);
                    table.ForeignKey(
                        name: "FK_Workload_WorkloadStatus_WorkloadStatusId",
                        column: x => x.WorkloadStatusId,
                        principalSchema: "work",
                        principalTable: "WorkloadStatus",
                        principalColumn: "WorkloadStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Workload_WorkloadType_WorkloadTypeId",
                        column: x => x.WorkloadTypeId,
                        principalSchema: "work",
                        principalTable: "WorkloadType",
                        principalColumn: "WorkloadTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessUserAvailability",
                schema: "business",
                columns: table => new
                {
                    BusinessUserAvailabilityId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessUserId = table.Column<long>(type: "bigint", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EffectiveStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveEndDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessUserAvailability", x => x.BusinessUserAvailabilityId);
                    table.ForeignKey(
                        name: "FK_BusinessUserAvailability_BusinessUser_BusinessUserId",
                        column: x => x.BusinessUserId,
                        principalSchema: "business",
                        principalTable: "BusinessUser",
                        principalColumn: "BusinessUserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessUserRole",
                schema: "business",
                columns: table => new
                {
                    BusinessUserRoleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessUserId = table.Column<long>(type: "bigint", nullable: false),
                    BusinessRoleId = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessUserRole", x => x.BusinessUserRoleId);
                    table.ForeignKey(
                        name: "FK_BusinessUserRole_BusinessRole_BusinessRoleId",
                        column: x => x.BusinessRoleId,
                        principalSchema: "business",
                        principalTable: "BusinessRole",
                        principalColumn: "BusinessRoleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessUserRole_BusinessUser_BusinessUserId",
                        column: x => x.BusinessUserId,
                        principalSchema: "business",
                        principalTable: "BusinessUser",
                        principalColumn: "BusinessUserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessUserTimeOff",
                schema: "business",
                columns: table => new
                {
                    BusinessUserTimeOffId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessUserId = table.Column<long>(type: "bigint", nullable: false),
                    EffectiveStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EffectiveEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    IsAllDay = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessUserTimeOff", x => x.BusinessUserTimeOffId);
                    table.ForeignKey(
                        name: "FK_BusinessUserTimeOff_BusinessUser_BusinessUserId",
                        column: x => x.BusinessUserId,
                        principalSchema: "business",
                        principalTable: "BusinessUser",
                        principalColumn: "BusinessUserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Job",
                schema: "job",
                columns: table => new
                {
                    JobId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    BusinessId = table.Column<int>(type: "int", nullable: false),
                    BusinessUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsContract = table.Column<bool>(type: "bit", nullable: false),
                    JobRecurrenceId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job", x => x.JobId);
                    table.ForeignKey(
                        name: "FK_Job_BusinessUser_BusinessUserId",
                        column: x => x.BusinessUserId,
                        principalSchema: "business",
                        principalTable: "BusinessUser",
                        principalColumn: "BusinessUserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Job_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "Business",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Job_JobRecurrence_JobRecurrenceId",
                        column: x => x.JobRecurrenceId,
                        principalSchema: "job",
                        principalTable: "JobRecurrence",
                        principalColumn: "JobRecurrenceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Job_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "authentication",
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobWorkload",
                schema: "job",
                columns: table => new
                {
                    JobWorkloadId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<long>(type: "bigint", nullable: false),
                    WorkloadId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobWorkload", x => x.JobWorkloadId);
                    table.ForeignKey(
                        name: "FK_JobWorkload_Job_JobId",
                        column: x => x.JobId,
                        principalSchema: "job",
                        principalTable: "Job",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobWorkload_Workload_WorkloadId",
                        column: x => x.WorkloadId,
                        principalSchema: "work",
                        principalTable: "Workload",
                        principalColumn: "WorkloadId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Business_ServiceId",
                schema: "business",
                table: "Business",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUser_BusinessId",
                schema: "business",
                table: "BusinessUser",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUser_UserId",
                schema: "business",
                table: "BusinessUser",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUserAvailability_BusinessUserId",
                schema: "business",
                table: "BusinessUserAvailability",
                column: "BusinessUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUserRole_BusinessRoleId",
                schema: "business",
                table: "BusinessUserRole",
                column: "BusinessRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUserRole_BusinessUserId",
                schema: "business",
                table: "BusinessUserRole",
                column: "BusinessUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUserTimeOff_BusinessUserId",
                schema: "business",
                table: "BusinessUserTimeOff",
                column: "BusinessUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Job_BusinessId",
                schema: "job",
                table: "Job",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Job_BusinessUserId",
                schema: "job",
                table: "Job",
                column: "BusinessUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Job_JobRecurrenceId",
                schema: "job",
                table: "Job",
                column: "JobRecurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Job_UserId",
                schema: "job",
                table: "Job",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRecurrenceException_JobRecurrenceId",
                schema: "job",
                table: "JobRecurrenceException",
                column: "JobRecurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX_JobWorkload_JobId",
                schema: "job",
                table: "JobWorkload",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobWorkload_WorkloadId",
                schema: "job",
                table: "JobWorkload",
                column: "WorkloadId");

            migrationBuilder.CreateIndex(
                name: "IX_Workload_WorkloadStatusId",
                schema: "work",
                table: "Workload",
                column: "WorkloadStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Workload_WorkloadTypeId",
                schema: "work",
                table: "Workload",
                column: "WorkloadTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkloadStatus_WorkloadTypeId",
                schema: "work",
                table: "WorkloadStatus",
                column: "WorkloadTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessUserAvailability",
                schema: "business");

            migrationBuilder.DropTable(
                name: "BusinessUserRole",
                schema: "business");

            migrationBuilder.DropTable(
                name: "BusinessUserTimeOff",
                schema: "business");

            migrationBuilder.DropTable(
                name: "JobRecurrenceException",
                schema: "job");

            migrationBuilder.DropTable(
                name: "JobWorkload",
                schema: "job");

            migrationBuilder.DropTable(
                name: "BusinessRole",
                schema: "business");

            migrationBuilder.DropTable(
                name: "Job",
                schema: "job");

            migrationBuilder.DropTable(
                name: "Workload",
                schema: "work");

            migrationBuilder.DropTable(
                name: "BusinessUser",
                schema: "business");

            migrationBuilder.DropTable(
                name: "JobRecurrence",
                schema: "job");

            migrationBuilder.DropTable(
                name: "WorkloadStatus",
                schema: "work");

            migrationBuilder.DropTable(
                name: "Business",
                schema: "business");

            migrationBuilder.DropTable(
                name: "WorkloadType",
                schema: "work");

            migrationBuilder.DropTable(
                name: "Service",
                schema: "Service");
        }
    }
}
