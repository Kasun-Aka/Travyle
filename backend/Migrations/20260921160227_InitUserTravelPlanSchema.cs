using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travyle.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitUserTravelPlanSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Destinations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    AverageRating = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonalizedItineraries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TravelerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    DestinationIds = table.Column<string>(type: "text", nullable: false),
                    PreferencesSnapshot = table.Column<string>(type: "text", nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    EstimatedBudget = table.Column<decimal>(type: "numeric", nullable: false),
                    PdfUrl = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalizedItineraries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalizedItineraries_Users_TravelerId",
                        column: x => x.TravelerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TravelerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferredWeather = table.Column<string[]>(type: "text[]", nullable: false),
                    PreferredActivities = table.Column<string[]>(type: "text[]", nullable: false),
                    PreferredLandscapes = table.Column<string[]>(type: "text[]", nullable: false),
                    BudgetRange = table.Column<string>(type: "text", nullable: false),
                    TripHistory = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalizedItineraries_TravelerId",
                table: "PersonalizedItineraries",
                column: "TravelerId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelerProfiles_UserId",
                table: "TravelerProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Destinations");

            migrationBuilder.DropTable(
                name: "PersonalizedItineraries");

            migrationBuilder.DropTable(
                name: "TravelerProfiles");
        }
    }
}
