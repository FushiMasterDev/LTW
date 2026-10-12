using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomestayBooking.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReviewFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "Settlements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaidBankRef",
                table: "Settlements",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAt",
                table: "Settlements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "SettlementItems",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RefundBankRef",
                table: "BankTransactions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "BankTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Settlements_Amounts",
                table: "Settlements",
                sql: "[TotalRoomAmount] >= 0 AND [TotalRetained] >= 0 AND [TotalCommission] >= 0 AND [TotalCommission] <= [TotalRetained] AND [PayoutAmount] = [TotalRetained] - [TotalCommission]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Settlements_Paid",
                table: "Settlements",
                sql: "[Status] <> 'Paid' OR [PaidAt] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SettlementItems_Amounts",
                table: "SettlementItems",
                sql: "[RoomAmount] >= 0 AND [DepositAmount] >= 0 AND [RefundAmount] >= 0 AND [DiscountAmount] >= 0 AND [RetainedAmount] > 0 AND [RetainedAmount] <= [DepositAmount] AND [CommissionAmount] >= 0 AND [CommissionAmount] <= [RetainedAmount] AND [PayoutAmount] = [RetainedAmount] - [CommissionAmount]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_Refund",
                table: "Bookings",
                sql: "[RefundAmount] IS NULL OR ([RefundAmount] >= 0 AND [RefundAmount] <= [DepositAmount] - [DiscountAmount])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_Voucher",
                table: "Bookings",
                sql: "([VoucherId] IS NULL AND [DiscountAmount] = 0) OR ([VoucherId] IS NOT NULL AND [DiscountAmount] > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BankTransactions_Refunded",
                table: "BankTransactions",
                sql: "[MatchStatus] <> 'Refunded' OR [RefundedAt] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Settlements_Amounts",
                table: "Settlements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Settlements_Paid",
                table: "Settlements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SettlementItems_Amounts",
                table: "SettlementItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_Refund",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_Voucher",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BankTransactions_Refunded",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "PaidBankRef",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "SettlementItems");

            migrationBuilder.DropColumn(
                name: "RefundBankRef",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "BankTransactions");
        }
    }
}
