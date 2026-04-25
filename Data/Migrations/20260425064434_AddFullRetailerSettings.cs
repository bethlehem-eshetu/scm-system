using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFullRetailerSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BidAcceptedAlert",
                table: "Retailers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DeliveryNotifications",
                table: "Retailers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LowStockAlert",
                table: "Retailers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NewTenderMatchAlert",
                table: "Retailers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OrderDeliveredAlert",
                table: "Retailers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OrderShippedAlert",
                table: "Retailers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PriceDropAlert",
                table: "Retailers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RetailerPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RetailerId = table.Column<int>(type: "int", nullable: false),
                    NewTenderMatchAlert = table.Column<bool>(type: "bit", nullable: false),
                    BidAcceptedAlert = table.Column<bool>(type: "bit", nullable: false),
                    OrderShippedAlert = table.Column<bool>(type: "bit", nullable: false),
                    OrderDeliveredAlert = table.Column<bool>(type: "bit", nullable: false),
                    LowStockAlert = table.Column<bool>(type: "bit", nullable: false),
                    PriceDropAlert = table.Column<bool>(type: "bit", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailerPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailerPreferences_Retailers_RetailerId",
                        column: x => x.RetailerId,
                        principalTable: "Retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetailerPreferences_RetailerId",
                table: "RetailerPreferences",
                column: "RetailerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetailerPreferences");

            migrationBuilder.DropColumn(
                name: "BidAcceptedAlert",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "DeliveryNotifications",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "LowStockAlert",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "NewTenderMatchAlert",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "OrderDeliveredAlert",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "OrderShippedAlert",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "PriceDropAlert",
                table: "Retailers");
        }
    }
}
