using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeLogisticsModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Vehicles");

            migrationBuilder.RenameColumn(
                name: "Capacity",
                table: "Warehouses",
                newName: "StorageType");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Vehicles",
                newName: "HasTemperatureControl");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Warehouses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Warehouses",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Warehouses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HandlingTimeHours",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxCapacity",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Warehouses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsDelivery",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Warehouses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarehouseCode",
                table: "Warehouses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "VehicleType",
                table: "Vehicles",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Vehicles",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<DateTime>(
                name: "InsuranceExpiryDate",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceStatus",
                table: "Vehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMaintenanceDate",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxLoadCapacity",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "Vehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoadworthinessStatus",
                table: "Vehicles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VolumeCapacity",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "HandlingTimeHours",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "MaxCapacity",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "SupportsDelivery",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "WarehouseCode",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "InsuranceExpiryDate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "InsuranceStatus",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "LastMaintenanceDate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "MaxLoadCapacity",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RoadworthinessStatus",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VolumeCapacity",
                table: "Vehicles");

            migrationBuilder.RenameColumn(
                name: "StorageType",
                table: "Warehouses",
                newName: "Capacity");

            migrationBuilder.RenameColumn(
                name: "HasTemperatureControl",
                table: "Vehicles",
                newName: "IsActive");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "Warehouses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Warehouses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "VehicleType",
                table: "Vehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Vehicles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Capacity",
                table: "Vehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
