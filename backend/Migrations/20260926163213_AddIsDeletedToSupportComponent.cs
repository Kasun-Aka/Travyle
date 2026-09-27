using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travyle.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeletedToSupportComponent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Vouchers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SupportTickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DisruptionAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisruptionAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuideAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuideUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuideAssignments_Users_GuideUserId",
                        column: x => x.GuideUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RouteLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric", nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteLogs_Users_RecordedBy",
                        column: x => x.RecordedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TourActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityName = table.Column<string>(type: "text", nullable: false),
                    ScheduledTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourActivities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DisruptionAlerts_BookingScheduleId_ResolvedAt",
                table: "DisruptionAlerts",
                columns: new[] { "BookingScheduleId", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GuideAssignments_GuideUserId",
                table: "GuideAssignments",
                column: "GuideUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteLogs_BookingScheduleId_Timestamp",
                table: "RouteLogs",
                columns: new[] { "BookingScheduleId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_RouteLogs_RecordedBy",
                table: "RouteLogs",
                column: "RecordedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisruptionAlerts");

            migrationBuilder.DropTable(
                name: "GuideAssignments");

            migrationBuilder.DropTable(
                name: "RouteLogs");

            migrationBuilder.DropTable(
                name: "TourActivities");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SupportTickets");
        }
    }
}
