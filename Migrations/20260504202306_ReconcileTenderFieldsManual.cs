using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class ReconcileTenderFieldsManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reconcile TenderBids
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TenderBids') AND name = 'PaymentTerms') ALTER TABLE TenderBids ADD PaymentTerms nvarchar(max) NULL;");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TenderBids') AND name = 'PreviousExperience') ALTER TABLE TenderBids ADD PreviousExperience nvarchar(max) NULL;");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TenderBids') AND name = 'WarrantyPeriod') ALTER TABLE TenderBids ADD WarrantyPeriod nvarchar(max) NULL;");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('TenderBids') AND name = 'WarrantyType') ALTER TABLE TenderBids ADD WarrantyType nvarchar(max) NULL;");

            // Reconcile Tenders
            migrationBuilder.Sql("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tenders') AND name = 'PaymentTerms') ALTER TABLE Tenders ADD PaymentTerms nvarchar(max) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PaymentTerms", table: "TenderBids");
            migrationBuilder.DropColumn(name: "PreviousExperience", table: "TenderBids");
            migrationBuilder.DropColumn(name: "WarrantyPeriod", table: "TenderBids");
            migrationBuilder.DropColumn(name: "WarrantyType", table: "TenderBids");
            migrationBuilder.DropColumn(name: "PaymentTerms", table: "Tenders");
        }
    }
}
