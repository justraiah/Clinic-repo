using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_Clinic.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalRecordClinicPatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClinicPatientId",
                table: "MedicalRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_ClinicPatientId",
                table: "MedicalRecords",
                column: "ClinicPatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalRecords_ClinicPatients_ClinicPatientId",
                table: "MedicalRecords",
                column: "ClinicPatientId",
                principalTable: "ClinicPatients",
                principalColumn: "ClinicPatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalRecords_ClinicPatients_ClinicPatientId",
                table: "MedicalRecords");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_ClinicPatientId",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "ClinicPatientId",
                table: "MedicalRecords");
        }
    }
}
