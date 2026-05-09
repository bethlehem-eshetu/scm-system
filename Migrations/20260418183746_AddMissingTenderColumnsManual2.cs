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
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenders') AND name = 'ProductName') ALTER TABLE Tenders ADD ProductName nvarchar(max) NULL;");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenders') AND name = 'AllowPartialBids') ALTER TABLE Tenders ADD AllowPartialBids bit NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenders') AND name = 'AttachmentPath') ALTER TABLE Tenders ADD AttachmentPath nvarchar(max) NULL;");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenders') AND name = 'BudgetMax') ALTER TABLE Tenders ADD BudgetMax decimal(18,2) NULL;");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenders') AND name = 'BudgetMin') ALTER TABLE Tenders ADD BudgetMin decimal(18,2) NULL;");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenders') AND name = 'PreferredSuppliers') ALTER TABLE Tenders ADD PreferredSuppliers nvarchar(max) NULL;");
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
