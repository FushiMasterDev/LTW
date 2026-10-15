using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Services;

/// <summary>Công thức đối soát mục 7.3, các ví dụ lấy đúng từ fuji_homestay_database_v1.md.</summary>
public class SettlementCalculatorTests
{
    /// <summary>Ví dụ của tài liệu: tổng 2.000.000đ, cọc 600.000đ, hoa hồng 12%.</summary>
    private static Booking Sample(BookingStatus status, decimal? refund = null, decimal total = 2_000_000, decimal deposit = 600_000, decimal discount = 0) => new()
    {
        Id = 1,
        HomestayId = 7,
        Status = status,
        CheckIn = new(2026, 9, 14),
        CheckOut = new(2026, 9, 16),
        TotalAmount = total,
        DepositAmount = deposit,
        DiscountAmount = discount,
        VoucherId = discount > 0 ? 1 : null,
        CommissionRate = 0.12m,
        RefundAmount = refund,
        DepositedAt = new DateTime(2026, 9, 1),
    };

    [Theory]
    [InlineData(BookingStatus.Completed, null, 600_000, 240_000, 360_000)]
    [InlineData(BookingStatus.Cancelled, 300_000, 300_000, 240_000, 60_000)] // hủy trước 5 ngày, hoàn 50%
    [InlineData(BookingStatus.Cancelled, 0, 600_000, 240_000, 360_000)]      // hủy sát ngày, không hoàn
    [InlineData(BookingStatus.NoShow, 0, 600_000, 240_000, 360_000)]
    public void ItemFor_FollowsDocumentExamples(BookingStatus status, int? refund, int retained, int commission, int payout)
    {
        var item = SettlementCalculator.ItemFor(Sample(status, refund))!;

        Assert.Equal(retained, item.RetainedAmount);
        Assert.Equal(commission, item.CommissionAmount);
        Assert.Equal(payout, item.PayoutAmount);
    }

    [Fact]
    public void ItemFor_SmallRetained_CommissionCappedAtRetained()
    {
        // Hoàn 90%: chỉ giữ 60.000đ < hoa hồng 240.000đ → Fuji lấy hết phần giữ lại, homestay nhận 0
        var item = SettlementCalculator.ItemFor(Sample(BookingStatus.Cancelled, 540_000))!;

        Assert.Equal(60_000, item.RetainedAmount);
        Assert.Equal(60_000, item.CommissionAmount);
        Assert.Equal(0, item.PayoutAmount);
    }

    [Fact]
    public void ItemFor_FullRefundOrNoDeposit_IsExcluded()
    {
        Assert.Null(SettlementCalculator.ItemFor(Sample(BookingStatus.Cancelled, 600_000)));
        var beforeDeposit = Sample(BookingStatus.Cancelled, 0);
        beforeDeposit.DepositedAt = null;
        Assert.Null(SettlementCalculator.ItemFor(beforeDeposit));
        Assert.Null(SettlementCalculator.ItemFor(Sample(BookingStatus.Rejected, 600_000)));
        Assert.Null(SettlementCalculator.ItemFor(Sample(BookingStatus.Expired)));
        Assert.Null(SettlementCalculator.ItemFor(Sample(BookingStatus.Confirmed)));
    }

    [Fact]
    public void ItemFor_WithVoucher_HomestayGetsFullPayoutAndFujiBearsDiscount()
    {
        // Ví dụ voucher: tổng 2.700.000đ, cọc 810.000đ, giảm 64.000đ → homestay 486.000đ, Fuji còn 260.000đ
        var item = SettlementCalculator.ItemFor(Sample(BookingStatus.Completed, total: 2_700_000, deposit: 810_000, discount: 64_000))!;

        Assert.Equal(810_000, item.RetainedAmount);
        Assert.Equal(324_000, item.CommissionAmount);
        Assert.Equal(486_000, item.PayoutAmount);
        Assert.Equal(64_000, item.DiscountAmount);
        Assert.Equal(260_000, item.CommissionAmount - item.DiscountAmount);
    }

    [Fact]
    public void ItemFor_CancelledWithVoucher_RetainedAsIfNoVoucher()
    {
        // Khách chuyển 746.000đ, hủy hoàn 50% = 373.000đ; homestay vẫn được tính như cọc 810.000đ giữ 50%
        var item = SettlementCalculator.ItemFor(Sample(BookingStatus.Cancelled, 373_000, 2_700_000, 810_000, 64_000))!;

        Assert.Equal(405_000, item.RetainedAmount);
        Assert.Equal(32_000, item.DiscountAmount); // nửa còn lại đã trả vào số dư voucher
    }

    [Theory]
    [InlineData(2026, 9, 1, 1)]
    [InlineData(2026, 9, 15, 1)]
    [InlineData(2026, 9, 16, 2)]
    [InlineData(2026, 2, 28, 2)]
    public void PeriodOf_SplitsMonthAtFifteenth(int year, int month, int day, int periodNo)
    {
        var period = SettlementCalculator.PeriodOf(new DateOnly(year, month, day));

        Assert.Equal(periodNo, period.PeriodNo);
        Assert.InRange(new DateOnly(year, month, day), period.Start, period.End);
    }

    [Fact]
    public void Build_SumsOnlyBookingsOfHomestayAndPeriod()
    {
        var inPeriod = Sample(BookingStatus.Completed);
        var otherPeriod = Sample(BookingStatus.Completed);
        otherPeriod.Id = 2;
        otherPeriod.CheckOut = new(2026, 9, 20);
        var otherHomestay = Sample(BookingStatus.NoShow, 0);
        otherHomestay.Id = 3;
        otherHomestay.HomestayId = 8;

        var settlement = SettlementCalculator.Build(7, new SettlementPeriod(2026, 9, 2), [inPeriod, otherPeriod, otherHomestay]);

        Assert.Equal(2, settlement.Items.Count); // 16/9 và 20/9 thuộc kỳ 2
        Assert.Equal(1_200_000, settlement.TotalRetained);
        Assert.Equal(720_000, settlement.PayoutAmount);
        Assert.Equal(SettlementStatus.Draft, settlement.Status);
    }
    [Theory]
    [InlineData(BookingStatus.Completed, 0, 300000, 900000, 276000, 624000)]
    [InlineData(BookingStatus.Cancelled, 600000, 300000, 300000, 36000, 264000)]
    [InlineData(BookingStatus.Completed, 0, 600000, 1200000, 312000, 888000)]
    [InlineData(BookingStatus.Rejected, 600000, 300000, 300000, 36000, 264000)]
    [InlineData(BookingStatus.Expired, 0, 300000, 300000, 36000, 264000)]
    public void ChangeFeesAreSharedWithoutDoubleCommission(BookingStatus status, int refund, int fee,
        int retained, int commission, int payout)
    {
        var booking = Sample(status, refund); booking.ChangeFeeAmount = fee;
        var item = SettlementCalculator.ItemFor(booking)!;
        Assert.Equal(retained, item.RetainedAmount); Assert.Equal(commission, item.CommissionAmount);
        Assert.Equal(payout, item.PayoutAmount); Assert.Equal(600_000 + fee, item.DepositAmount);
        Assert.Equal(2_000_000, item.RoomAmount); Assert.Equal(refund, item.RefundAmount);
    }

    [Fact]
    public void ChangeFeeCommissionRoundsAwayFromZeroAndDoesNotSettleAnOpenBooking()
    {
        var booking = Sample(BookingStatus.Cancelled, 600_000); booking.ChangeFeeAmount = 25;
        booking.CommissionRate = 0.10m;
        var item = SettlementCalculator.ItemFor(booking)!;
        Assert.Equal(3, item.CommissionAmount); Assert.Equal(22, item.PayoutAmount);
        booking.Status = BookingStatus.Confirmed; Assert.Null(SettlementCalculator.ItemFor(booking));
    }

}
