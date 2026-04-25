using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRetailerSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoAcceptPreferredBids",
                table: "Retailers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoNotifyNewTenders",
                table: "Retailers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BlockedSuppliers",
                table: "Retailers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetMax",
                table: "Retailers",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetMin",
                table: "Retailers",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonEmail",
                table: "Retailers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonName",
                table: "Retailers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonPhone",
                table: "Retailers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBillingAddress",
                table: "Retailers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultShippingAddress",
                table: "Retailers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultShippingMethod",
                table: "Retailers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultTenderClosingDays",
                table: "Retailers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FavoriteSuppliers",
                table: "Retailers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredCategories",
                table: "Retailers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredDeliveryTimeline",
                table: "Retailers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PreferredPaymentMethod",
                table: "Retailers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProofOfDeliveryRequired",
                table: "Retailers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SupplierRatingThreshold",
                table: "Retailers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "Retailers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsInBusiness",
                table: "Retailers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RetailerAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RetailerId = table.Column<int>(type: "int", nullable: false),
                    AddressType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailerAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailerAddresses_Retailers_RetailerId",
                        column: x => x.RetailerId,
                        principalTable: "Retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RetailerPaymentMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RetailerId = table.Column<int>(type: "int", nullable: false),
                    MethodType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailerPaymentMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailerPaymentMethods_Retailers_RetailerId",
                        column: x => x.RetailerId,
                        principalTable: "Retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetailerAddresses_RetailerId",
                table: "RetailerAddresses",
                column: "RetailerId");

            migrationBuilder.CreateIndex(
                name: "IX_RetailerPaymentMethods_RetailerId",
                table: "RetailerPaymentMethods",
                column: "RetailerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetailerAddresses");

            migrationBuilder.DropTable(
                name: "RetailerPaymentMethods");

            migrationBuilder.DropColumn(
                name: "AutoAcceptPreferredBids",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "AutoNotifyNewTenders",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "BlockedSuppliers",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "BudgetMax",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "BudgetMin",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "ContactPersonEmail",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "ContactPersonName",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "ContactPersonPhone",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "DefaultBillingAddress",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "DefaultShippingAddress",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "DefaultShippingMethod",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "DefaultTenderClosingDays",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "FavoriteSuppliers",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "PreferredCategories",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "PreferredDeliveryTimeline",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "PreferredPaymentMethod",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "ProofOfDeliveryRequired",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "SupplierRatingThreshold",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "Retailers");

            migrationBuilder.DropColumn(
                name: "YearsInBusiness",
                table: "Retailers");
        }
    }
}
