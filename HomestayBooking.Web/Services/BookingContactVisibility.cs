using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Services;

/// <summary>BR-13: một chốt dùng cho dữ liệu trả khách và tin nhắc chứa liên hệ chính xác.</summary>
public static class BookingContactVisibility
{
    public static bool CanView(BookingStatus status) => status is BookingStatus.Confirmed or BookingStatus.CheckedIn or BookingStatus.Completed;

    /// <summary>Chỉ gọi trên bản đọc không tracking, không lưu lại entity đã lược dữ liệu.</summary>
    public static void RedactIfHidden(Booking booking)
    {
        if (CanView(booking.Status)) return;
        booking.Homestay.Address = ""; booking.Homestay.ContactPhone = null;
        booking.Homestay.Latitude = null; booking.Homestay.Longitude = null; booking.Homestay.CheckinGuideHtml = null;
    }
}
