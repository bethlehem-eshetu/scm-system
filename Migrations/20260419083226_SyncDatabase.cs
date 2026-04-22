using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class SyncDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoAcceptPickTasks",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DefaultWarehouseLocation",
                table: "SupplierEmployees",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LowStockThreshold",
                table: "SupplierEmployees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyLowStock",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PicklistFormat",
                table: "SupplierEmployees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoAcceptPickTasks",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "DefaultWarehouseLocation",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "LowStockThreshold",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "NotifyLowStock",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "PicklistFormat",
                table: "SupplierEmployees");
        }
    }
}
