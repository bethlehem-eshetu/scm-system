using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixVehiclePropertyTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TireChangeDue",
                table: "Vehicles");

            migrationBuilder.AlterColumn<int>(
                name: "TireChangeDueMileage",
                table: "Vehicles",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Mileage",
                table: "Vehicles",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CurrentMileage",
                table: "Vehicles",
                type: "int",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TireChangeDueMileage",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Mileage",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentMileage",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TireChangeDue",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);
        }
    }
}
