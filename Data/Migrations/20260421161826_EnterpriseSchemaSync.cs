using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseSchemaSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_PerformedBy",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_PerformedBy",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "PerformedBy",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "AuditLogs",
                newName: "PerformedAtUtc");

            migrationBuilder.RenameColumn(
                name: "Reason",
                table: "AuditLogs",
                newName: "OldValueJson");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "AuditLogs",
                newName: "EntityId");

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlySalary",
                table: "SupplierEmployees",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetRole",
                table: "Notifications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetWarehouseId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "AuditLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "AuditLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NewValueJson",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PerformedByUserId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PerformedByUserId",
                table: "AuditLogs",
                column: "PerformedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_PerformedByUserId",
                table: "AuditLogs",
                column: "PerformedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_PerformedByUserId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_PerformedByUserId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "MonthlySalary",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "TargetRole",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TargetWarehouseId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "NewValueJson",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "PerformedByUserId",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "PerformedAtUtc",
                table: "AuditLogs",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "OldValueJson",
                table: "AuditLogs",
                newName: "Reason");

            migrationBuilder.RenameColumn(
                name: "EntityId",
                table: "AuditLogs",
                newName: "Action");

            migrationBuilder.AddColumn<int>(
                name: "PerformedBy",
                table: "AuditLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "AuditLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PerformedBy",
                table: "AuditLogs",
                column: "PerformedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_PerformedBy",
                table: "AuditLogs",
                column: "PerformedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
