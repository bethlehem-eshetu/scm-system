using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreBidFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AfterSalesSupport",
                table: "TenderBids",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceCoverage",
                table: "TenderBids",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductSpecifications",
                table: "TenderBids",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityCertifications",
                table: "TenderBids",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "References",
                table: "TenderBids",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AfterSalesSupport",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "InsuranceCoverage",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "ProductSpecifications",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "QualityCertifications",
                table: "TenderBids");

            migrationBuilder.DropColumn(
                name: "References",
                table: "TenderBids");
        }
    }
}
