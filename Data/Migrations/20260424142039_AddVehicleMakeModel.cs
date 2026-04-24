using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleMakeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Column 'Make' already exists in the database
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Make",
                table: "Vehicles");
        }
    }
}
