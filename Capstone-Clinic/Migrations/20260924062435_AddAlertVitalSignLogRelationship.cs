using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_Clinic.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertVitalSignLogRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Alerts_VitalLogId",
                table: "Alerts",
                column: "VitalLogId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_VitalSignLogs_VitalLogId",
                table: "Alerts",
                column: "VitalLogId",
                principalTable: "VitalSignLogs",
                principalColumn: "VitalSignLogId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_VitalSignLogs_VitalLogId",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_VitalLogId",
                table: "Alerts");
        }
    }
}
