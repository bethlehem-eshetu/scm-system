using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseAdvancedSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedZones",
                table: "SupplierEmployees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "DailyCutoffTime",
                table: "SupplierEmployees",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPackingPriority",
                table: "SupplierEmployees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EnableVoicePicking",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PrintLabelFormat",
                table: "SupplierEmployees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedZones",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "DailyCutoffTime",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "DefaultPackingPriority",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "EnableVoicePicking",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "PrintLabelFormat",
                table: "SupplierEmployees");
        }
    }
}
