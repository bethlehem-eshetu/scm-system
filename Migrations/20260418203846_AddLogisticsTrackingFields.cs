using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class AddLogisticsTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedManagerId",
                table: "Warehouses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CapacityUsed",
                table: "Warehouses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonName",
                table: "Warehouses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Warehouses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContact",
                table: "Warehouses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastInventoryCount",
                table: "Warehouses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OperatingHoursFrom",
                table: "Warehouses",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OperatingHoursTo",
                table: "Warehouses",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedDriverId",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FuelEfficiency",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelType",
                table: "Vehicles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastServiceDate",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Mileage",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextServiceDueDate",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedManagerId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CapacityUsed",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ContactPersonName",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "EmergencyContact",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "LastInventoryCount",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "OperatingHoursFrom",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "OperatingHoursTo",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "AssignedDriverId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FuelEfficiency",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FuelType",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "LastServiceDate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Mileage",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "NextServiceDueDate",
                table: "Vehicles");
        }
    }
}
