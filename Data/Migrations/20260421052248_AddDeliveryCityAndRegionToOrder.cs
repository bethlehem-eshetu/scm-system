using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryCityAndRegionToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverageRegions",
                table: "Warehouses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentWorkload",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxDeliveryDistanceKM",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "SupplierEmployees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeDisplayId",
                table: "SupplierEmployees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ForcePasswordChange",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "SupplierEmployees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryCity",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryRegion",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverageRegions",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "CurrentWorkload",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "MaxDeliveryDistanceKM",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "EmployeeDisplayId",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "ForcePasswordChange",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "DeliveryCity",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryRegion",
                table: "Orders");
        }
    }
}
