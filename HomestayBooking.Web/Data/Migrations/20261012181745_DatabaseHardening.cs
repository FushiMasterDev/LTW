using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomestayBooking.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "HomestayImages",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Settlements_Sent",
                table: "Settlements",
                sql: "[Status] = 'Draft' OR [SentAt] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SettlementItems_CommissionRate",
                table: "SettlementItems",
                sql: "[CommissionRate] > 0 AND [CommissionRate] < 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Receipt",
                table: "Payments",
                sql: "[Method] <> 'ManualReceipt' OR ([ReceiptImageUrl] IS NOT NULL AND [ReceiptImageUrl] <> '')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_CommissionRate",
                table: "Bookings",
                sql: "[CommissionRate] > 0 AND [CommissionRate] < 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_DepositRate",
                table: "Bookings",
                sql: "[DepositRate] > 0 AND [DepositRate] <= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Status",
                table: "BookingChangeRequests",
                sql: "[Status] IN ('Pending', 'Approved', 'Rejected')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Type",
                table: "BookingChangeRequests",
                sql: "[RequestType] IN ('ChangeDates', 'ChangeRoomType', 'Cancel')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Settlements_Sent",
                table: "Settlements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SettlementItems_CommissionRate",
                table: "SettlementItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Receipt",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_CommissionRate",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_DepositRate",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Status",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Type",
                table: "BookingChangeRequests");

            migrationBuilder.AlterColumn<int>(
                name: "SortOrder",
                table: "HomestayImages",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);
        }
    }
}
