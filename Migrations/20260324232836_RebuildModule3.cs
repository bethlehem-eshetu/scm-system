using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class RebuildModule3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commissions_PurchaseOrders_PurchaseOrderId",
                table: "Commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_Orders_OrderId",
                table: "Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Retailers_RetailerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_TenderBids_TenderBidId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_PurchaseOrders_PurchaseOrderId",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_TenderBids_Tenders_TenderId",
                table: "TenderBids");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenders_ProductCategories_CategoryId",
                table: "Tenders");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenders_Retailers_RetailerId",
                table: "Tenders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_PONumber",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TenderBidId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_PurchaseOrderId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryTimeline",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "OrderItems");

            migrationBuilder.RenameColumn(
                name: "ClosingDate",
                table: "Tenders",
                newName: "SubmissionDeadline");

            migrationBuilder.RenameColumn(
                name: "SubmittedDate",
                table: "TenderBids",
                newName: "SubmittedAt");

            migrationBuilder.RenameColumn(
                name: "BidNotes",
                table: "TenderBids",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "BidAmount",
                table: "TenderBids",
                newName: "ProposedTotalAmount");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "OrderStatusHistories",
                newName: "Comments");

            migrationBuilder.RenameColumn(
                name: "ChangedBy",
                table: "OrderStatusHistories",
                newName: "ChangedByUserId");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Tenders",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "Tenders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "Tenders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "TenderItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedUnitPrice",
                table: "TenderItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "TenderItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DeliveryLeadTimeDays",
                table: "TenderBids",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ValidityPeriodDays",
                table: "TenderBids",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress",
                table: "PurchaseOrders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "PurchaseOrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RetailerId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OrderStatus",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Processing");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Orders",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrderId1",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenderBidId",
                table: "PurchaseOrders",
                column: "TenderBidId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_ProductId1",
                table: "PurchaseOrderItems",
                column: "ProductId1");

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusHistories_ChangedByUserId",
                table: "OrderStatusHistories",
                column: "ChangedByUserId");

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
                name: "FK_Commissions_PurchaseOrders_PurchaseOrderId",
                table: "Commissions",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_Orders_OrderId",
                table: "Deliveries",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_Orders_Retailers_RetailerId",
                table: "Orders",
                column: "RetailerId",
                principalTable: "Retailers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderStatusHistories_Users_ChangedByUserId",
                table: "OrderStatusHistories",
                column: "ChangedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_Products_ProductId1",
                table: "PurchaseOrderItems",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_TenderBids_TenderBidId",
                table: "PurchaseOrders",
                column: "TenderBidId",
                principalTable: "TenderBids",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_PurchaseOrders_PurchaseOrderId",
                table: "Ratings",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TenderBids_Tenders_TenderId",
                table: "TenderBids",
                column: "TenderId",
                principalTable: "Tenders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenders_ProductCategories_CategoryId",
                table: "Tenders",
                column: "CategoryId",
                principalTable: "ProductCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenders_Retailers_RetailerId",
                table: "Tenders",
                column: "RetailerId",
                principalTable: "Retailers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commissions_PurchaseOrders_PurchaseOrderId",
                table: "Commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Deliveries_Orders_OrderId",
                table: "Deliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductId1",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_PurchaseOrders_PurchaseOrderId1",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Retailers_RetailerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderStatusHistories_Users_ChangedByUserId",
                table: "OrderStatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_Products_ProductId1",
                table: "PurchaseOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_TenderBids_TenderBidId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_PurchaseOrders_PurchaseOrderId",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_TenderBids_Tenders_TenderId",
                table: "TenderBids");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenders_ProductCategories_CategoryId",
                table: "Tenders");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenders_Retailers_RetailerId",
                table: "Tenders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_TenderBidId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderItems_ProductId1",
                table: "PurchaseOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderStatusHistories_ChangedByUserId",
                table: "OrderStatusHistories");

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
                name: "ExpectedDeliveryDate",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "EstimatedUnitPrice",
                table: "TenderItems");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "TenderItems");

            migrationBuilder.DropColumn(
                name: "DeliveryLeadTimeDays",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "ValidityPeriodDays",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId1",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "OrderItems");

            migrationBuilder.RenameColumn(
                name: "SubmissionDeadline",
                table: "Tenders",
                newName: "ClosingDate");

            migrationBuilder.RenameColumn(
                name: "SubmittedAt",
                table: "TenderBids",
                newName: "SubmittedDate");

            migrationBuilder.RenameColumn(
                name: "ProposedTotalAmount",
                table: "TenderBids",
                newName: "BidAmount");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "TenderBids",
                newName: "BidNotes");

            migrationBuilder.RenameColumn(
                name: "Comments",
                table: "OrderStatusHistories",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ChangedByUserId",
                table: "OrderStatusHistories",
                newName: "ChangedBy");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Tenders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "TenderItems",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryTimeline",
                table: "TenderBids",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpectedDeliveryDate",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "PurchaseOrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "RetailerId",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "OrderStatus",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Processing",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Orders",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "OrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PONumber",
                table: "PurchaseOrders",
                column: "PONumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_TenderBidId",
                table: "PurchaseOrders",
                column: "TenderBidId",
                unique: true,
                filter: "[TenderBidId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PurchaseOrderId",
                table: "Orders",
                column: "PurchaseOrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Commissions_PurchaseOrders_PurchaseOrderId",
                table: "Commissions",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Deliveries_Orders_OrderId",
                table: "Deliveries",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Retailers_RetailerId",
                table: "Orders",
                column: "RetailerId",
                principalTable: "Retailers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_TenderBids_TenderBidId",
                table: "PurchaseOrders",
                column: "TenderBidId",
                principalTable: "TenderBids",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_PurchaseOrders_PurchaseOrderId",
                table: "Ratings",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenderBids_Tenders_TenderId",
                table: "TenderBids",
                column: "TenderId",
                principalTable: "Tenders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenders_ProductCategories_CategoryId",
                table: "Tenders",
                column: "CategoryId",
                principalTable: "ProductCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenders_Retailers_RetailerId",
                table: "Tenders",
                column: "RetailerId",
                principalTable: "Retailers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
