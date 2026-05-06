using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCommissionFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
/*
            migrationBuilder.DropForeignKey(
                name: "FK_Commissions_PurchaseOrders_PurchaseOrderId",
                table: "Commissions");

            migrationBuilder.DropIndex(
                name: "IX_Commissions_PurchaseOrderId",
                table: "Commissions");
*/

            migrationBuilder.AlterColumn<int>(
                name: "PurchaseOrderId",
                table: "Commissions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Commissions_PurchaseOrderId' AND object_id = OBJECT_ID('Commissions')) DROP INDEX IX_Commissions_PurchaseOrderId ON Commissions");
            migrationBuilder.CreateIndex(
                name: "IX_Commissions_PurchaseOrderId",
                table: "Commissions",
                column: "PurchaseOrderId");

/*
            migrationBuilder.AddForeignKey(
                name: "FK_Commissions_PurchaseOrders_PurchaseOrderId",
                table: "Commissions",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id");
*/
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commissions_PurchaseOrders_PurchaseOrderId",
                table: "Commissions");

            migrationBuilder.DropIndex(
                name: "IX_Commissions_PurchaseOrderId",
                table: "Commissions");

            migrationBuilder.AlterColumn<int>(
                name: "PurchaseOrderId",
                table: "Commissions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_PurchaseOrderId",
                table: "Commissions",
                column: "PurchaseOrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Commissions_PurchaseOrders_PurchaseOrderId",
                table: "Commissions",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
