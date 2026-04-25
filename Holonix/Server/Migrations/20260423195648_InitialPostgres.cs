using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "business");

            migrationBuilder.EnsureSchema(
                name: "service");

            migrationBuilder.EnsureSchema(
                name: "reference");

            migrationBuilder.EnsureSchema(
                name: "work");

            migrationBuilder.EnsureSchema(
                name: "job");

            migrationBuilder.EnsureSchema(
                name: "authentication");

            migrationBuilder.CreateTable(
                name: "Business",
                schema: "business",
                columns: table => new
                {
                    BusinessId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InactiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsProductBased = table.Column<bool>(type: "boolean", nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Business", x => x.BusinessId);
                });

            migrationBuilder.CreateTable(
                name: "BusinessRole",
                schema: "business",
                columns: table => new
                {
                    BusinessRoleId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HierarchyNumber = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessRole", x => x.BusinessRoleId);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                schema: "service",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InactiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Country",
                schema: "reference",
                columns: table => new
                {
                    CountryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TwoLetterIsoCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    ThreeLetterIsoCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Country", x => x.CountryId);
                });

            migrationBuilder.CreateTable(
                name: "JobRecurrence",
                schema: "job",
                columns: table => new
                {
                    JobRecurrenceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartEffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Interval = table.Column<int>(type: "integer", nullable: false),
                    FrequencyId = table.Column<byte>(type: "smallint", nullable: false),
                    EndEffectiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRecurrence", x => x.JobRecurrenceId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "authentication",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "authentication",
                columns: table => new
                {
                    UsersId = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    ProfileImageBase64 = table.Column<string>(type: "text", nullable: true),
                    InactiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UsersId);
                });

            migrationBuilder.CreateTable(
                name: "WorkloadType",
                schema: "work",
                columns: table => new
                {
                    WorkloadTypeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkloadType", x => x.WorkloadTypeId);
                });

            migrationBuilder.CreateTable(
                name: "BusinessCategory",
                schema: "business",
                columns: table => new
                {
                    BusinessCategoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessCategory", x => x.BusinessCategoryId);
                    table.ForeignKey(
                        name: "FK_BusinessCategory_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "Business",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessCategory_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "service",
                        principalTable: "Category",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessService",
                schema: "business",
                columns: table => new
                {
                    BusinessServiceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ConsultationNeeded = table.Column<bool>(type: "boolean", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EmployeeCount = table.Column<int>(type: "integer", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InactiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessService", x => x.BusinessServiceId);
                    table.ForeignKey(
                        name: "FK_BusinessService_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "Business",
                        principalColumn: "BusinessId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessService_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "service",
                        principalTable: "Category",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessDetails",
                schema: "business",
                columns: table => new
                {
                    BusinessId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BusinessEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BusinessPhoneNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Address1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Address2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    State = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CountryId = table.Column<int>(type: "integer", nullable: true),
                    ZipCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    BusinessIconBase64 = table.Column<string>(type: "text", nullable: true),
                    BusinessJobPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_BusinessDetails_Country_CountryId",
                        column: x => x.CountryId,
                        principalSchema: "reference",
                        principalTable: "Country",
                        principalColumn: "CountryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobRecurrenceException",
                schema: "job",
                columns: table => new
                {
                    JobRecurrenceExceptionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExceptionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OverrideStartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OverrideEndDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                name: "RoleClaims",
                schema: "authentication",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "authentication",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessUser",
                schema: "business",
                columns: table => new
                {
                    BusinessUserId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ReportsToUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                        name: "FK_BusinessUser_Users_ReportsToUserId",
                        column: x => x.ReportsToUserId,
                        principalSchema: "authentication",
                        principalTable: "Users",
                        principalColumn: "UsersId",
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
                name: "EmployeeInvite",
                schema: "business",
                columns: table => new
                {
                    EmployeeInviteId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessId = table.Column<int>(type: "integer", nullable: false),
                    TargetEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedTargetEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TargetUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    TargetBusinessRoleId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeInvite", x => x.EmployeeInviteId);
                    table.ForeignKey(
                        name: "FK_EmployeeInvite_BusinessRole_TargetBusinessRoleId",
                        column: x => x.TargetBusinessRoleId,
                        principalSchema: "business",
                        principalTable: "BusinessRole",
                        principalColumn: "BusinessRoleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeInvite_Business_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "business",
                        principalTable: "Business",
                        principalColumn: "BusinessId",
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
                name: "UserClaims",
                schema: "authentication",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "authentication",
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                schema: "authentication",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "authentication",
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "authentication",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "authentication",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "authentication",
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                schema: "authentication",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "authentication",
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkloadStatus",
                schema: "work",
                columns: table => new
                {
                    WorkloadStatusId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkloadTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkloadStatus", x => x.WorkloadStatusId);
                    table.UniqueConstraint("AK_WorkloadStatus_WorkloadStatusId_WorkloadTypeId", x => new { x.WorkloadStatusId, x.WorkloadTypeId });
                    table.ForeignKey(
                        name: "FK_WorkloadStatus_WorkloadType_WorkloadTypeId",
                        column: x => x.WorkloadTypeId,
                        principalSchema: "work",
                        principalTable: "WorkloadType",
                        principalColumn: "WorkloadTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessServiceAssignment",
                schema: "business",
                columns: table => new
                {
                    BusinessServiceAssignmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessServiceId = table.Column<long>(type: "bigint", nullable: false),
                    BusinessUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessServiceAssignment", x => x.BusinessServiceAssignmentId);
                    table.ForeignKey(
                        name: "FK_BusinessServiceAssignment_BusinessService_BusinessServiceId",
                        column: x => x.BusinessServiceId,
                        principalSchema: "business",
                        principalTable: "BusinessService",
                        principalColumn: "BusinessServiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessServiceAssignment_BusinessUser_BusinessUserId",
                        column: x => x.BusinessUserId,
                        principalSchema: "business",
                        principalTable: "BusinessUser",
                        principalColumn: "BusinessUserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessUserAvailability",
                schema: "business",
                columns: table => new
                {
                    BusinessUserAvailabilityId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessUserId = table.Column<long>(type: "bigint", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "smallint", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessUserId = table.Column<long>(type: "bigint", nullable: false),
                    BusinessRoleId = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessUserId = table.Column<long>(type: "bigint", nullable: false),
                    EffectiveStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EffectiveEndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    IsAllDay = table.Column<bool>(type: "boolean", nullable: true)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    BusinessId = table.Column<int>(type: "integer", nullable: false),
                    BusinessUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsContract = table.Column<bool>(type: "boolean", nullable: false),
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
                name: "Workload",
                schema: "work",
                columns: table => new
                {
                    WorkloadId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    WorkloadTypeId = table.Column<long>(type: "bigint", nullable: false),
                    WorkloadStatusId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workload", x => x.WorkloadId);
                    table.ForeignKey(
                        name: "FK_Workload_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "authentication",
                        principalTable: "Users",
                        principalColumn: "UsersId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Workload_WorkloadStatus_WorkloadStatusId_WorkloadTypeId",
                        columns: x => new { x.WorkloadStatusId, x.WorkloadTypeId },
                        principalSchema: "work",
                        principalTable: "WorkloadStatus",
                        principalColumns: new[] { "WorkloadStatusId", "WorkloadTypeId" },
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
                name: "EmployeeInviteWorkload",
                schema: "work",
                columns: table => new
                {
                    EmployeeInviteWorkloadId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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

            migrationBuilder.CreateTable(
                name: "JobWorkload",
                schema: "job",
                columns: table => new
                {
                    JobWorkloadId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                name: "IX_Business_BusinessCode",
                schema: "business",
                table: "Business",
                column: "BusinessCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCategory_BusinessId",
                schema: "business",
                table: "BusinessCategory",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCategory_CategoryId",
                schema: "business",
                table: "BusinessCategory",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDetails_CountryId",
                schema: "business",
                table: "BusinessDetails",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRole_Name",
                schema: "business",
                table: "BusinessRole",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessService_BusinessId_CategoryId_Name",
                schema: "business",
                table: "BusinessService",
                columns: new[] { "BusinessId", "CategoryId", "Name" },
                unique: true,
                filter: "\"InactiveDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessService_CategoryId",
                schema: "business",
                table: "BusinessService",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessServiceAssignment_BusinessServiceId_BusinessUserId",
                schema: "business",
                table: "BusinessServiceAssignment",
                columns: new[] { "BusinessServiceId", "BusinessUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessServiceAssignment_BusinessUserId",
                schema: "business",
                table: "BusinessServiceAssignment",
                column: "BusinessUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUser_BusinessId",
                schema: "business",
                table: "BusinessUser",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessUser_ReportsToUserId",
                schema: "business",
                table: "BusinessUser",
                column: "ReportsToUserId");

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
                name: "IX_Country_Name",
                schema: "reference",
                table: "Country",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Country_ThreeLetterIsoCode",
                schema: "reference",
                table: "Country",
                column: "ThreeLetterIsoCode",
                unique: true,
                filter: "\"ThreeLetterIsoCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Country_TwoLetterIsoCode",
                schema: "reference",
                table: "Country",
                column: "TwoLetterIsoCode",
                unique: true);

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
                name: "IX_RoleClaims_RoleId",
                schema: "authentication",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "authentication",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                schema: "authentication",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                schema: "authentication",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "authentication",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "authentication",
                table: "Users",
                column: "NormalizedEmail",
                unique: true,
                filter: "\"NormalizedEmail\" IS NOT NULL AND \"InactiveDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "authentication",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workload_CreatedByUserId",
                schema: "work",
                table: "Workload",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Workload_WorkloadStatusId_WorkloadTypeId",
                schema: "work",
                table: "Workload",
                columns: new[] { "WorkloadStatusId", "WorkloadTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Workload_WorkloadTypeId",
                schema: "work",
                table: "Workload",
                column: "WorkloadTypeId");

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
                name: "IX_WorkloadType_Type",
                schema: "work",
                table: "WorkloadType",
                column: "Type",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessCategory",
                schema: "business");

            migrationBuilder.DropTable(
                name: "BusinessDetails",
                schema: "business");

            migrationBuilder.DropTable(
                name: "BusinessServiceAssignment",
                schema: "business");

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
                name: "EmployeeInviteWorkload",
                schema: "work");

            migrationBuilder.DropTable(
                name: "JobRecurrenceException",
                schema: "job");

            migrationBuilder.DropTable(
                name: "JobWorkload",
                schema: "job");

            migrationBuilder.DropTable(
                name: "RoleClaims",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "UserClaims",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "UserLogins",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "UserTokens",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "Country",
                schema: "reference");

            migrationBuilder.DropTable(
                name: "BusinessService",
                schema: "business");

            migrationBuilder.DropTable(
                name: "EmployeeInvite",
                schema: "business");

            migrationBuilder.DropTable(
                name: "Job",
                schema: "job");

            migrationBuilder.DropTable(
                name: "Workload",
                schema: "work");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "Category",
                schema: "service");

            migrationBuilder.DropTable(
                name: "BusinessRole",
                schema: "business");

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
                name: "Users",
                schema: "authentication");

            migrationBuilder.DropTable(
                name: "WorkloadType",
                schema: "work");
        }
    }
}
