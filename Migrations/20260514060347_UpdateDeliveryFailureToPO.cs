using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeliveryFailureToPO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryFailures_DispatchTasks_DispatchTaskId",
                table: "DeliveryFailures");

            migrationBuilder.RenameColumn(
                name: "DispatchTaskId",
                table: "DeliveryFailures",
                newName: "PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_DeliveryFailures_DispatchTaskId",
                table: "DeliveryFailures",
                newName: "IX_DeliveryFailures_PurchaseOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryFailures_PurchaseOrders_PurchaseOrderId",
                table: "DeliveryFailures",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryFailures_PurchaseOrders_PurchaseOrderId",
                table: "DeliveryFailures");

            migrationBuilder.RenameColumn(
                name: "PurchaseOrderId",
                table: "DeliveryFailures",
                newName: "DispatchTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_DeliveryFailures_PurchaseOrderId",
                table: "DeliveryFailures",
                newName: "IX_DeliveryFailures_DispatchTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryFailures_DispatchTasks_DispatchTaskId",
                table: "DeliveryFailures",
                column: "DispatchTaskId",
                principalTable: "DispatchTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
