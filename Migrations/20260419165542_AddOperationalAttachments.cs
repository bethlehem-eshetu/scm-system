using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Timezone",
                table: "Warehouses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeekendDays",
                table: "Warehouses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceCertificateUrl",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationCertificateUrl",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehiclePhotosUrls",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractDocumentUrl",
                table: "SupplierEmployees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdDocumentUrl",
                table: "SupplierEmployees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "SupplierEmployees",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Timezone",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "WeekendDays",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "InsuranceCertificateUrl",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RegistrationCertificateUrl",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehiclePhotosUrls",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ContractDocumentUrl",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "IdDocumentUrl",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "SupplierEmployees");
        }
    }
}
