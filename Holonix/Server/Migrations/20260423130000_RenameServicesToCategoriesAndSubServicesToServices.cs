using Holonix.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260423130000_RenameServicesToCategoriesAndSubServicesToServices")]
public partial class RenameServicesToCategoriesAndSubServicesToServices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_BusinessService_Service_ServiceId",
            schema: "business",
            table: "BusinessService");

        migrationBuilder.DropForeignKey(
            name: "FK_BusinessSubService_Service_ServiceId",
            schema: "business",
            table: "BusinessSubService");

        migrationBuilder.DropForeignKey(
            name: "FK_BusinessSubServiceAssignment_BusinessSubService_BusinessSubServiceId",
            schema: "business",
            table: "BusinessSubServiceAssignment");

        migrationBuilder.DropPrimaryKey(
            name: "PK_Service",
            schema: "service",
            table: "Service");

        migrationBuilder.RenameTable(
            name: "Service",
            schema: "service",
            newName: "Category",
            newSchema: "service");

        migrationBuilder.RenameColumn(
            name: "ServiceId",
            schema: "service",
            table: "Category",
            newName: "CategoryId");

        migrationBuilder.AddPrimaryKey(
            name: "PK_Category",
            schema: "service",
            table: "Category",
            column: "CategoryId");

        migrationBuilder.DropPrimaryKey(
            name: "PK_BusinessService",
            schema: "business",
            table: "BusinessService");

        migrationBuilder.RenameTable(
            name: "BusinessService",
            schema: "business",
            newName: "BusinessCategory",
            newSchema: "business");

        migrationBuilder.RenameColumn(
            name: "BusinessServiceId",
            schema: "business",
            table: "BusinessCategory",
            newName: "BusinessCategoryId");

        migrationBuilder.RenameColumn(
            name: "ServiceId",
            schema: "business",
            table: "BusinessCategory",
            newName: "CategoryId");

        migrationBuilder.RenameIndex(
            name: "IX_BusinessService_ServiceId",
            schema: "business",
            table: "BusinessCategory",
            newName: "IX_BusinessCategory_CategoryId");

        migrationBuilder.RenameIndex(
            name: "IX_BusinessService_BusinessId",
            schema: "business",
            table: "BusinessCategory",
            newName: "IX_BusinessCategory_BusinessId");

        migrationBuilder.AddPrimaryKey(
            name: "PK_BusinessCategory",
            schema: "business",
            table: "BusinessCategory",
            column: "BusinessCategoryId");

        migrationBuilder.DropPrimaryKey(
            name: "PK_BusinessSubService",
            schema: "business",
            table: "BusinessSubService");

        migrationBuilder.RenameTable(
            name: "BusinessSubService",
            schema: "business",
            newName: "BusinessService",
            newSchema: "business");

        migrationBuilder.RenameColumn(
            name: "BusinessSubServiceId",
            schema: "business",
            table: "BusinessService",
            newName: "BusinessServiceId");

        migrationBuilder.RenameColumn(
            name: "ServiceId",
            schema: "business",
            table: "BusinessService",
            newName: "CategoryId");

        migrationBuilder.RenameIndex(
            name: "IX_BusinessSubService_BusinessId",
            schema: "business",
            table: "BusinessService",
            newName: "IX_BusinessService_BusinessId");

        migrationBuilder.RenameIndex(
            name: "IX_BusinessSubService_ServiceId",
            schema: "business",
            table: "BusinessService",
            newName: "IX_BusinessService_CategoryId");

        migrationBuilder.RenameIndex(
            name: "IX_BusinessSubService_BusinessId_ServiceId_Name",
            schema: "business",
            table: "BusinessService",
            newName: "IX_BusinessService_BusinessId_CategoryId_Name");

        migrationBuilder.AddPrimaryKey(
            name: "PK_BusinessService",
            schema: "business",
            table: "BusinessService",
            column: "BusinessServiceId");

        migrationBuilder.DropPrimaryKey(
            name: "PK_BusinessSubServiceAssignment",
            schema: "business",
            table: "BusinessSubServiceAssignment");

        migrationBuilder.RenameTable(
            name: "BusinessSubServiceAssignment",
            schema: "business",
            newName: "BusinessServiceAssignment",
            newSchema: "business");

        migrationBuilder.DropIndex(
            name: "IX_BusinessSubServiceAssignment_BusinessSubServiceId",
            schema: "business",
            table: "BusinessServiceAssignment");

        migrationBuilder.DropIndex(
            name: "IX_BusinessSubServiceAssignment_BusinessUserId",
            schema: "business",
            table: "BusinessServiceAssignment");

        migrationBuilder.DropIndex(
            name: "IX_BusinessSubServiceAssignment_BusinessSubServiceId_BusinessUserId",
            schema: "business",
            table: "BusinessServiceAssignment");

        migrationBuilder.RenameColumn(
            name: "BusinessSubServiceAssignmentId",
            schema: "business",
            table: "BusinessServiceAssignment",
            newName: "BusinessServiceAssignmentId");

        migrationBuilder.RenameColumn(
            name: "BusinessSubServiceId",
            schema: "business",
            table: "BusinessServiceAssignment",
            newName: "BusinessServiceId");

        migrationBuilder.AddPrimaryKey(
            name: "PK_BusinessServiceAssignment",
            schema: "business",
            table: "BusinessServiceAssignment",
            column: "BusinessServiceAssignmentId");

        migrationBuilder.CreateIndex(
            name: "IX_BusinessServiceAssignment_BusinessServiceId",
            schema: "business",
            table: "BusinessServiceAssignment",
            column: "BusinessServiceId");

        migrationBuilder.CreateIndex(
            name: "IX_BusinessServiceAssignment_BusinessUserId",
            schema: "business",
            table: "BusinessServiceAssignment",
            column: "BusinessUserId");

        migrationBuilder.CreateIndex(
            name: "IX_BusinessServiceAssignment_BusinessServiceId_BusinessUserId",
            schema: "business",
            table: "BusinessServiceAssignment",
            columns: new[] { "BusinessServiceId", "BusinessUserId" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_BusinessCategory_Category_CategoryId",
            schema: "business",
            table: "BusinessCategory",
            column: "CategoryId",
            principalSchema: "service",
            principalTable: "Category",
            principalColumn: "CategoryId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_BusinessService_Category_CategoryId",
            schema: "business",
            table: "BusinessService",
            column: "CategoryId",
            principalSchema: "service",
            principalTable: "Category",
            principalColumn: "CategoryId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_BusinessServiceAssignment_BusinessService_BusinessServiceId",
            schema: "business",
            table: "BusinessServiceAssignment",
            column: "BusinessServiceId",
            principalSchema: "business",
            principalTable: "BusinessService",
            principalColumn: "BusinessServiceId",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_BusinessCategory_Category_CategoryId",
            schema: "business",
            table: "BusinessCategory");

        migrationBuilder.DropForeignKey(
            name: "FK_BusinessService_Category_CategoryId",
            schema: "business",
            table: "BusinessService");

        migrationBuilder.DropForeignKey(
            name: "FK_BusinessServiceAssignment_BusinessService_BusinessServiceId",
            schema: "business",
            table: "BusinessServiceAssignment");

        migrationBuilder.DropPrimaryKey(
            name: "PK_BusinessServiceAssignment",
            schema: "business",
            table: "BusinessServiceAssignment");

        migrationBuilder.DropIndex(
            name: "IX_BusinessServiceAssignment_BusinessServiceId",
            schema: "business",
            table: "BusinessServiceAssignment");

        migrationBuilder.DropIndex(
            name: "IX_BusinessServiceAssignment_BusinessUserId",
            schema: "business",
            table: "BusinessServiceAssignment");

        migrationBuilder.DropIndex(
            name: "IX_BusinessServiceAssignment_BusinessServiceId_BusinessUserId",
            schema: "business",
            table: "BusinessServiceAssignment");

        migrationBuilder.RenameColumn(
            name: "BusinessServiceAssignmentId",
            schema: "business",
            table: "BusinessServiceAssignment",
            newName: "BusinessSubServiceAssignmentId");

        migrationBuilder.RenameColumn(
            name: "BusinessServiceId",
            schema: "business",
            table: "BusinessServiceAssignment",
            newName: "BusinessSubServiceId");

        migrationBuilder.RenameTable(
            name: "BusinessServiceAssignment",
            schema: "business",
            newName: "BusinessSubServiceAssignment",
            newSchema: "business");

        migrationBuilder.AddPrimaryKey(
            name: "PK_BusinessSubServiceAssignment",
            schema: "business",
            table: "BusinessSubServiceAssignment",
            column: "BusinessSubServiceAssignmentId");

        migrationBuilder.CreateIndex(
            name: "IX_BusinessSubServiceAssignment_BusinessSubServiceId",
            schema: "business",
            table: "BusinessSubServiceAssignment",
            column: "BusinessSubServiceId");

        migrationBuilder.CreateIndex(
            name: "IX_BusinessSubServiceAssignment_BusinessUserId",
            schema: "business",
            table: "BusinessSubServiceAssignment",
            column: "BusinessUserId");

        migrationBuilder.CreateIndex(
            name: "IX_BusinessSubServiceAssignment_BusinessSubServiceId_BusinessUserId",
            schema: "business",
            table: "BusinessSubServiceAssignment",
            columns: new[] { "BusinessSubServiceId", "BusinessUserId" },
            unique: true);

        migrationBuilder.DropPrimaryKey(
            name: "PK_BusinessService",
            schema: "business",
            table: "BusinessService");

        migrationBuilder.RenameIndex(
            name: "IX_BusinessService_BusinessId",
            schema: "business",
            table: "BusinessService",
            newName: "IX_BusinessSubService_BusinessId");

        migrationBuilder.RenameIndex(
            name: "IX_BusinessService_CategoryId",
            schema: "business",
            table: "BusinessService",
            newName: "IX_BusinessSubService_ServiceId");

        migrationBuilder.RenameIndex(
            name: "IX_BusinessService_BusinessId_CategoryId_Name",
            schema: "business",
            table: "BusinessService",
            newName: "IX_BusinessSubService_BusinessId_ServiceId_Name");

        migrationBuilder.RenameColumn(
            name: "BusinessServiceId",
            schema: "business",
            table: "BusinessService",
            newName: "BusinessSubServiceId");

        migrationBuilder.RenameColumn(
            name: "CategoryId",
            schema: "business",
            table: "BusinessService",
            newName: "ServiceId");

        migrationBuilder.RenameTable(
            name: "BusinessService",
            schema: "business",
            newName: "BusinessSubService",
            newSchema: "business");

        migrationBuilder.AddPrimaryKey(
            name: "PK_BusinessSubService",
            schema: "business",
            table: "BusinessSubService",
            column: "BusinessSubServiceId");

        migrationBuilder.DropPrimaryKey(
            name: "PK_BusinessCategory",
            schema: "business",
            table: "BusinessCategory");

        migrationBuilder.RenameIndex(
            name: "IX_BusinessCategory_CategoryId",
            schema: "business",
            table: "BusinessCategory",
            newName: "IX_BusinessService_ServiceId");

        migrationBuilder.RenameIndex(
            name: "IX_BusinessCategory_BusinessId",
            schema: "business",
            table: "BusinessCategory",
            newName: "IX_BusinessService_BusinessId");

        migrationBuilder.RenameColumn(
            name: "BusinessCategoryId",
            schema: "business",
            table: "BusinessCategory",
            newName: "BusinessServiceId");

        migrationBuilder.RenameColumn(
            name: "CategoryId",
            schema: "business",
            table: "BusinessCategory",
            newName: "ServiceId");

        migrationBuilder.RenameTable(
            name: "BusinessCategory",
            schema: "business",
            newName: "BusinessService",
            newSchema: "business");

        migrationBuilder.AddPrimaryKey(
            name: "PK_BusinessService",
            schema: "business",
            table: "BusinessService",
            column: "BusinessServiceId");

        migrationBuilder.DropPrimaryKey(
            name: "PK_Category",
            schema: "service",
            table: "Category");

        migrationBuilder.RenameColumn(
            name: "CategoryId",
            schema: "service",
            table: "Category",
            newName: "ServiceId");

        migrationBuilder.RenameTable(
            name: "Category",
            schema: "service",
            newName: "Service",
            newSchema: "service");

        migrationBuilder.AddPrimaryKey(
            name: "PK_Service",
            schema: "service",
            table: "Service",
            column: "ServiceId");

        migrationBuilder.AddForeignKey(
            name: "FK_BusinessService_Service_ServiceId",
            schema: "business",
            table: "BusinessService",
            column: "ServiceId",
            principalSchema: "service",
            principalTable: "Service",
            principalColumn: "ServiceId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_BusinessSubService_Service_ServiceId",
            schema: "business",
            table: "BusinessSubService",
            column: "ServiceId",
            principalSchema: "service",
            principalTable: "Service",
            principalColumn: "ServiceId",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_BusinessSubServiceAssignment_BusinessSubService_BusinessSubServiceId",
            schema: "business",
            table: "BusinessSubServiceAssignment",
            column: "BusinessSubServiceId",
            principalSchema: "business",
            principalTable: "BusinessSubService",
            principalColumn: "BusinessSubServiceId",
            onDelete: ReferentialAction.Restrict);
    }
}
