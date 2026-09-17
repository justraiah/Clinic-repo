using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_Clinic.Migrations
{
    /// <inheritdoc />
    public partial class MakeClinicPatientStudentUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicPatients_StudentId",
                table: "ClinicPatients");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicPatients_StudentId",
                table: "ClinicPatients",
                column: "StudentId",
                unique: true,
                filter: "[StudentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicPatients_StudentId",
                table: "ClinicPatients");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicPatients_StudentId",
                table: "ClinicPatients",
                column: "StudentId");
        }
    }
}
