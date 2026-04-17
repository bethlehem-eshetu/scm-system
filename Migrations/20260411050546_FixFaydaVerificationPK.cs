using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class FixFaydaVerificationPK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_FaydaVerifications",
                table: "FaydaVerifications");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "FaydaVerifications");

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedDob",
                table: "FaydaVerifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedName",
                table: "FaydaVerifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedPhone",
                table: "FaydaVerifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FaydaVerifications",
                table: "FaydaVerifications",
                column: "FAN");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_FaydaVerifications_FAN",
                table: "Users",
                column: "FAN",
                principalTable: "FaydaVerifications",
                principalColumn: "FAN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_FaydaVerifications_FAN",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FaydaVerifications",
                table: "FaydaVerifications");

            migrationBuilder.DropColumn(
                name: "VerifiedDob",
                table: "FaydaVerifications");

            migrationBuilder.DropColumn(
                name: "VerifiedName",
                table: "FaydaVerifications");

            migrationBuilder.DropColumn(
                name: "VerifiedPhone",
                table: "FaydaVerifications");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "FaydaVerifications",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FaydaVerifications",
                table: "FaydaVerifications",
                column: "Id");
        }
    }
}
