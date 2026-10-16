using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed partial class BookingChangeRequestServiceTests
{
    [Theory]
    [InlineData(HomestayStatus.PendingReview)] [InlineData(HomestayStatus.Suspended)] [InlineData(HomestayStatus.Ended)]
    public async Task NonActiveHomestayBlocksNewChangeAndHidesButton(HomestayStatus status)
    {
        using var s = new Scenario(); var b = await s.Book();
        var home = await s.Db.Homestays.SingleAsync(); home.Status = status; await s.Db.SaveChangesAsync();
        Assert.False(await s.Service().CanChangeAsync(b.Id, s.Customer));
        await Assert.ThrowsAsync<BookingException>(() => s.Service().QuoteAsync(s.Dates(b), s.Customer));
        await Assert.ThrowsAsync<BookingException>(() => s.Service().SubmitAsync(s.Dates(b), s.Customer));
        Assert.Empty(s.Db.BookingChangeRequests);
    }

    [Theory]
    [InlineData(HomestayStatus.PendingReview, true)]
    [InlineData(HomestayStatus.Suspended, false)] [InlineData(HomestayStatus.Ended, false)]
    public async Task ExistingRequestBusinessApprovalOnlyAllowsActiveOrPendingReview(HomestayStatus status, bool allowed)
    {
        using var s = new Scenario(); var b = await s.Book();
        var r = await s.Service().SubmitAsync(s.Dates(b), s.Customer);
        (await s.Db.Homestays.SingleAsync()).Status = status; await s.Db.SaveChangesAsync();
        if (allowed)
        {
            await s.Service().ApproveAsync(r.Id, "Duyệt lịch đã gửi", s.Manager);
            Assert.Equal(ChangeRequestStatus.Applied, r.Status);
            Assert.Equal(HomestayStatus.PendingReview, (await s.Db.Homestays.SingleAsync()).Status);
        }
        else
        {
            await Assert.ThrowsAsync<BookingException>(() => s.Service().ApproveAsync(r.Id, "Không mở lại cơ sở", s.Manager));
            Assert.Equal(ChangeRequestStatus.Pending, (await s.Db.BookingChangeRequests.SingleAsync()).Status);
        }
    }

    [Theory]
    [InlineData(HomestayStatus.PendingReview, true)]
    [InlineData(HomestayStatus.Suspended, false)] [InlineData(HomestayStatus.Ended, false)]
    public async Task ExistingPaidRequestFinanceOnlyAllowsActiveOrPendingReview(HomestayStatus status, bool allowed)
    {
        using var s = new Scenario(); var (b, r, _, accountant) = await Awaiting(s);
        await s.H.Matching().ProcessAsync(AdditionalTx(s, b, r));
        (await s.Db.Homestays.SingleAsync()).Status = status; await s.Db.SaveChangesAsync();
        if (allowed)
        {
            await s.Service().ConfirmAsync(r.Id, "Đã nộp tiền cho lịch đã duyệt", accountant);
            Assert.Equal(ChangeRequestStatus.Applied, r.Status);
            Assert.Equal(PaymentStatus.Approved, (await s.Db.Payments.SingleAsync()).Status);
            Assert.Equal(HomestayStatus.PendingReview, (await s.Db.Homestays.SingleAsync()).Status);
        }
        else
        {
            await Assert.ThrowsAsync<BookingException>(() => s.Service().ConfirmAsync(r.Id, "Không mở lại cơ sở", accountant));
            Assert.Equal(ChangeRequestStatus.AwaitingPayment, (await s.Db.BookingChangeRequests.SingleAsync()).Status);
            Assert.Equal(PaymentStatus.Submitted, (await s.Db.Payments.SingleAsync()).Status);
            Assert.NotEmpty(await s.Db.RoomNights.Where(n => n.BookingChangeRequestId == r.Id).ToListAsync());
        }
    }

    [Theory]
    [InlineData("contract", false)] [InlineData("room", false)] [InlineData("type", false)]
    [InlineData("contract", true)] [InlineData("room", true)] [InlineData("type", true)]
    public async Task PendingReviewStillChecksContractAndTargetAvailability(string invalid, bool finance)
    {
        using var s = new Scenario(); Booking b; BookingChangeRequest r;
        System.Security.Claims.ClaimsPrincipal actor;
        if (finance)
        {
            (b, r, _, actor) = await Awaiting(s);
            await s.H.Matching().ProcessAsync(AdditionalTx(s, b, r));
        }
        else
        {
            b = await s.Book(); r = await s.Service().SubmitAsync(s.Dates(b), s.Customer); actor = s.Manager;
        }
        (await s.Db.Homestays.SingleAsync()).Status = HomestayStatus.PendingReview;
        if (invalid == "contract") (await s.Db.Contracts.SingleAsync()).EndDate = r.NewCheckOut!.Value.AddDays(-1);
        if (invalid == "type") (await s.Db.RoomTypes.SingleAsync()).IsActive = false;
        if (invalid == "room") foreach (var room in s.Db.Rooms) room.IsActive = false;
        await s.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BookingException>(() => finance
            ? s.Service().ConfirmAsync(r.Id, "Kiểm tra lại", actor)
            : s.Service().ApproveAsync(r.Id, "Kiểm tra lại", actor));
        Assert.Equal(finance ? ChangeRequestStatus.AwaitingPayment : ChangeRequestStatus.Pending,
            (await s.Db.BookingChangeRequests.SingleAsync()).Status);
    }
}
