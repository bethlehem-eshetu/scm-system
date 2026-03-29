using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderRejectionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Orders");
        }
    }
}
