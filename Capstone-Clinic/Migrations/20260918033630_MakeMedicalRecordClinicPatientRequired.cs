using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_Clinic.Migrations
{
    /// <inheritdoc />
    public partial class MakeMedicalRecordClinicPatientRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalRecords_ClinicPatients_ClinicPatientId",
                table: "MedicalRecords");

            migrationBuilder.AlterColumn<int>(
                name: "ClinicPatientId",
                table: "MedicalRecords",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalRecords_ClinicPatients_ClinicPatientId",
                table: "MedicalRecords",
                column: "ClinicPatientId",
                principalTable: "ClinicPatients",
                principalColumn: "ClinicPatientId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalRecords_ClinicPatients_ClinicPatientId",
                table: "MedicalRecords");

            migrationBuilder.AlterColumn<int>(
                name: "ClinicPatientId",
                table: "MedicalRecords",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalRecords_ClinicPatients_ClinicPatientId",
                table: "MedicalRecords",
                column: "ClinicPatientId",
                principalTable: "ClinicPatients",
                principalColumn: "ClinicPatientId");
        }
    }
}
