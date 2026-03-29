using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeMultiWarehouseSchema3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Flush orphaned records before applying tight foreign key constraints
            migrationBuilder.Sql("DELETE FROM PurchaseOrders;");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PurchaseOrders_PurchaseOrderId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Warehouses_WarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PurchaseOrderId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "Orders");

            migrationBuilder.AlterColumn<int>(
                name: "WarehouseId",
                table: "PurchaseOrders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "PurchaseOrders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PurchaseOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "PurchaseOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderId",
                table: "PurchaseOrders",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Orders_OrderId",
                table: "PurchaseOrders",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Warehouses_WarehouseId",
                table: "PurchaseOrders",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Orders_OrderId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Warehouses_WarehouseId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_OrderId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "PurchaseOrders");

            migrationBuilder.AlterColumn<int>(
                name: "WarehouseId",
                table: "PurchaseOrders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrderId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PurchaseOrderId",
                table: "Orders",
                column: "PurchaseOrderId",
                unique: true,
                filter: "[PurchaseOrderId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PurchaseOrders_PurchaseOrderId",
                table: "Orders",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Warehouses_WarehouseId",
                table: "PurchaseOrders",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }
    }
}
