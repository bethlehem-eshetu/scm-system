using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierEmployees_Users_UserId",
                table: "SupplierEmployees");

            migrationBuilder.DropIndex(
                name: "IX_SupplierEmployees_UserId",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "SupplierEmployees");

            migrationBuilder.RenameColumn(
                name: "EmployeeRole",
                table: "SupplierEmployees",
                newName: "Role");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "SupplierEmployees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SupplierEmployees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SupplierEmployees");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "SupplierEmployees",
                newName: "EmployeeRole");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "SupplierEmployees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierEmployees_UserId",
                table: "SupplierEmployees",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierEmployees_Users_UserId",
                table: "SupplierEmployees",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
