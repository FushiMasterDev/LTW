using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.TagHelpers;

namespace HomestayBooking.Tests.TagHelpers;

public class OwnerStatusBadgeTests
{
    [Theory]
    [InlineData(SettlementStatus.Draft,"Bản nháp","tone-slate")]
    [InlineData(SettlementStatus.Sent,"Đã gửi","tone-sky")]
    [InlineData(SettlementStatus.Confirmed,"Đã xác nhận","tone-emerald")]
    [InlineData(SettlementStatus.Paid,"Đã thanh toán","tone-emerald")]
    public void SettlementStatus_HasLocalizedLabelAndSemanticTone(SettlementStatus status,string label,string tone)
    {
        var badge=StatusBadgeTagHelper.Describe(status);
        Assert.Equal(label,badge.Label);
        Assert.Equal(tone,badge.Tone);
    }

    [Theory]
    [InlineData(PaymentStatus.Submitted,"Chờ xử lý","tone-amber")]
    [InlineData(PaymentStatus.Approved,"Đã xác nhận","tone-emerald")]
    [InlineData(PaymentStatus.Rejected,"Đã từ chối","tone-rose")]
    public void CompensationStatus_HasLocalizedLabelAndSemanticTone(PaymentStatus status,string label,string tone)
    {
        var badge=StatusBadgeTagHelper.Describe(status);
        Assert.Equal(label,badge.Label);
        Assert.Equal(tone,badge.Tone);
    }
}
