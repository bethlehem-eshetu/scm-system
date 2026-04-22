using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class ProfessionalSCMArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLicenseVerified",
                table: "SupplierEmployees");

            migrationBuilder.RenameColumn(
                name: "StorageType",
                table: "Warehouses",
                newName: "StorageArchitecture");

            migrationBuilder.RenameColumn(
                name: "HandlingTimeHours",
                table: "Warehouses",
                newName: "HubType");

            migrationBuilder.RenameColumn(
                name: "AssignedManagerId",
                table: "Warehouses",
                newName: "LoadingBays");

            migrationBuilder.RenameColumn(
                name: "VolumeCapacity",
                table: "Vehicles",
                newName: "PurchaseCost");

            migrationBuilder.RenameColumn(
                name: "RoadworthinessStatus",
                table: "Vehicles",
                newName: "Model");

            migrationBuilder.RenameColumn(
                name: "RegistrationNumber",
                table: "Vehicles",
                newName: "Color");

            migrationBuilder.RenameColumn(
                name: "LastMaintenanceDate",
                table: "Vehicles",
                newName: "TireChangeDue");

            migrationBuilder.RenameColumn(
                name: "InsuranceStatus",
                table: "Vehicles",
                newName: "AssetCode");

            migrationBuilder.RenameColumn(
                name: "HasTemperatureControl",
                table: "Vehicles",
                newName: "TemperatureControlled");

            migrationBuilder.RenameColumn(
                name: "AssignedDriverId",
                table: "Vehicles",
                newName: "ManufactureYear");

            migrationBuilder.RenameColumn(
                name: "LicenseExpiryDate",
                table: "SupplierEmployees",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "DrivingLicenseNumber",
                table: "SupplierEmployees",
                newName: "EmergencyContact");

            migrationBuilder.AddColumn<int>(
                name: "AvgProcessingTimeHours",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "CCTVEnabled",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Warehouses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Warehouses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FireSafetyInstalled",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ForkliftsAvailable",
                table: "Warehouses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Landmark",
                table: "Warehouses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Warehouses",
                type: "decimal(10,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Warehouses",
                type: "decimal(11,8)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubCityZone",
                table: "Warehouses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Warehouses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkingDays",
                table: "Warehouses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Vehicles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentEstimatedValue",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FuelTankCapacity",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GPSInstalled",
                table: "Vehicles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "InternalVolumeM3",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Vehicles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationExpiryDate",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SupplierEmployees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "SupplierEmployees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmploymentType",
                table: "SupplierEmployees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "SupplierEmployees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "JoinDate",
                table: "SupplierEmployees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NationalID",
                table: "SupplierEmployees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Shift",
                table: "SupplierEmployees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "SupplierEmployees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DriverProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierEmployeeId = table.Column<int>(type: "int", nullable: false),
                    DrivingLicenseNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LicenseType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LicenseIssueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LicenseExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MedicalFitnessExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryRegion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CityCoverage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverProfiles_SupplierEmployees_SupplierEmployeeId",
                        column: x => x.SupplierEmployeeId,
                        principalTable: "SupplierEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    SupplierEmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_SupplierEmployees_SupplierEmployeeId",
                        column: x => x.SupplierEmployeeId,
                        principalTable: "SupplierEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VehicleAssignments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    SupplierEmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseAssignments_SupplierEmployees_SupplierEmployeeId",
                        column: x => x.SupplierEmployeeId,
                        principalTable: "SupplierEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseAssignments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CanApproveTransfers = table.Column<bool>(type: "bit", nullable: false),
                    CanManageInventory = table.Column<bool>(type: "bit", nullable: false),
                    CanViewReports = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseProfiles_SupplierEmployees_SupplierEmployeeId",
                        column: x => x.SupplierEmployeeId,
                        principalTable: "SupplierEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_SupplierEmployeeId",
                table: "DriverProfiles",
                column: "SupplierEmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_SupplierEmployeeId",
                table: "VehicleAssignments",
                column: "SupplierEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleAssignments_VehicleId",
                table: "VehicleAssignments",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseAssignments_SupplierEmployeeId",
                table: "WarehouseAssignments",
                column: "SupplierEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseAssignments_WarehouseId",
                table: "WarehouseAssignments",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseProfiles_SupplierEmployeeId",
                table: "WarehouseProfiles",
                column: "SupplierEmployeeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverProfiles");

            migrationBuilder.DropTable(
                name: "VehicleAssignments");

            migrationBuilder.DropTable(
                name: "WarehouseAssignments");

            migrationBuilder.DropTable(
                name: "WarehouseProfiles");

            migrationBuilder.DropColumn(
                name: "AvgProcessingTimeHours",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CCTVEnabled",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "FireSafetyInstalled",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ForkliftsAvailable",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Landmark",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "SubCityZone",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "WorkingDays",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CurrentEstimatedValue",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FuelTankCapacity",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "GPSInstalled",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "InternalVolumeM3",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RegistrationExpiryDate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "JoinDate",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "NationalID",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "Shift",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "SupplierEmployees");

            migrationBuilder.RenameColumn(
                name: "StorageArchitecture",
                table: "Warehouses",
                newName: "StorageType");

            migrationBuilder.RenameColumn(
                name: "LoadingBays",
                table: "Warehouses",
                newName: "AssignedManagerId");

            migrationBuilder.RenameColumn(
                name: "HubType",
                table: "Warehouses",
                newName: "HandlingTimeHours");

            migrationBuilder.RenameColumn(
                name: "TireChangeDue",
                table: "Vehicles",
                newName: "LastMaintenanceDate");

            migrationBuilder.RenameColumn(
                name: "TemperatureControlled",
                table: "Vehicles",
                newName: "HasTemperatureControl");

            migrationBuilder.RenameColumn(
                name: "PurchaseCost",
                table: "Vehicles",
                newName: "VolumeCapacity");

            migrationBuilder.RenameColumn(
                name: "Model",
                table: "Vehicles",
                newName: "RoadworthinessStatus");

            migrationBuilder.RenameColumn(
                name: "ManufactureYear",
                table: "Vehicles",
                newName: "AssignedDriverId");

            migrationBuilder.RenameColumn(
                name: "Color",
                table: "Vehicles",
                newName: "RegistrationNumber");

            migrationBuilder.RenameColumn(
                name: "AssetCode",
                table: "Vehicles",
                newName: "InsuranceStatus");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "SupplierEmployees",
                newName: "LicenseExpiryDate");

            migrationBuilder.RenameColumn(
                name: "EmergencyContact",
                table: "SupplierEmployees",
                newName: "DrivingLicenseNumber");

            migrationBuilder.AddColumn<bool>(
                name: "IsLicenseVerified",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
