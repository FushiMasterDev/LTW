using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomestayBooking.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnumAndReviewChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_VerificationDocuments_DocumentType",
                table: "VerificationDocuments",
                sql: "[DocumentType] IN ('BusinessLicense', 'FireSafety', 'PropertyPhoto', 'Other')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Settlements_Status",
                table: "Settlements",
                sql: "[Status] IN ('Draft', 'Sent', 'Confirmed', 'Paid')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Settlements_Year",
                table: "Settlements",
                sql: "[Year] BETWEEN 2020 AND 2100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SettlementItems_ItemType",
                table: "SettlementItems",
                sql: "[ItemType] IN ('Completed', 'CancelledRetained', 'NoShow')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoomTypePrices_DayType",
                table: "RoomTypePrices",
                sql: "[DayType] IN ('Weekday', 'Weekend', 'Holiday')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoomBlocks_Reason",
                table: "RoomBlocks",
                sql: "[Reason] IN ('OtherChannel', 'Maintenance')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reviews_Reply",
                table: "Reviews",
                sql: "([OwnerReply] IS NULL AND [RepliedAt] IS NULL) OR ([OwnerReply] IS NOT NULL AND [OwnerReply] <> '' AND [RepliedAt] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Method",
                table: "Payments",
                sql: "[Method] IN ('BankAutoMatch', 'ManualReceipt', 'ManualTransfer')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Status",
                table: "Payments",
                sql: "[Status] IN ('Submitted', 'Approved', 'Rejected')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Type",
                table: "Payments",
                sql: "[Type] IN ('Deposit', 'Refund', 'Compensation', 'AdditionalDeposit')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OwnerRegistrations_Decision",
                table: "OwnerRegistrations",
                sql: "([Status] NOT IN ('NeedsRevision', 'Approved', 'Rejected') OR ([ReviewedByEmployeeId] IS NOT NULL AND [ReviewedAt] IS NOT NULL)) AND ([Status] NOT IN ('NeedsRevision', 'Rejected') OR ([ReviewNote] IS NOT NULL AND [ReviewNote] <> ''))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OwnerRegistrations_Status",
                table: "OwnerRegistrations",
                sql: "[Status] IN ('Draft', 'WaitingOwnerActivation', 'PendingReview', 'UnderReview', 'NeedsRevision', 'Approved', 'Rejected')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Homestays_Status",
                table: "Homestays",
                sql: "[Status] IN ('Draft', 'PendingReview', 'Active', 'Suspended', 'Ended')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Contracts_Status",
                table: "Contracts",
                sql: "[Status] IN ('Active', 'Expired', 'Terminated')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CancellationPolicies_Scope",
                table: "CancellationPolicies",
                sql: "[Scope] IN ('All', 'Holiday')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingNights_DayType",
                table: "BookingNights",
                sql: "[DayType] IN ('Weekday', 'Weekend', 'Holiday')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_VerificationDocuments_DocumentType",
                table: "VerificationDocuments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Settlements_Status",
                table: "Settlements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Settlements_Year",
                table: "Settlements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SettlementItems_ItemType",
                table: "SettlementItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoomTypePrices_DayType",
                table: "RoomTypePrices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoomBlocks_Reason",
                table: "RoomBlocks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reviews_Reply",
                table: "Reviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Method",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Status",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Type",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OwnerRegistrations_Decision",
                table: "OwnerRegistrations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OwnerRegistrations_Status",
                table: "OwnerRegistrations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Homestays_Status",
                table: "Homestays");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Contracts_Status",
                table: "Contracts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CancellationPolicies_Scope",
                table: "CancellationPolicies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingNights_DayType",
                table: "BookingNights");
        }
    }
}
