using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInboundLogisticsAndProductHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAdjustments_Products_ProductId",
                table: "InventoryAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAdjustments_Users_ApprovedById",
                table: "InventoryAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAdjustments_Warehouses_WarehouseId",
                table: "InventoryAdjustments");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Products",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "InventoryMovements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Inventories",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "InboundShipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpectedArrival = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundShipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboundShipments_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InboundShipments_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InboundShipmentItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InboundShipmentId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ExpectedQuantity = table.Column<int>(type: "int", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "int", nullable: false),
                    DamagedQuantity = table.Column<int>(type: "int", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboundShipmentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InboundShipmentItems_InboundShipments_InboundShipmentId",
                        column: x => x.InboundShipmentId,
                        principalTable: "InboundShipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InboundShipmentItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_WarehouseCode",
                table: "Warehouses",
                column: "WarehouseCode",
                unique: true,
                filter: "[WarehouseCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_LicensePlate",
                table: "Vehicles",
                column: "LicensePlate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierEmployees_Email",
                table: "SupplierEmployees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierEmployees_Phone",
                table: "SupplierEmployees",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InboundShipmentItems_InboundShipmentId",
                table: "InboundShipmentItems",
                column: "InboundShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundShipmentItems_ProductId",
                table: "InboundShipmentItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundShipments_SupplierId",
                table: "InboundShipments",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_InboundShipments_WarehouseId",
                table: "InboundShipments",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAdjustments_Products_ProductId",
                table: "InventoryAdjustments",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAdjustments_Users_ApprovedById",
                table: "InventoryAdjustments",
                column: "ApprovedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAdjustments_Warehouses_WarehouseId",
                table: "InventoryAdjustments",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAdjustments_Products_ProductId",
                table: "InventoryAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAdjustments_Users_ApprovedById",
                table: "InventoryAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryAdjustments_Warehouses_WarehouseId",
                table: "InventoryAdjustments");

            migrationBuilder.DropTable(
                name: "InboundShipmentItems");

            migrationBuilder.DropTable(
                name: "InboundShipments");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_WarehouseCode",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_LicensePlate",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_SupplierEmployees_Email",
                table: "SupplierEmployees");

            migrationBuilder.DropIndex(
                name: "IX_SupplierEmployees_Phone",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Inventories");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAdjustments_Products_ProductId",
                table: "InventoryAdjustments",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAdjustments_Users_ApprovedById",
                table: "InventoryAdjustments",
                column: "ApprovedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryAdjustments_Warehouses_WarehouseId",
                table: "InventoryAdjustments",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
