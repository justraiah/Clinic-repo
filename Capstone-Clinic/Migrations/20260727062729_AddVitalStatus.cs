using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_Clinic.Migrations
{
    /// <inheritdoc />
    public partial class AddVitalStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "VitalSignLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "VitalSignLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "VitalSignLogs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "VitalSignLogs");
        }
    }
}
