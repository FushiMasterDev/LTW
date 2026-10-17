using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class OwnerNotificationTests
{
    [Theory]
    [InlineData("deposit", "đã đặt cọc")]
    [InlineData("staff-confirm", "xác nhận thay chủ")]
    [InlineData("staff-reject", "từ chối thay chủ")]
    [InlineData("customer-cancel", "đã bị hủy")]
    [InlineData("request-cancel", "đã bị hủy theo yêu cầu")]
    [InlineData("change", "cần xác nhận lại trong 30 phút")]
    [InlineData("finance-change", "cần xác nhận lại trong 30 phút")]
    [InlineData("overbooking", "đã bị hủy")]
    [InlineData("replacement", "đã đặt cọc")]
    [InlineData("review", "vừa có đánh giá")]
    public async Task EveryRequiredOwnerEventNotifiesTheCorrectOwner(string kind, string text)
    {
        using var h = await GuestServicesTestHost.CreateAsync();
        var b = await h.BookAsync(kind is "change" ? 10 : kind is "review" ? 0 : 4,
            kind is "deposit" or "staff-confirm" or "staff-reject" ? BookingStatus.AwaitingPayment : kind == "review" ? BookingStatus.Completed : BookingStatus.Confirmed);
        var before = await h.Db.Notifications.MaxAsync(n => (int?)n.Id) ?? 0;
        if (kind == "deposit") await h.H.Bookings().ChangeStatusAsync(b, BookingStatus.Deposited, Actor.System);
        else if (kind is "staff-confirm" or "staff-reject")
        {
            await h.H.Bookings().ChangeStatusAsync(b, BookingStatus.Deposited, Actor.System);
            await h.B.Staff().ChangeStatusAsync(b.Id, kind == "staff-confirm" ? BookingStatus.Confirmed : BookingStatus.Rejected, null, "Đã gọi chủ", h.P.Consultant);
        }
        else if (kind == "customer-cancel") await h.H.Bookings().ChangeStatusAsync(b, BookingStatus.Cancelled, h.Customer, "Khách đổi kế hoạch");
        else if (kind is "request-cancel" or "change" or "finance-change")
        {
            var principal = HomestayTestHost.Principal(h.P.Consultant.AccountId!, "Employee");
            var input = kind == "request-cancel" ? new ChangeBookingRequest(b.Id, ChangeRequestType.Cancel)
                : new ChangeBookingRequest(b.Id, ChangeRequestType.ChangeDates, b.CheckIn.AddDays(7), b.CheckOut.AddDays(7));
            var request = await h.H.Changes().SubmitAsync(input, principal);
            await h.H.Changes().ApproveAsync(request.Id, "Quản lý đồng ý", HomestayTestHost.Principal(h.P.Manager.AccountId!, "Employee"));
            if (kind == "finance-change")
            {
                Assert.Equal(ChangeRequestStatus.AwaitingPayment, request.Status);
                await h.H.Receipts().SubmitChangeRequestAsync(request.Id, "receipts/cr.png", null, HomestayTestHost.Principal(h.Customer.AccountId!, "Customer"));
                await h.H.Changes().ConfirmAsync(request.Id, "Đã nhận tiền", HomestayTestHost.Principal(h.P.Accountant.AccountId!, "Employee"));
            }
        }
        else if (kind == "overbooking") await h.B.Overbooking().RefundAsync(b.Id, "Không tìm được phòng", h.P.Manager);
        else if (kind == "replacement")
        {
            var type = await h.B.TypeAsync(800000, sameHome: false);
            b = await h.B.Overbooking().ReplaceAsync(b.Id, type.Id, "Khách đồng ý", h.P.Manager);
        }
        else await h.Reviews().CreateAsync(b.Id, 5, null, h.Customer);
        var notices = await h.Db.Notifications.Where(n => n.Id > before).ToListAsync();
        Assert.Contains(notices, n => n.UserId == h.B.Owner.AccountId && n.Message.Contains(b.Code) && n.Message.Contains(text));
        Assert.All(notices, n => Assert.True(n.Message.Length <= 500));
    }

    [Fact]
    public async Task CompletingBookingInvitesOnlyItsCustomerToReview()
    {
        using var h = await GuestServicesTestHost.CreateAsync(); var b = await h.BookAsync(0, BookingStatus.CheckedIn);
        await h.H.Bookings().ChangeStatusAsync(b, BookingStatus.Completed, Actor.System);
        var notice = Assert.Single(await h.Db.Notifications.Where(n => n.Message.Contains("Mời bạn đánh giá")).ToListAsync());
        Assert.Equal(h.Customer.AccountId, notice.UserId); Assert.Equal($"/Bookings/Details/{b.Code}", notice.Url);
    }
}
