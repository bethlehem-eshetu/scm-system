using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryFieldsToPO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChecklistVerified",
                table: "PurchaseOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryNotes",
                table: "PurchaseOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "PurchaseOrders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsQRVerified",
                table: "PurchaseOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SignaturePath",
                table: "PurchaseOrders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionRateAtTransaction",
                table: "Commissions",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChecklistVerified",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryNotes",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "IsQRVerified",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SignaturePath",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CommissionRateAtTransaction",
                table: "Commissions");
        }
    }
}
