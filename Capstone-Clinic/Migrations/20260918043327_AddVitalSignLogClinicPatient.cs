using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_Clinic.Migrations
{
    /// <inheritdoc />
    public partial class AddVitalSignLogClinicPatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClinicPatientId",
                table: "VitalSignLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignLogs_ClinicPatientId",
                table: "VitalSignLogs",
                column: "ClinicPatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_VitalSignLogs_ClinicPatients_ClinicPatientId",
                table: "VitalSignLogs",
                column: "ClinicPatientId",
                principalTable: "ClinicPatients",
                principalColumn: "ClinicPatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VitalSignLogs_ClinicPatients_ClinicPatientId",
                table: "VitalSignLogs");

            migrationBuilder.DropIndex(
                name: "IX_VitalSignLogs_ClinicPatientId",
                table: "VitalSignLogs");

            migrationBuilder.DropColumn(
                name: "ClinicPatientId",
                table: "VitalSignLogs");
        }
    }
}
