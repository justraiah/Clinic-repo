using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_Clinic.Migrations
{
    /// <inheritdoc />
    public partial class AddPostConsultationVitalSigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PostDiastolicBP",
                table: "VitalSignLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostHeartRate",
                table: "VitalSignLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostOxygenSaturation",
                table: "VitalSignLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostRespiratoryRate",
                table: "VitalSignLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostSystolicBP",
                table: "VitalSignLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PostTemperature",
                table: "VitalSignLogs",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostDiastolicBP",
                table: "VitalSignLogs");

            migrationBuilder.DropColumn(
                name: "PostHeartRate",
                table: "VitalSignLogs");

            migrationBuilder.DropColumn(
                name: "PostOxygenSaturation",
                table: "VitalSignLogs");

            migrationBuilder.DropColumn(
                name: "PostRespiratoryRate",
                table: "VitalSignLogs");

            migrationBuilder.DropColumn(
                name: "PostSystolicBP",
                table: "VitalSignLogs");

            migrationBuilder.DropColumn(
                name: "PostTemperature",
                table: "VitalSignLogs");
        }
    }
}
