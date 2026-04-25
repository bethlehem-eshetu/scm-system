using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationAnd2FA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyBidAlert",
                table: "Suppliers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NotifyChannel",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "NotifyDisputeAlert",
                table: "Suppliers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyLowStockAlert",
                table: "Suppliers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOrderAlert",
                table: "Suppliers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyPaymentAlert",
                table: "Suppliers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyBidAlert",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "NotifyChannel",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "NotifyDisputeAlert",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "NotifyLowStockAlert",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "NotifyOrderAlert",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "NotifyPaymentAlert",
                table: "Suppliers");
        }
    }
}
