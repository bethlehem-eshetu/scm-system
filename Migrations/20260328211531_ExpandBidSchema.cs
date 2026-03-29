using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class ExpandBidSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FinancialProposal",
                table: "TenderBids",
                newName: "PackagingPlan");

            migrationBuilder.RenameColumn(
                name: "DeliveryPlan",
                table: "TenderBids",
                newName: "InspectionCompliance");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "TenderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "TenderBids",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCapacity",
                table: "TenderBids",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryMethod",
                table: "TenderBids",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "TenderBids",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "PenaltyAcceptance",
                table: "TenderBids",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProposedDeliveryDate",
                table: "TenderBids",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "TenderBids",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "TenderBids",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "TenderBids",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "TenderBids",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VATPercentage",
                table: "TenderBids",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_TenderItems_ProductId",
                table: "TenderItems",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_TenderItems_Products_ProductId",
                table: "TenderItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenderItems_Products_ProductId",
                table: "TenderItems");

            migrationBuilder.DropIndex(
                name: "IX_TenderItems_ProductId",
                table: "TenderItems");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "TenderItems");

            migrationBuilder.DropColumn(
                name: "DeliveryCapacity",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "DeliveryMethod",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "PenaltyAcceptance",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "ProposedDeliveryDate",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "VATPercentage",
                table: "TenderBids");

            migrationBuilder.RenameColumn(
                name: "PackagingPlan",
                table: "TenderBids",
                newName: "FinancialProposal");

            migrationBuilder.RenameColumn(
                name: "InspectionCompliance",
                table: "TenderBids",
                newName: "DeliveryPlan");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "TenderBids",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
