using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class FinalLogisticsSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhotoUrl",
                table: "SupplierEmployees",
                newName: "ProfilePhotoPath");

            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "Warehouses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "Vehicles");

            migrationBuilder.RenameColumn(
                name: "ProfilePhotoPath",
                table: "SupplierEmployees",
                newName: "PhotoUrl");
        }
    }
}
