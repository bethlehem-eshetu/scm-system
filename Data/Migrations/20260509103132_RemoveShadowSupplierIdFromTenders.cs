using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShadowSupplierIdFromTenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tenders_Suppliers_SupplierId",
                table: "Tenders");

            migrationBuilder.DropIndex(
                name: "IX_Tenders_SupplierId",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Tenders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "Tenders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_SupplierId",
                table: "Tenders",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tenders_Suppliers_SupplierId",
                table: "Tenders",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }
    }
}
