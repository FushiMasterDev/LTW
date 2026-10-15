using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HomestayBooking.Tests.Data;

public sealed partial class DatabaseConstraintTests
{
    private async Task<ApplicationUser> WorkflowAccountAsync(string suffix)
    {
        var user = new ApplicationUser { Person = new PersonProfile { PhoneNumber = "+84901999" + suffix } };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task PopulateDecisionAsync(BookingChangeRequest request)
    {
        if (request.Status == ChangeRequestStatus.Pending) return;
        request.DecidedByAccountId = (await WorkflowAccountAsync("001")).Id;
        request.DecidedAt ??= DateTime.UtcNow;
        request.DecisionNote = "Quyết định thử.";
        if (request.Status is ChangeRequestStatus.AwaitingPayment or ChangeRequestStatus.Expired)
        {
            request.AdditionalDeposit = 1;
            request.PaymentDeadlineAt = request.DecidedAt.Value.AddMinutes(15);
        }
    }

    // Type/Status sai đồng thời vi phạm Fields/PaymentDeadline. Tách đúng biểu thức từ model
    // sang bảng SQLite thử để xác nhận tên riêng, không phụ thuộc thứ tự đánh giá nhiều CHECK.
    private async Task AssertEnumCheckAsync(string constraint, string column, string valid, string invalid)
    {
        var model = _db.GetService<IDesignTimeModel>().Model;
        var sql = model.FindEntityType(typeof(BookingChangeRequest))!.GetCheckConstraints().Single(c => c.Name == constraint).Sql;
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE TABLE CheckProbe ([{column}] TEXT NOT NULL, CONSTRAINT [{constraint}] CHECK ({sql}));";
        await command.ExecuteNonQueryAsync();
        command.CommandText = $"INSERT INTO CheckProbe ([{column}]) VALUES ($value);";
        command.Parameters.AddWithValue("$value", valid);
        await command.ExecuteNonQueryAsync();
        command.CommandText = $"UPDATE CheckProbe SET [{column}] = $value;";
        command.Parameters["$value"].Value = invalid;
        var ex = await Assert.ThrowsAsync<SqliteException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(275, ex.SqliteExtendedErrorCode);
        Assert.Contains(constraint, ex.Message);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public async Task Booking_ChangeFeeAmountIsNonnegative(int value, bool allowed)
    {
        var booking = NewBooking("WF-FEE", new(2026, 12, 1), new(2026, 12, 2));
        booking.ChangeFeeAmount = value;
        if (allowed) await AssertAcceptedAsync(booking);
        else await AssertRejectedByAsync(booking, "CK_Bookings_ChangeFeeAmount");
    }

    [Theory]
    [InlineData("ChangeFee", -1, false)]
    [InlineData("ChangeFee", 0, true)]
    [InlineData("DepositCarriedOver", -1, false)]
    [InlineData("DepositCarriedOver", 0, true)]
    [InlineData("AdditionalDeposit", -1, false)]
    [InlineData("AdditionalDeposit", 0, true)]
    [InlineData("NewTotalAmount", -1, false)]
    [InlineData("NewTotalAmount", 0, true)]
    [InlineData("RefundAmount", -1, false)]
    [InlineData("RefundAmount", 0, true)]
    public async Task ChangeRequest_AmountsAreNonnegativeWhenKnown(string property, int value, bool allowed)
    {
        var request = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel);
        typeof(BookingChangeRequest).GetProperty(property)!.SetValue(request, (decimal?)value);
        if (allowed) await AssertAcceptedAsync(request);
        else await AssertRejectedByAsync(request, "CK_BookingChangeRequests_Amounts");
    }

    [Theory]
    [InlineData("legacy", true)]
    [InlineData("zero", true)]
    [InlineData("complete", true)]
    [InlineData("late", true)]
    [InlineData("missingAccount", false)]
    [InlineData("missingTime", false)]
    [InlineData("missingNote", false)]
    [InlineData("emptyNote", false)]
    [InlineData("beforeDecision", false)]
    [InlineData("premature", false)]
    public async Task ChangeRequest_FinanceMatchesAppliedAdditionalDeposit(string scenario, bool allowed)
    {
        var request = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel);
        request.Status = ChangeRequestStatus.Applied;
        await PopulateDecisionAsync(request);
        if (scenario == "zero") request.AdditionalDeposit = 0;
        if (scenario is not ("legacy" or "zero"))
        {
            request.AdditionalDeposit = 1;
            request.PaymentDeadlineAt = request.DecidedAt!.Value.AddMinutes(15);
            request.FinanceConfirmedByAccountId = (await WorkflowAccountAsync("002")).Id;
            request.FinanceConfirmedAt = scenario == "late" ? request.PaymentDeadlineAt.Value.AddDays(1) : request.DecidedAt;
            request.FinanceNote = "Đã nhận đủ tiền.";
        }
        if (scenario == "missingAccount") request.FinanceConfirmedByAccountId = null;
        if (scenario == "missingTime") request.FinanceConfirmedAt = null;
        if (scenario == "missingNote") request.FinanceNote = null;
        if (scenario == "emptyNote") request.FinanceNote = "";
        if (scenario == "beforeDecision") request.FinanceConfirmedAt = request.DecidedAt!.Value.AddTicks(-1);
        if (scenario == "premature") request.Status = ChangeRequestStatus.AwaitingPayment;
        if (allowed) await AssertAcceptedAsync(request);
        else await AssertRejectedByAsync(request, "CK_BookingChangeRequests_Finance");
    }

    [Theory]
    [InlineData("different", true)]
    [InlineData("senderManager", false)]
    [InlineData("senderFinance", false)]
    [InlineData("managerFinance", false)]
    public async Task ChangeRequest_ActorsAreSeparated(string scenario, bool allowed)
    {
        var request = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel);
        request.Status = ChangeRequestStatus.Applied;
        await PopulateDecisionAsync(request);
        request.RequestedByAccountId = (await WorkflowAccountAsync("003")).Id;
        request.FinanceConfirmedByAccountId = (await WorkflowAccountAsync("002")).Id;
        request.AdditionalDeposit = 1;
        request.PaymentDeadlineAt = request.DecidedAt!.Value.AddMinutes(15);
        request.FinanceConfirmedAt = request.DecidedAt;
        request.FinanceNote = "Đã xác nhận.";
        if (scenario == "senderManager") request.RequestedByAccountId = request.DecidedByAccountId;
        if (scenario == "senderFinance") request.RequestedByAccountId = request.FinanceConfirmedByAccountId;
        if (scenario == "managerFinance") request.FinanceConfirmedByAccountId = request.DecidedByAccountId;
        if (allowed) await AssertAcceptedAsync(request);
        else await AssertRejectedByAsync(request, "CK_BookingChangeRequests_ActorSeparation");
    }

    [Theory]
    [InlineData("decisionNull", false)]
    [InlineData("decisionEmpty", false)]
    [InlineData("reasonEmpty", false)]
    [InlineData("legacyReason", true)]
    [InlineData("complete", true)]
    public async Task ChangeRequest_NotesRequireContent(string scenario, bool allowed)
    {
        var request = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel);
        request.Status = ChangeRequestStatus.Rejected;
        await PopulateDecisionAsync(request);
        request.RejectionReason = "Không còn phòng.";
        if (scenario == "decisionNull") request.DecisionNote = null;
        if (scenario == "decisionEmpty") request.DecisionNote = "";
        if (scenario == "reasonEmpty") request.RejectionReason = "";
        if (scenario == "legacyReason") request.RejectionReason = null;
        if (allowed) await AssertAcceptedAsync(request);
        else await AssertRejectedByAsync(request, "CK_BookingChangeRequests_Notes");
    }

    [Theory]
    [InlineData("missing", false)]
    [InlineData("equal", false)]
    [InlineData("before", false)]
    [InlineData("zero", false)]
    [InlineData("unknown", false)]
    [InlineData("positive", true)]
    [InlineData("expired", true)]
    [InlineData("pendingDeadline", false)]
    public async Task ChangeRequest_DeadlineIsCustomerSubmissionLimit(string scenario, bool allowed)
    {
        var request = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel);
        request.Status = scenario == "expired" ? ChangeRequestStatus.Expired : ChangeRequestStatus.AwaitingPayment;
        await PopulateDecisionAsync(request);
        if (scenario == "missing") request.PaymentDeadlineAt = null;
        if (scenario == "equal") request.PaymentDeadlineAt = request.DecidedAt;
        if (scenario == "before") request.PaymentDeadlineAt = request.DecidedAt!.Value.AddTicks(-1);
        if (scenario == "zero") request.AdditionalDeposit = 0;
        if (scenario == "unknown") request.AdditionalDeposit = null;
        if (scenario == "pendingDeadline")
        {
            request.Status = ChangeRequestStatus.Pending;
            request.DecidedAt = null; request.DecidedByAccountId = null;
        }
        if (allowed) await AssertAcceptedAsync(request);
        else await AssertRejectedByAsync(request, "CK_BookingChangeRequests_PaymentDeadline");
    }

    [Theory]
    [InlineData(PaymentType.AdditionalDeposit, true, true)]
    [InlineData(PaymentType.AdditionalDeposit, false, false)]
    [InlineData(PaymentType.Deposit, true, false)]
    [InlineData(PaymentType.Deposit, false, true)]
    public async Task Payment_AdditionalDepositRequiresRequest(PaymentType type, bool linked, bool allowed)
    {
        var request = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel);
        await AssertAcceptedAsync(request);
        var payment = Deposit(request.BookingId, PaymentStatus.Submitted);
        payment.Type = type;
        payment.ChangeRequestId = linked ? request.Id : null;
        if (allowed) await AssertAcceptedAsync(payment);
        else await AssertRejectedByAsync(payment, "CK_Payments_ChangeRequest");
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(4, true)]
    [InlineData(0, false)]
    [InlineData(3, false)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(7, false)]
    public async Task RoomNight_ExactlyOneOfThreeOwners(int mask, bool allowed)
    {
        var request = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel);
        await AssertAcceptedAsync(request);
        var block = new RoomBlock { RoomId = _room.Id, StartDate = new(2026, 12, 1), EndDate = new(2026, 12, 2),
            CreatedByAccountId = (await WorkflowAccountAsync("004")).Id };
        await AssertAcceptedAsync(block);
        var night = new RoomNight { RoomId = _room.Id, Date = new(2026, 12, 1),
            BookingId = (mask & 1) != 0 ? request.BookingId : null,
            RoomBlockId = (mask & 2) != 0 ? block.Id : null,
            BookingChangeRequestId = (mask & 4) != 0 ? request.Id : null };
        if (allowed) await AssertAcceptedAsync(night);
        else await AssertRejectedByAsync(night, "CK_RoomNights_Owner");
    }

    // SQLite báo cột bị trùng thay vì tên index; đối chiếu index model cùng mã lỗi unique thật.
    private async Task AssertUniqueRejectedAsync(object entity, string indexName, string columns)
    {
        var indexes = _db.GetService<IDesignTimeModel>().Model.GetEntityTypes().SelectMany(e => e.GetIndexes());
        Assert.True(indexes.Single(i => i.GetDatabaseName() == indexName).IsUnique);
        _db.ChangeTracker.Clear(); _db.Add(entity);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
        var sqlite = Assert.IsType<SqliteException>(ex.InnerException);
        Assert.Equal(2067, sqlite.SqliteExtendedErrorCode);
        Assert.Contains(columns, sqlite.Message);
    }

    [Theory]
    [InlineData(ChangeRequestStatus.Pending, false)]
    [InlineData(ChangeRequestStatus.AwaitingPayment, false)]
    [InlineData(ChangeRequestStatus.Applied, true)]
    [InlineData(ChangeRequestStatus.Rejected, true)]
    [InlineData(ChangeRequestStatus.Expired, true)]
    public async Task ChangeRequest_OneOpenRequestPerBooking(ChangeRequestStatus firstStatus, bool allowed)
    {
        var first = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel);
        first.Status = firstStatus;
        await PopulateDecisionAsync(first);
        await AssertAcceptedAsync(first);
        var second = Request(first.BookingId, ChangeRequestType.Cancel);
        if (allowed) await AssertAcceptedAsync(second);
        else await AssertUniqueRejectedAsync(second, "IX_BookingChangeRequests_OneActivePerBooking", "BookingChangeRequests.BookingId");
    }

    [Theory]
    [InlineData(PaymentStatus.Submitted, false)]
    [InlineData(PaymentStatus.Approved, false)]
    [InlineData(PaymentStatus.Rejected, true)]
    public async Task Payment_OneNonRejectedAdditionalDepositPerRequest(PaymentStatus firstStatus, bool allowed)
    {
        var request = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel);
        await AssertAcceptedAsync(request);
        Payment Additional(PaymentStatus status)
        {
            var p = Deposit(request.BookingId, status);
            p.Type = PaymentType.AdditionalDeposit; p.ChangeRequestId = request.Id;
            return p;
        }
        await AssertAcceptedAsync(Additional(firstStatus));
        if (allowed) await AssertAcceptedAsync(Additional(PaymentStatus.Submitted));
        else await AssertUniqueRejectedAsync(Additional(PaymentStatus.Submitted), "IX_Payments_OneAdditionalDepositPerChangeRequest", "Payments.ChangeRequestId");
    }

    [Fact]
    public async Task Payment_InitialAndAdditionalDepositsCoexist()
    {
        var request = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel);
        await AssertAcceptedAsync(request);
        await AssertAcceptedAsync(Deposit(request.BookingId, PaymentStatus.Approved));
        var additional = Deposit(request.BookingId, PaymentStatus.Submitted);
        additional.Type = PaymentType.AdditionalDeposit; additional.ChangeRequestId = request.Id;
        await AssertAcceptedAsync(additional);
        Assert.Equal(2, await _db.Payments.CountAsync());
    }

    [Fact]
    public async Task ChangeRequest_StatusConcurrencyRejectsSecondDecision()
    {
        var request = Request(await SavedBookingIdAsync(), ChangeRequestType.Cancel);
        var decider = await WorkflowAccountAsync("005");
        await AssertAcceptedAsync(request);
        await using var other = new SqliteTestDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        var stale = await other.BookingChangeRequests.SingleAsync();
        var first = await _db.BookingChangeRequests.SingleAsync();
        foreach (var r in new[] { first, stale })
        {
            r.Status = ChangeRequestStatus.Rejected; r.DecidedByAccountId = decider.Id; r.DecidedAt = DateTime.UtcNow;
            r.DecisionNote = r.RejectionReason = "Không còn phòng.";
        }
        await _db.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => other.SaveChangesAsync());
    }
}
