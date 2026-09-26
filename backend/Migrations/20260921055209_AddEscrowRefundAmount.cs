using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Travyle.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEscrowRefundAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "PaymentEscrows",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundedAmount",
                table: "PaymentEscrows");
        }
    }
}
