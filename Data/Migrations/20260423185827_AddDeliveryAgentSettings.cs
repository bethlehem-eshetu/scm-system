using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCM_System.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAgentSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnDuty",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "WorkingHoursStart",
                table: "SupplierEmployees",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "WorkingHoursEnd",
                table: "SupplierEmployees",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxDailyDeliveries",
                table: "SupplierEmployees",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<bool>(
                name: "RequireProofPhoto",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireSignature",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoAcceptAssignments",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowNightDeliveries",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyNewAssignment",
                table: "SupplierEmployees",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SmsNotificationNumber",
                table: "SupplierEmployees",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsOnDuty", table: "SupplierEmployees");
            migrationBuilder.DropColumn(name: "WorkingHoursStart", table: "SupplierEmployees");
            migrationBuilder.DropColumn(name: "WorkingHoursEnd", table: "SupplierEmployees");
            migrationBuilder.DropColumn(name: "MaxDailyDeliveries", table: "SupplierEmployees");
            migrationBuilder.DropColumn(name: "RequireProofPhoto", table: "SupplierEmployees");
            migrationBuilder.DropColumn(name: "RequireSignature", table: "SupplierEmployees");
            migrationBuilder.DropColumn(name: "AutoAcceptAssignments", table: "SupplierEmployees");
            migrationBuilder.DropColumn(name: "AllowNightDeliveries", table: "SupplierEmployees");
            migrationBuilder.DropColumn(name: "NotifyNewAssignment", table: "SupplierEmployees");
            migrationBuilder.DropColumn(name: "SmsNotificationNumber", table: "SupplierEmployees");
        }
    }
}
