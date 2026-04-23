using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NotificationEmail",
                table: "Users",
                newName: "SecondaryNotificationEmail");

            migrationBuilder.AddColumn<bool>(
                name: "ReceiveSystemAlerts",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiveSystemAlerts",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "SecondaryNotificationEmail",
                table: "Users",
                newName: "NotificationEmail");
        }
    }
}
