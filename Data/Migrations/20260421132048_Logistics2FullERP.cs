using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class Logistics2FullERP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmergencyContact",
                table: "SupplierEmployees",
                newName: "EmergencyContactName");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Warehouses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasBackupPower",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasInternet",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HazardStorageAllowed",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Warehouses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OccupancyStatus",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OverflowWarningThreshold",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PackingStationsCount",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ReceivingAreaSizeM2",
                table: "Warehouses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReservedSpace",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TemperatureZoneTypes",
                table: "Warehouses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccidentHistoryNote",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverEligibilityType",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelCardNumber",
                table: "Vehicles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceProvider",
                table: "Vehicles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Vehicles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ServiceIntervalMonths",
                table: "Vehicles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TireChangeDueMileage",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllowedLoginZones",
                table: "SupplierEmployees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BloodGroup",
                table: "SupplierEmployees",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SupplierEmployees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceAccessRestriction",
                table: "SupplierEmployees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "SupplierEmployees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireMFA",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RolePermissions",
                table: "SupplierEmployees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalaryGrade",
                table: "SupplierEmployees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupervisorId",
                table: "SupplierEmployees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DispatchTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    DeliveryAgentId = table.Column<int>(type: "int", nullable: true),
                    HubId = table.Column<int>(type: "int", nullable: true),
                    RouteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PlannedDeparture = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDeparture = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedArrival = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualArrival = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecipientName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignaturePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryPhotoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryLat = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeliveryLong = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchTasks_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispatchTasks_SupplierEmployees_DeliveryAgentId",
                        column: x => x.DeliveryAgentId,
                        principalTable: "SupplierEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchTasks_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DispatchTasks_Warehouses_HubId",
                        column: x => x.HubId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierEmployeeId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_SupplierEmployees_SupplierEmployeeId",
                        column: x => x.SupplierEmployeeId,
                        principalTable: "SupplierEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWarehouseAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierEmployeeId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    PermissionLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CanApproveDispatch = table.Column<bool>(type: "bit", nullable: false),
                    CanManageStock = table.Column<bool>(type: "bit", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GrantedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWarehouseAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWarehouseAccesses_SupplierEmployees_SupplierEmployeeId",
                        column: x => x.SupplierEmployeeId,
                        principalTable: "SupplierEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeWarehouseAccesses_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GPSLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: false),
                    SpeedKph = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NearestAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GPSLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GPSLogs_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    SourceWarehouseId = table.Column<int>(type: "int", nullable: false),
                    DestinationWarehouseId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_SupplierEmployees_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "SupplierEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_SupplierEmployees_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "SupplierEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryTransfers_Warehouses_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    ServiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OdometerAtService = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceProvider = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NextServiceDue = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextServiceMileage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InvoiceDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuingAuthority = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleDocuments_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleDriverHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    SupplierEmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChangeReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleDriverHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleDriverHistories_SupplierEmployees_SupplierEmployeeId",
                        column: x => x.SupplierEmployeeId,
                        principalTable: "SupplierEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VehicleDriverHistories_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseManagerHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    SupplierEmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChangeReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseManagerHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseManagerHistories_SupplierEmployees_SupplierEmployeeId",
                        column: x => x.SupplierEmployeeId,
                        principalTable: "SupplierEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WarehouseManagerHistories_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    ReportedById = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    DispatchTaskId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Lat = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Long = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ObservedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentReports_DispatchTasks_DispatchTaskId",
                        column: x => x.DispatchTaskId,
                        principalTable: "DispatchTasks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IncidentReports_SupplierEmployees_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "SupplierEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentReports_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentReports_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_IncidentReports_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierEmployees_SupervisorId",
                table: "SupplierEmployees",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchTasks_DeliveryAgentId",
                table: "DispatchTasks",
                column: "DeliveryAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchTasks_HubId",
                table: "DispatchTasks",
                column: "HubId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchTasks_OrderId",
                table: "DispatchTasks",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchTasks_VehicleId",
                table: "DispatchTasks",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_SupplierEmployeeId",
                table: "EmployeeDocuments",
                column: "SupplierEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWarehouseAccesses_SupplierEmployeeId",
                table: "EmployeeWarehouseAccesses",
                column: "SupplierEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWarehouseAccesses_WarehouseId",
                table: "EmployeeWarehouseAccesses",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_GPSLogs_VehicleId",
                table: "GPSLogs",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_DispatchTaskId",
                table: "IncidentReports",
                column: "DispatchTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_ReportedById",
                table: "IncidentReports",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_SupplierId",
                table: "IncidentReports",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_VehicleId",
                table: "IncidentReports",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_WarehouseId",
                table: "IncidentReports",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_ApprovedById",
                table: "InventoryTransfers",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_DestinationWarehouseId",
                table: "InventoryTransfers",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_ProductId",
                table: "InventoryTransfers",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_RequestedById",
                table: "InventoryTransfers",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_SourceWarehouseId",
                table: "InventoryTransfers",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransfers_SupplierId",
                table: "InventoryTransfers",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_VehicleId",
                table: "MaintenanceRecords",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDocuments_VehicleId",
                table: "VehicleDocuments",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDriverHistories_SupplierEmployeeId",
                table: "VehicleDriverHistories",
                column: "SupplierEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDriverHistories_VehicleId",
                table: "VehicleDriverHistories",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseManagerHistories_SupplierEmployeeId",
                table: "WarehouseManagerHistories",
                column: "SupplierEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseManagerHistories_WarehouseId",
                table: "WarehouseManagerHistories",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierEmployees_SupplierEmployees_SupervisorId",
                table: "SupplierEmployees",
                column: "SupervisorId",
                principalTable: "SupplierEmployees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplierEmployees_SupplierEmployees_SupervisorId",
                table: "SupplierEmployees");

            migrationBuilder.DropTable(
                name: "EmployeeDocuments");

            migrationBuilder.DropTable(
                name: "EmployeeWarehouseAccesses");

            migrationBuilder.DropTable(
                name: "GPSLogs");

            migrationBuilder.DropTable(
                name: "IncidentReports");

            migrationBuilder.DropTable(
                name: "InventoryTransfers");

            migrationBuilder.DropTable(
                name: "MaintenanceRecords");

            migrationBuilder.DropTable(
                name: "VehicleDocuments");

            migrationBuilder.DropTable(
                name: "VehicleDriverHistories");

            migrationBuilder.DropTable(
                name: "WarehouseManagerHistories");

            migrationBuilder.DropTable(
                name: "DispatchTasks");

            migrationBuilder.DropIndex(
                name: "IX_SupplierEmployees_SupervisorId",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "HasBackupPower",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "HasInternet",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "HazardStorageAllowed",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "OccupancyStatus",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "OverflowWarningThreshold",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "PackingStationsCount",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ReceivingAreaSizeM2",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ReservedSpace",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "TemperatureZoneTypes",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "AccidentHistoryNote",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "DriverEligibilityType",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FuelCardNumber",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "InsuranceProvider",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ServiceIntervalMonths",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "TireChangeDueMileage",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "AllowedLoginZones",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "BloodGroup",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "DeviceAccessRestriction",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "RequireMFA",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "RolePermissions",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "SalaryGrade",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "SupervisorId",
                table: "SupplierEmployees");

            migrationBuilder.RenameColumn(
                name: "EmergencyContactName",
                table: "SupplierEmployees",
                newName: "EmergencyContact");
        }
    }
}
