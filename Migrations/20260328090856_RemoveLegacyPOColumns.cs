using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyPOColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_PurchaseOrders_Products_ProductId", "PurchaseOrders");
            migrationBuilder.DropIndex("IX_PurchaseOrders_ProductId", "PurchaseOrders");
            migrationBuilder.DropColumn("ProductId", "PurchaseOrders");
            migrationBuilder.DropColumn("ProductName", "PurchaseOrders");
            migrationBuilder.DropColumn("Quantity", "PurchaseOrders");
            migrationBuilder.DropColumn("UnitPrice", "PurchaseOrders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
