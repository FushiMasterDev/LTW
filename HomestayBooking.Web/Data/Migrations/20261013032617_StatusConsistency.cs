using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomestayBooking.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class StatusConsistency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_ConfirmedAt",
                table: "Bookings",
                sql: "([Status] IN ('Confirmed', 'CheckedIn', 'Completed', 'NoShow') AND [ConfirmedAt] IS NOT NULL) OR ([Status] IN ('AwaitingPayment', 'PaymentSubmitted', 'Deposited', 'Rejected', 'Expired') AND [ConfirmedAt] IS NULL) OR [Status] = 'Cancelled'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Decision",
                table: "BookingChangeRequests",
                sql: "([Status] = 'Pending' AND [DecidedAt] IS NULL AND [DecidedByAccountId] IS NULL) OR ([Status] <> 'Pending' AND [DecidedAt] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Fields",
                table: "BookingChangeRequests",
                sql: "([RequestType] <> 'Cancel' OR ([NewCheckIn] IS NULL AND [NewCheckOut] IS NULL AND [NewRoomTypeId] IS NULL)) AND ([RequestType] <> 'ChangeDates' OR ([NewCheckIn] IS NOT NULL AND [NewCheckOut] IS NOT NULL AND [NewCheckOut] > [NewCheckIn])) AND ([RequestType] <> 'ChangeRoomType' OR [NewRoomTypeId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_RequestedBy",
                table: "BookingChangeRequests",
                sql: "[RequestedByType] IN ('Customer', 'Employee')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_ConfirmedAt",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Decision",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Fields",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_RequestedBy",
                table: "BookingChangeRequests");
        }
    }
}
