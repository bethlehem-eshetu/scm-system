using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseLogisticsOverhaul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentMileage",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverageArea",
                table: "DriverProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_WarehouseId",
                table: "Vehicles",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Warehouses_WarehouseId",
                table: "Vehicles",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Warehouses_WarehouseId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_WarehouseId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CurrentMileage",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CoverageArea",
                table: "DriverProfiles");
        }
    }
}
