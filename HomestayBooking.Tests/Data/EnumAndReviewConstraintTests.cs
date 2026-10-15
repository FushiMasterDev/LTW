using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Data;

public sealed partial class DatabaseConstraintTests
{
    public static IEnumerable<object[]> WhitelistCases()
    {
        (string Table, string Column, Type Enum)[] columns =
        [
            ("Settlements", "Status", typeof(SettlementStatus)),
            ("SettlementItems", "ItemType", typeof(SettlementItemType)),
            ("Contracts", "Status", typeof(ContractStatus)),
            ("Homestays", "Status", typeof(HomestayStatus)),
            ("RoomTypePrices", "DayType", typeof(DayType)),
            ("BookingNights", "DayType", typeof(DayType)),
            ("RoomBlocks", "Reason", typeof(RoomBlockReason)),
            ("OwnerRegistrations", "Status", typeof(OwnerRegistrationStatus)),
            ("VerificationDocuments", "DocumentType", typeof(DocumentType)),
            ("CancellationPolicies", "Scope", typeof(PolicyScope)),
            ("Payments", "Type", typeof(PaymentType)),
            ("Payments", "Method", typeof(PaymentMethod)),
            ("Payments", "Status", typeof(PaymentStatus)),
        ];
        foreach (var (table, column, type) in columns)
        {
            foreach (var value in Enum.GetValues(type))
                yield return [table, column, value, true];
            yield return [table, column, Enum.ToObject(type, 999), false];
        }
    }

    [Theory]
    [MemberData(nameof(WhitelistCases))]
    public async Task EnumWhitelist_AcceptsEveryMemberAndRejectsUnknown(string table, string column, object value, bool allowed)
    {
        var now = DateTime.UtcNow;
        object entity;
        switch (table)
        {
            case "Settlements":
                // Paid được phép đi thẳng từ Sent, nên không gán ConfirmedAt.
                entity = new Settlement { HomestayId = _homestay.Id, Year = 2026, Month = 11, PeriodNo = 1,
                    SentAt = now, PaidAt = now };
                break;
            case "SettlementItems":
                var settlement = new Settlement { HomestayId = _homestay.Id, Year = 2026, Month = 11, PeriodNo = 1 };
                _db.Add(settlement);
                await _db.SaveChangesAsync();
                entity = Item(settlement.Id, await SavedBookingIdAsync());
                break;
            case "Contracts":
                entity = new Contract { HomestayId = _homestay.Id, ContractNo = "ENUM-001",
                    StartDate = new(2026, 1, 1), EndDate = new(2027, 1, 1), CommissionRate = 0.12m };
                break;
            case "Homestays":
                entity = new Homestay { OwnerId = _homestay.OwnerId, AreaId = _homestay.AreaId, Name = "Enum", Address = "Hà Nội" };
                break;
            case "RoomTypePrices":
                entity = new RoomTypePrice { RoomTypeId = _roomType.Id, EffectiveFrom = new(2026, 1, 1), Price = 1 };
                break;
            case "BookingNights":
                entity = new BookingNight { BookingId = await SavedBookingIdAsync(), Date = new(2026, 12, 1), Price = 1 };
                break;
            case "RoomBlocks":
                entity = new RoomBlock { RoomId = _room.Id, StartDate = new(2026, 12, 1), EndDate = new(2026, 12, 2),
                    CreatedByAccountId = (await WorkflowAccountAsync("101")).Id };
                break;
            case "OwnerRegistrations":
                entity = new OwnerRegistration { OwnerId = _homestay.OwnerId, ReviewedByEmployeeId = await ReviewEmployeeIdAsync(),
                    ReviewedAt = now, ReviewNote = "Đã thẩm định hồ sơ." };
                break;
            case "VerificationDocuments":
                entity = new VerificationDocument { HomestayId = _homestay.Id, FileUrl = "/test/giay-to.pdf" };
                break;
            case "CancellationPolicies":
                entity = new CancellationPolicy { HomestayId = _homestay.Id, Name = "Enum" };
                break;
            case "Payments":
                var payment = Deposit(await SavedBookingIdAsync(), PaymentStatus.Submitted);
                payment.ReceiptImageUrl = "/test/bill.png";
                if (column == "Type" && (PaymentType)value == PaymentType.AdditionalDeposit)
                {
                    var request = Request(payment.BookingId, ChangeRequestType.Cancel);
                    _db.Add(request);
                    await _db.SaveChangesAsync();
                    payment.ChangeRequestId = request.Id;
                }
                entity = payment;
                break;
            default:
                throw new InvalidOperationException(table);
        }
        entity.GetType().GetProperty(column)!.SetValue(entity, value);
        if (allowed) await AssertAcceptedAsync(entity);
        else await AssertRejectedByAsync(entity, $"CK_{table}_{column}");
    }

    [Theory]
    [InlineData(null, false, true)]
    [InlineData(null, true, false)]
    [InlineData("", false, false)]
    [InlineData("", true, false)]
    [InlineData("Cảm ơn quý khách.", false, false)]
    [InlineData("Cảm ơn quý khách.", true, true)]
    public async Task Review_ReplyAndTimeMustBePresentTogether(string? reply, bool hasTime, bool allowed)
    {
        var review = new Review { BookingId = await SavedBookingIdAsync(), HomestayId = _homestay.Id, Rating = 5,
            OwnerReply = reply, RepliedAt = hasTime ? DateTime.UtcNow : null };
        if (allowed) await AssertAcceptedAsync(review);
        else await AssertRejectedByAsync(review, "CK_Reviews_Reply");
    }

    [Theory]
    [InlineData(2019, false)]
    [InlineData(2020, true)]
    [InlineData(2100, true)]
    [InlineData(2101, false)]
    public async Task Settlement_YearWithinSupportedBounds(int year, bool allowed)
    {
        var settlement = new Settlement { HomestayId = _homestay.Id, Year = year, Month = 1, PeriodNo = 1 };
        if (allowed) await AssertAcceptedAsync(settlement);
        else await AssertRejectedByAsync(settlement, "CK_Settlements_Year");
    }

    public static IEnumerable<object?[]> RegistrationDecisionCases()
    {
        foreach (var status in Enum.GetValues<OwnerRegistrationStatus>())
        {
            var decided = status is OwnerRegistrationStatus.NeedsRevision or OwnerRegistrationStatus.Approved or OwnerRegistrationStatus.Rejected;
            var needsReason = status is OwnerRegistrationStatus.NeedsRevision or OwnerRegistrationStatus.Rejected;
            yield return [status, false, false, null, !decided];
            yield return [status, true, false, "Đã xem hồ sơ.", !decided];
            yield return [status, false, true, "Đã xem hồ sơ.", !decided];
            yield return [status, true, true, "Đã xem hồ sơ.", true];
            yield return [status, true, true, null, !needsReason];
            yield return [status, true, true, "", !needsReason];
        }
    }

    [Theory]
    [MemberData(nameof(RegistrationDecisionCases))]
    public async Task OwnerRegistration_DecisionRequiresReviewerTimeAndReasonWhenNeeded(
        OwnerRegistrationStatus status, bool hasReviewer, bool hasTime, string? note, bool allowed)
    {
        var employeeId = await ReviewEmployeeIdAsync();
        var registration = new OwnerRegistration { OwnerId = _homestay.OwnerId, Status = status,
            ReviewedByEmployeeId = hasReviewer ? employeeId : null, ReviewedAt = hasTime ? DateTime.UtcNow : null, ReviewNote = note };
        if (allowed) await AssertAcceptedAsync(registration);
        else await AssertRejectedByAsync(registration, "CK_OwnerRegistrations_Decision");
    }

    private async Task<int> ReviewEmployeeIdAsync()
    {
        var employee = new Employee { EmployeeCode = "CHECK-QL", Position = EmployeePosition.Manager,
            Person = new PersonProfile { PhoneNumber = "+84901999102" } };
        _db.Add(employee);
        await _db.SaveChangesAsync();
        return employee.Id;
    }
}
