using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class SyncMessageModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AppealDate",
                table: "Penalties",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppealReason",
                table: "Penalties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppealResponse",
                table: "Penalties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AppealResponseDate",
                table: "Penalties",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAppealed",
                table: "Penalties",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "IssuedByAdminId",
                table: "Penalties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MessageId",
                table: "Penalties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MessageId1",
                table: "Penalties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Penalties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserType",
                table: "Penalties",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "BlockedAt",
                table: "Messages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlockedReason",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PenaltyId",
                table: "Messages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TriggeredPenalty",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_IssuedByAdminId",
                table: "Penalties",
                column: "IssuedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_MessageId1",
                table: "Penalties",
                column: "MessageId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Penalties_Messages_MessageId1",
                table: "Penalties",
                column: "MessageId1",
                principalTable: "Messages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Penalties_Users_IssuedByAdminId",
                table: "Penalties",
                column: "IssuedByAdminId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Penalties_Messages_MessageId1",
                table: "Penalties");

            migrationBuilder.DropForeignKey(
                name: "FK_Penalties_Users_IssuedByAdminId",
                table: "Penalties");

            migrationBuilder.DropIndex(
                name: "IX_Penalties_IssuedByAdminId",
                table: "Penalties");

            migrationBuilder.DropIndex(
                name: "IX_Penalties_MessageId1",
                table: "Penalties");

            migrationBuilder.DropColumn(
                name: "AppealDate",
                table: "Penalties");

            migrationBuilder.DropColumn(
                name: "AppealReason",
                table: "Penalties");

            migrationBuilder.DropColumn(
                name: "AppealResponse",
                table: "Penalties");

            migrationBuilder.DropColumn(
                name: "AppealResponseDate",
                table: "Penalties");

            migrationBuilder.DropColumn(
                name: "HasAppealed",
                table: "Penalties");

            migrationBuilder.DropColumn(
                name: "IssuedByAdminId",
                table: "Penalties");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "Penalties");

            migrationBuilder.DropColumn(
                name: "MessageId1",
                table: "Penalties");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Penalties");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "Penalties");

            migrationBuilder.DropColumn(
                name: "BlockedAt",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "BlockedReason",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PenaltyId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "TriggeredPenalty",
                table: "Messages");
        }
    }
}
