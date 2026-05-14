using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class AddAverageRatingToSupplierEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "Suppliers",
                type: "decimal(3,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "SupplierEmployees",
                type: "decimal(3,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Communication",
                table: "Ratings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryAgentId",
                table: "Ratings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackagingQuality",
                table: "Ratings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductQuality",
                table: "Ratings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Professionalism",
                table: "Ratings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RatingType",
                table: "Ratings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShippingSpeed",
                table: "Ratings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Timeliness",
                table: "Ratings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleCondition",
                table: "Ratings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_DeliveryAgentId",
                table: "Ratings",
                column: "DeliveryAgentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_SupplierEmployees_DeliveryAgentId",
                table: "Ratings",
                column: "DeliveryAgentId",
                principalTable: "SupplierEmployees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_SupplierEmployees_DeliveryAgentId",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_DeliveryAgentId",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "SupplierEmployees");

            migrationBuilder.DropColumn(
                name: "Communication",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "DeliveryAgentId",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "PackagingQuality",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "ProductQuality",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "Professionalism",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "RatingType",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "ShippingSpeed",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "Timeliness",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "VehicleCondition",
                table: "Ratings");
        }
    }
}
