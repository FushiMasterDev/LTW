using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomestayBooking.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeRequestWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookingChangeRequestId",
                table: "RoomNights",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChangeRequestId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChangeFeeAmount",
                table: "Bookings",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AdditionalDeposit",
                table: "BookingChangeRequests",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChangeFee",
                table: "BookingChangeRequests",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositCarriedOver",
                table: "BookingChangeRequests",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinanceConfirmedAt",
                table: "BookingChangeRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinanceConfirmedByAccountId",
                table: "BookingChangeRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinanceNote",
                table: "BookingChangeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewRoomId",
                table: "BookingChangeRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NewTotalAmount",
                table: "BookingChangeRequests",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDeadlineAt",
                table: "BookingChangeRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "BookingChangeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedByAccountId",
                table: "BookingChangeRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestType",
                table: "BookingChangeRequests",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingChangeRequests_AspNetUsers_FinanceConfirmedByAccountId",
                table: "BookingChangeRequests",
                column: "FinanceConfirmedByAccountId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingChangeRequests_AspNetUsers_RequestedByAccountId",
                table: "BookingChangeRequests",
                column: "RequestedByAccountId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingChangeRequests_Rooms_NewRoomId",
                table: "BookingChangeRequests",
                column: "NewRoomId",
                principalTable: "Rooms",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_BookingChangeRequests_ChangeRequestId",
                table: "Payments",
                column: "ChangeRequestId",
                principalTable: "BookingChangeRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoomNights_BookingChangeRequests_BookingChangeRequestId",
                table: "RoomNights",
                column: "BookingChangeRequestId",
                principalTable: "BookingChangeRequests",
                principalColumn: "Id");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoomNights_Owner",
                table: "RoomNights");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Decision",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Fields",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Status",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Type",
                table: "BookingChangeRequests");

            // CHECK cũ chưa nhận Applied; chỉ đổi trạng thái đã đóng, giữ nguyên ba Pending cũ.
            migrationBuilder.Sql("UPDATE [BookingChangeRequests] SET [Status] = 'Applied' WHERE [Status] = 'Approved';");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Type",
                table: "BookingChangeRequests",
                sql: "[RequestType] IN ('ChangeDates', 'ChangeRoomType', 'ChangeDatesAndRoomType', 'Cancel')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Status",
                table: "BookingChangeRequests",
                sql: "[Status] IN ('Pending', 'AwaitingPayment', 'Applied', 'Rejected', 'Expired')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Fields",
                table: "BookingChangeRequests",
                sql: "([RequestType] = 'Cancel' AND [NewCheckIn] IS NULL AND [NewCheckOut] IS NULL AND [NewRoomTypeId] IS NULL) OR ([RequestType] = 'ChangeDates' AND [NewCheckIn] IS NOT NULL AND [NewCheckOut] IS NOT NULL AND [NewCheckOut] > [NewCheckIn] AND [NewRoomTypeId] IS NULL) OR ([RequestType] = 'ChangeRoomType' AND [NewRoomTypeId] IS NOT NULL AND [NewCheckIn] IS NULL AND [NewCheckOut] IS NULL) OR ([RequestType] = 'ChangeDatesAndRoomType' AND [NewRoomTypeId] IS NOT NULL AND [NewCheckIn] IS NOT NULL AND [NewCheckOut] IS NOT NULL AND [NewCheckOut] > [NewCheckIn])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Decision",
                table: "BookingChangeRequests",
                sql: "([Status] = 'Pending' AND [DecidedByAccountId] IS NULL AND [DecidedAt] IS NULL) OR ([Status] <> 'Pending' AND [DecidedByAccountId] IS NOT NULL AND [DecidedAt] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoomNights_Owner",
                table: "RoomNights",
                sql: "([BookingId] IS NOT NULL AND [RoomBlockId] IS NULL AND [BookingChangeRequestId] IS NULL) OR ([BookingId] IS NULL AND [RoomBlockId] IS NOT NULL AND [BookingChangeRequestId] IS NULL) OR ([BookingId] IS NULL AND [RoomBlockId] IS NULL AND [BookingChangeRequestId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_ChangeRequest",
                table: "Payments",
                sql: "([Type] = 'AdditionalDeposit' AND [ChangeRequestId] IS NOT NULL) OR ([Type] <> 'AdditionalDeposit' AND [ChangeRequestId] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_ChangeFeeAmount",
                table: "Bookings",
                sql: "[ChangeFeeAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_ActorSeparation",
                table: "BookingChangeRequests",
                sql: "([RequestedByAccountId] IS NULL OR [DecidedByAccountId] IS NULL OR [RequestedByAccountId] <> [DecidedByAccountId]) AND ([RequestedByAccountId] IS NULL OR [FinanceConfirmedByAccountId] IS NULL OR [RequestedByAccountId] <> [FinanceConfirmedByAccountId]) AND ([DecidedByAccountId] IS NULL OR [FinanceConfirmedByAccountId] IS NULL OR [DecidedByAccountId] <> [FinanceConfirmedByAccountId])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Amounts",
                table: "BookingChangeRequests",
                sql: "([ChangeFee] IS NULL OR [ChangeFee] >= 0) AND ([DepositCarriedOver] IS NULL OR [DepositCarriedOver] >= 0) AND ([AdditionalDeposit] IS NULL OR [AdditionalDeposit] >= 0) AND ([NewTotalAmount] IS NULL OR [NewTotalAmount] >= 0) AND ([RefundAmount] IS NULL OR [RefundAmount] >= 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Finance",
                table: "BookingChangeRequests",
                sql: "([Status] = 'Applied' AND [AdditionalDeposit] IS NOT NULL AND [AdditionalDeposit] > 0 AND [FinanceConfirmedByAccountId] IS NOT NULL AND [FinanceConfirmedAt] IS NOT NULL AND [FinanceConfirmedAt] >= [DecidedAt] AND [FinanceNote] IS NOT NULL AND [FinanceNote] <> '') OR (([Status] <> 'Applied' OR [AdditionalDeposit] IS NULL OR [AdditionalDeposit] = 0) AND [FinanceConfirmedByAccountId] IS NULL AND [FinanceConfirmedAt] IS NULL AND [FinanceNote] IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Notes",
                table: "BookingChangeRequests",
                sql: "([Status] = 'Pending' OR ([DecisionNote] IS NOT NULL AND [DecisionNote] <> '')) AND ([Status] <> 'Rejected' OR ([RejectionReason] IS NOT NULL AND [RejectionReason] <> '') OR ([RejectionReason] IS NULL AND [DecisionNote] IS NOT NULL AND [DecisionNote] <> ''))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_PaymentDeadline",
                table: "BookingChangeRequests",
                sql: "([Status] = 'Pending' AND [PaymentDeadlineAt] IS NULL) OR ([Status] IN ('AwaitingPayment', 'Expired') AND [AdditionalDeposit] IS NOT NULL AND [AdditionalDeposit] > 0 AND [PaymentDeadlineAt] IS NOT NULL AND [PaymentDeadlineAt] > [DecidedAt]) OR ([Status] = 'Applied' AND (([AdditionalDeposit] IS NULL AND [PaymentDeadlineAt] IS NULL) OR ([AdditionalDeposit] = 0 AND [PaymentDeadlineAt] IS NULL) OR ([AdditionalDeposit] > 0 AND [PaymentDeadlineAt] IS NOT NULL AND [PaymentDeadlineAt] > [DecidedAt]))) OR ([Status] = 'Rejected' AND ([PaymentDeadlineAt] IS NULL OR ([AdditionalDeposit] IS NOT NULL AND [AdditionalDeposit] > 0 AND [PaymentDeadlineAt] > [DecidedAt])))");

            migrationBuilder.CreateIndex(
                name: "IX_RoomNights_BookingChangeRequestId",
                table: "RoomNights",
                column: "BookingChangeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OneAdditionalDepositPerChangeRequest",
                table: "Payments",
                column: "ChangeRequestId",
                unique: true,
                filter: "[Type] = 'AdditionalDeposit' AND [Status] <> 'Rejected'");

            migrationBuilder.CreateIndex(
                name: "IX_BookingChangeRequests_FinanceConfirmedByAccountId",
                table: "BookingChangeRequests",
                column: "FinanceConfirmedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingChangeRequests_NewRoomId",
                table: "BookingChangeRequests",
                column: "NewRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingChangeRequests_OneActivePerBooking",
                table: "BookingChangeRequests",
                column: "BookingId",
                unique: true,
                filter: "[Status] IN ('Pending', 'AwaitingPayment')");

            migrationBuilder.CreateIndex(
                name: "IX_BookingChangeRequests_RequestedByAccountId",
                table: "BookingChangeRequests",
                column: "RequestedByAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không âm thầm bỏ tiền, giữ phòng hoặc trạng thái mà phiên bản cũ không biểu diễn được.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [BookingChangeRequests]
                    WHERE [Status] IN ('AwaitingPayment', 'Expired') OR [RequestType] = 'ChangeDatesAndRoomType'
                       OR [RequestedByAccountId] IS NOT NULL OR [NewRoomId] IS NOT NULL OR [RejectionReason] IS NOT NULL
                       OR [ChangeFee] IS NOT NULL OR [DepositCarriedOver] IS NOT NULL OR [AdditionalDeposit] IS NOT NULL
                       OR [NewTotalAmount] IS NOT NULL OR [PaymentDeadlineAt] IS NOT NULL
                       OR [FinanceConfirmedByAccountId] IS NOT NULL OR [FinanceConfirmedAt] IS NOT NULL OR [FinanceNote] IS NOT NULL)
                   OR EXISTS (SELECT 1 FROM [Payments] WHERE [Type] = 'AdditionalDeposit' OR [ChangeRequestId] IS NOT NULL)
                   OR EXISTS (SELECT 1 FROM [RoomNights] WHERE [BookingChangeRequestId] IS NOT NULL)
                   OR EXISTS (SELECT 1 FROM [Bookings] WHERE [ChangeFeeAmount] <> 0)
                    THROW 50500, N'Không thể hạ migration khi đã sử dụng luồng đổi/hủy mới; cần xử lý dữ liệu trước.', 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_BookingChangeRequests_AspNetUsers_FinanceConfirmedByAccountId",
                table: "BookingChangeRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingChangeRequests_AspNetUsers_RequestedByAccountId",
                table: "BookingChangeRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingChangeRequests_Rooms_NewRoomId",
                table: "BookingChangeRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_BookingChangeRequests_ChangeRequestId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_RoomNights_BookingChangeRequests_BookingChangeRequestId",
                table: "RoomNights");

            migrationBuilder.DropIndex(
                name: "IX_RoomNights_BookingChangeRequestId",
                table: "RoomNights");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OneAdditionalDepositPerChangeRequest",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_BookingChangeRequests_FinanceConfirmedByAccountId",
                table: "BookingChangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_BookingChangeRequests_NewRoomId",
                table: "BookingChangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_BookingChangeRequests_OneActivePerBooking",
                table: "BookingChangeRequests");

            migrationBuilder.DropIndex(
                name: "IX_BookingChangeRequests_RequestedByAccountId",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RoomNights_Owner",
                table: "RoomNights");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_ChangeRequest",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_ChangeFeeAmount",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_ActorSeparation",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Amounts",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Decision",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Fields",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Finance",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Notes",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_PaymentDeadline",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Status",
                table: "BookingChangeRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BookingChangeRequests_Type",
                table: "BookingChangeRequests");

            migrationBuilder.Sql("UPDATE [BookingChangeRequests] SET [Status] = 'Approved' WHERE [Status] = 'Applied';");

            migrationBuilder.DropColumn(
                name: "BookingChangeRequestId",
                table: "RoomNights");

            migrationBuilder.DropColumn(
                name: "ChangeRequestId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ChangeFeeAmount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AdditionalDeposit",
                table: "BookingChangeRequests");

            migrationBuilder.DropColumn(
                name: "ChangeFee",
                table: "BookingChangeRequests");

            migrationBuilder.DropColumn(
                name: "DepositCarriedOver",
                table: "BookingChangeRequests");

            migrationBuilder.DropColumn(
                name: "FinanceConfirmedAt",
                table: "BookingChangeRequests");

            migrationBuilder.DropColumn(
                name: "FinanceConfirmedByAccountId",
                table: "BookingChangeRequests");

            migrationBuilder.DropColumn(
                name: "FinanceNote",
                table: "BookingChangeRequests");

            migrationBuilder.DropColumn(
                name: "NewRoomId",
                table: "BookingChangeRequests");

            migrationBuilder.DropColumn(
                name: "NewTotalAmount",
                table: "BookingChangeRequests");

            migrationBuilder.DropColumn(
                name: "PaymentDeadlineAt",
                table: "BookingChangeRequests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "BookingChangeRequests");

            migrationBuilder.DropColumn(
                name: "RequestedByAccountId",
                table: "BookingChangeRequests");

            migrationBuilder.AlterColumn<string>(
                name: "RequestType",
                table: "BookingChangeRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AddCheckConstraint(
                name: "CK_RoomNights_Owner",
                table: "RoomNights",
                sql: "([BookingId] IS NOT NULL AND [RoomBlockId] IS NULL) OR ([BookingId] IS NULL AND [RoomBlockId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Decision",
                table: "BookingChangeRequests",
                sql: "([Status] = 'Pending' AND [DecidedAt] IS NULL AND [DecidedByAccountId] IS NULL) OR ([Status] <> 'Pending' AND [DecidedAt] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Fields",
                table: "BookingChangeRequests",
                sql: "([RequestType] <> 'Cancel' OR ([NewCheckIn] IS NULL AND [NewCheckOut] IS NULL AND [NewRoomTypeId] IS NULL)) AND ([RequestType] <> 'ChangeDates' OR ([NewCheckIn] IS NOT NULL AND [NewCheckOut] IS NOT NULL AND [NewCheckOut] > [NewCheckIn])) AND ([RequestType] <> 'ChangeRoomType' OR [NewRoomTypeId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Status",
                table: "BookingChangeRequests",
                sql: "[Status] IN ('Pending', 'Approved', 'Rejected')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BookingChangeRequests_Type",
                table: "BookingChangeRequests",
                sql: "[RequestType] IN ('ChangeDates', 'ChangeRoomType', 'Cancel')");
        }
    }
}
