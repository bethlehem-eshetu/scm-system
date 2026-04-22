using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class AddPrimaryAssignmentsToAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactPersonName",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "EmergencyContact",
                table: "Warehouses");

            migrationBuilder.AddColumn<int>(
                name: "PrimaryManagerId",
                table: "Warehouses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryDriverId",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_PrimaryManagerId",
                table: "Warehouses",
                column: "PrimaryManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_PrimaryDriverId",
                table: "Vehicles",
                column: "PrimaryDriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_SupplierEmployees_PrimaryDriverId",
                table: "Vehicles",
                column: "PrimaryDriverId",
                principalTable: "SupplierEmployees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Warehouses_SupplierEmployees_PrimaryManagerId",
                table: "Warehouses",
                column: "PrimaryManagerId",
                principalTable: "SupplierEmployees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_SupplierEmployees_PrimaryDriverId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Warehouses_SupplierEmployees_PrimaryManagerId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Warehouses_PrimaryManagerId",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_PrimaryDriverId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "PrimaryManagerId",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "PrimaryDriverId",
                table: "Vehicles");

            migrationBuilder.AddColumn<string>(
                name: "ContactPersonName",
                table: "Warehouses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Warehouses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Warehouses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContact",
                table: "Warehouses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }
    }
}
