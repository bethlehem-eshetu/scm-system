using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingTenderColumnsManual2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "Tenders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowPartialBids",
                table: "Tenders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "Tenders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetMax",
                table: "Tenders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetMin",
                table: "Tenders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredSuppliers",
                table: "Tenders",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "AllowPartialBids",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "BudgetMax",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "BudgetMin",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "PreferredSuppliers",
                table: "Tenders");
        }
    }
}
