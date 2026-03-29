using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShadowProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductId1",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PurchaseOrders_PurchaseOrderId1",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_Products_ProductId1",
                table: "PurchaseOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderItems_ProductId1",
                table: "PurchaseOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PurchaseOrderId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PurchaseOrderId1",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductId1",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId1",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "OrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PurchaseOrderId",
                table: "Orders",
                column: "PurchaseOrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_PurchaseOrderId",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "PurchaseOrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrderId1",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_ProductId1",
                table: "PurchaseOrderItems",
                column: "ProductId1");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PurchaseOrderId",
                table: "Orders",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PurchaseOrderId1",
                table: "Orders",
                column: "PurchaseOrderId1",
                unique: true,
                filter: "[PurchaseOrderId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId1",
                table: "OrderItems",
                column: "ProductId1");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductId1",
                table: "OrderItems",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_PurchaseOrders_PurchaseOrderId1",
                table: "Orders",
                column: "PurchaseOrderId1",
                principalTable: "PurchaseOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_Products_ProductId1",
                table: "PurchaseOrderItems",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
