using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class MergeNewSCMEnhancement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "AddressUpdateRequests",
                newName: "RejectionReason");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "AddressUpdateRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reason",
                table: "AddressUpdateRequests");

            migrationBuilder.RenameColumn(
                name: "RejectionReason",
                table: "AddressUpdateRequests",
                newName: "Notes");
        }
    }
}
