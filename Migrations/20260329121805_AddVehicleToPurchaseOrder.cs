using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleToPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "PurchaseOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_VehicleId",
                table: "PurchaseOrders",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Vehicles_VehicleId",
                table: "PurchaseOrders",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Vehicles_VehicleId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_VehicleId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "PurchaseOrders");
        }
    }
}
