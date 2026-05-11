using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagingPremiumFields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageType",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SeenAt",
                table: "Messages",
                type: "datetime2",
                nullable: true);

/*
            migrationBuilder.AddColumn<int>(
                name: "OrderId",
                table: "Conversations",
                type: "int",
                nullable: true);
*/

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Conversations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "Conversations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_OrderId",
                table: "Conversations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_WarehouseId",
                table: "Conversations",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Orders_OrderId",
                table: "Conversations",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Warehouses_WarehouseId",
                table: "Conversations",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Orders_OrderId",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Warehouses_WarehouseId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_OrderId",
                table: "Conversations");

            migrationBuilder.DropIndex(
                name: "IX_Conversations_WarehouseId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SeenAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Conversations");
        }
    }
}
