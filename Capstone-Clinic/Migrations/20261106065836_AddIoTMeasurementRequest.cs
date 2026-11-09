using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Capstone_Clinic.Migrations
{
    /// <inheritdoc />
    public partial class AddIoTMeasurementRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "PendingVitalReadings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IoTMeasurementRequests",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IoTMeasurementRequests", x => x.RequestId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingVitalReadings_RequestId",
                table: "PendingVitalReadings",
                column: "RequestId",
                unique: true,
                filter: "[RequestId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PendingVitalReadings_IoTMeasurementRequests_RequestId",
                table: "PendingVitalReadings",
                column: "RequestId",
                principalTable: "IoTMeasurementRequests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PendingVitalReadings_IoTMeasurementRequests_RequestId",
                table: "PendingVitalReadings");

            migrationBuilder.DropTable(
                name: "IoTMeasurementRequests");

            migrationBuilder.DropIndex(
                name: "IX_PendingVitalReadings_RequestId",
                table: "PendingVitalReadings");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "PendingVitalReadings");
        }
    }
}
