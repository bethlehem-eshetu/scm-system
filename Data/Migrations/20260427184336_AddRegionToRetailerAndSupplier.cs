using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionToRetailerAndSupplier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hardened Idempotent SQL checks for Region column
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'Region') BEGIN ALTER TABLE Suppliers ADD Region nvarchar(100) NOT NULL DEFAULT ''; END");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Retailers') AND name = 'Region') BEGIN ALTER TABLE Retailers ADD Region nvarchar(100) NOT NULL DEFAULT ''; END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Region",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Retailers");
        }
    }
}
