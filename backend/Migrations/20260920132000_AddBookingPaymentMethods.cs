using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travyle.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPaymentMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Bookings\" ADD COLUMN IF NOT EXISTS \"PaymentMethod\" text NOT NULL DEFAULT 'SampleCard';");

            migrationBuilder.Sql(
                "ALTER TABLE \"Bookings\" ADD COLUMN IF NOT EXISTS \"ReceiptReference\" text NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Bookings\" DROP COLUMN IF EXISTS \"PaymentMethod\";");
            migrationBuilder.Sql("ALTER TABLE \"Bookings\" DROP COLUMN IF EXISTS \"ReceiptReference\";");
        }
    }
}
