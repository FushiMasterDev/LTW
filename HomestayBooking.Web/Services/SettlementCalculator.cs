using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Services;

public record SettlementPeriod(int Year, int Month, int PeriodNo)
{
    public DateOnly Start => new(Year, Month, PeriodNo == 1 ? 1 : 16);
    public DateOnly End => PeriodNo == 1 ? new(Year, Month, 15) : new DateOnly(Year, Month, 1).AddMonths(1).AddDays(-1);
}

/// <summary>
/// Công thức đối soát (fuji_homestay_database_v1 mục 7.3, D7, D8): đơn vào kỳ theo ngày CheckOut;
/// homestay nhận phần cọc giữ lại trừ hoa hồng, tính như đơn không có voucher (voucher do Fuji chịu).
/// </summary>
public static class SettlementCalculator
{
    /// <summary>Kỳ 1: ngày 1–15; kỳ 2: ngày 16 – cuối tháng.</summary>
    public static SettlementPeriod PeriodOf(DateOnly checkOut) =>
        new(checkOut.Year, checkOut.Month, checkOut.Day <= 15 ? 1 : 2);

    /// <summary>
    /// Dòng đối soát của một đơn, hoặc null nếu đơn không vào đối soát: chưa từng có cọc,
    /// homestay từ chối / hết hạn, hoặc hủy được hoàn 100%.
    /// </summary>
    public static SettlementItem? ItemFor(Booking booking)
    {
        var itemType = booking.Status switch
        {
            BookingStatus.Completed => SettlementItemType.Completed,
            BookingStatus.NoShow => SettlementItemType.NoShow,
            BookingStatus.Cancelled when booking.DepositedAt is not null => SettlementItemType.CancelledRetained,
            _ => (SettlementItemType?)null,
        };
        if (itemType is null)
        {
            return null;
        }

        var refund = booking.RefundAmount ?? 0;
        // Tỷ lệ hoàn đã áp (quy tắc hủy là số nguyên phần trăm); tiền hoàn đã làm tròn nghìn nên lấy lại % gần nhất
        var percent = booking.AmountDue > 0 ? Math.Round(100m * refund / booking.AmountDue) : 0;
        var retained = booking.DepositAmount - PricingService.RoundToThousand(booking.DepositAmount * percent / 100m);
        if (retained <= 0)
        {
            return null;
        }
        var commission = Math.Min(Math.Round(booking.TotalAmount * booking.CommissionRate, 0, MidpointRounding.AwayFromZero), retained);
        // Voucher: phần đã trả lại vào số dư khi hủy (VoucherService.RestoreAsync) không tính là Fuji chịu
        var discount = booking.DiscountAmount - PricingService.RoundToThousand(booking.DiscountAmount * percent / 100m);

        return new SettlementItem
        {
            BookingId = booking.Id,
            Booking = booking,
            ItemType = itemType.Value,
            RoomAmount = booking.TotalAmount,
            DepositAmount = booking.DepositAmount,
            RefundAmount = refund,
            RetainedAmount = retained,
            CommissionRate = booking.CommissionRate,
            CommissionAmount = commission,
            PayoutAmount = retained - commission,
            DiscountAmount = Math.Max(0, discount),
        };
    }

    /// <summary>Lập bảng đối soát nháp cho một homestay một kỳ từ các đơn có CheckOut trong kỳ.</summary>
    public static Settlement Build(int homestayId, SettlementPeriod period, IEnumerable<Booking> bookings)
    {
        var settlement = new Settlement
        {
            HomestayId = homestayId,
            Year = period.Year,
            Month = period.Month,
            PeriodNo = period.PeriodNo,
            Status = SettlementStatus.Draft,
        };
        foreach (var booking in bookings.Where(b => b.HomestayId == homestayId && PeriodOf(b.CheckOut) == period))
        {
            if (ItemFor(booking) is { } item)
            {
                settlement.Items.Add(item);
            }
        }
        settlement.TotalRoomAmount = settlement.Items.Sum(i => i.RoomAmount);
        settlement.TotalRetained = settlement.Items.Sum(i => i.RetainedAmount);
        settlement.TotalCommission = settlement.Items.Sum(i => i.CommissionAmount);
        settlement.PayoutAmount = settlement.Items.Sum(i => i.PayoutAmount);
        return settlement;
    }
}
