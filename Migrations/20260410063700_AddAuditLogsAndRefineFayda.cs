using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogsAndRefineFayda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OTPExpiry",
                table: "FaydaVerifications",
                newName: "OtpExpiry");

            migrationBuilder.RenameColumn(
                name: "FaydaId",
                table: "FaydaVerifications",
                newName: "FAN");

            migrationBuilder.RenameColumn(
                name: "AttemptCount",
                table: "FaydaVerifications",
                newName: "ResendCount");

            migrationBuilder.AlterColumn<string>(
                name: "OTP",
                table: "FaydaVerifications",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(6)",
                oldMaxLength: 6,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "FaydaVerifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryTime",
                table: "FaydaVerifications",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "FaydaVerifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedBy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Attempts",
                table: "FaydaVerifications");

            migrationBuilder.DropColumn(
                name: "ExpiryTime",
                table: "FaydaVerifications");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "FaydaVerifications");

            migrationBuilder.RenameColumn(
                name: "OtpExpiry",
                table: "FaydaVerifications",
                newName: "OTPExpiry");

            migrationBuilder.RenameColumn(
                name: "ResendCount",
                table: "FaydaVerifications",
                newName: "AttemptCount");

            migrationBuilder.RenameColumn(
                name: "FAN",
                table: "FaydaVerifications",
                newName: "FaydaId");

            migrationBuilder.AlterColumn<string>(
                name: "OTP",
                table: "FaydaVerifications",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
