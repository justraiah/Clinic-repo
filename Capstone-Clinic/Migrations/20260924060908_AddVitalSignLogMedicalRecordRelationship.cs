using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_Clinic.Migrations
{
    /// <inheritdoc />
    public partial class AddVitalSignLogMedicalRecordRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VitalSignLogs_MedicalRecordId",
                table: "VitalSignLogs",
                column: "MedicalRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_VitalSignLogs_MedicalRecords_MedicalRecordId",
                table: "VitalSignLogs",
                column: "MedicalRecordId",
                principalTable: "MedicalRecords",
                principalColumn: "MedicalRecordId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VitalSignLogs_MedicalRecords_MedicalRecordId",
                table: "VitalSignLogs");

            migrationBuilder.DropIndex(
                name: "IX_VitalSignLogs_MedicalRecordId",
                table: "VitalSignLogs");
        }
    }
}
