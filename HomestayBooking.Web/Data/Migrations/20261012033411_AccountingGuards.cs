using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomestayBooking.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AccountingGuards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Payments_BookingId",
                table: "Payments",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OneRefundPerBooking",
                table: "Payments",
                column: "BookingId",
                unique: true,
                filter: "[Type] = 'Refund'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_BookingId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OneRefundPerBooking",
                table: "Payments");
        }
    }
}
