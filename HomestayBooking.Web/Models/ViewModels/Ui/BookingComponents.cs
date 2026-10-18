using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomestayBooking.Web.Models.ViewModels.Ui;

/// <param name="Id">Id duy nhất cho các trường tìm phòng.</param>
/// <param name="Areas">Danh mục khu vực đã nạp bởi controller.</param>
/// <param name="Query">Bộ lọc hiện tại, giữ tên trường theo HomestaysController.</param>
/// <param name="Variant">hero hoặc compact.</param>
public record SearchBarModel(string Id, IReadOnlyList<SelectListItem> Areas, HomestaySearchQuery? Query = null, string Variant = "compact");

/// <param name="Id">Id homestay dùng cho liên kết.</param>
/// <param name="Name">Tên homestay, không chứa HTML.</param>
/// <param name="AreaName">Khu vực công khai; không đưa địa chỉ riêng vào thẻ.</param>
/// <param name="CoverUrl">Ảnh từ dữ liệu, qua ImageUrlExtensions.</param>
/// <param name="FromPrice">Giá mỗi đêm đã được service tính.</param>
/// <param name="Rating">Điểm đánh giá không bị ẩn.</param>
/// <param name="ReviewCount">Số đánh giá.</param>
/// <param name="Badge">Nhãn tùy chọn.</param>
/// <param name="MaxGuests">Sức chứa hiển thị khi có.</param>
/// <param name="Amenities">Tiện nghi đã nạp.</param>
/// <param name="Query">Giữ ngày/số khách khi sang trang chi tiết.</param>
/// <param name="HeadingLevel">2 cho danh sách tìm kiếm, 3 trong section trang chủ.</param>
public record HomestayCardModel(int Id, string Name, string AreaName, string? CoverUrl, decimal? FromPrice,
    double? Rating = null, int ReviewCount = 0, string? Badge = null, int MaxGuests = 0,
    IReadOnlyList<string>? Amenities = null, HomestaySearchQuery? Query = null, int HeadingLevel = 3)
{
    public static HomestayCardModel From(HomestayCard card, HomestaySearchQuery query) =>
        new(card.Id, card.Name, card.AreaName, card.CoverUrl, card.FromPrice, card.Rating, card.ReviewCount,
            MaxGuests: card.MaxGuests, Amenities: card.Amenities, Query: query, HeadingLevel: 2);
    public static HomestayCardModel From(FeaturedHomestay card) =>
        new(card.Id, card.Name, card.AreaName, card.CoverUrl, card.FromPrice, card.Rating, card.ReviewCount);
}

/// <param name="Name">Tên homestay để mô tả ảnh.</param>
/// <param name="Images">URL ảnh từ dữ liệu; mỗi trang có một lightbox.</param>
public record GalleryModel(string Name, IReadOnlyList<string> Images);

/// <param name="Id">Id riêng của form báo giá.</param>
/// <param name="Dates">Ngày đã chọn.</param>
/// <param name="Guests">Số khách và sức chứa.</param>
/// <param name="Price">Báo giá đã tính ở service.</param>
/// <param name="Action">Đường dẫn GET nhận lựa chọn (trang chi tiết homestay).</param>
/// <param name="Title">Khóa resource của tiêu đề ô, bỏ trống thì không có tiêu đề.</param>
/// <param name="SubmitLabel">Khóa resource của nút gửi.</param>
/// <param name="Empty">Gợi ý khi chưa có dòng giá (chưa chọn ngày).</param>
/// <param name="Note">Ghi chú dưới form, ví dụ quy tắc hiện địa chỉ.</param>
public record BookingPanelModel(string Id, DateRangePickerModel Dates, GuestPickerModel Guests, PriceBreakdownModel Price, string Action,
    string? Title = null, string SubmitLabel = "Xem báo giá", string? Empty = null, string? Note = null);

/// <param name="Status">Trạng thái đơn hiện tại; component chỉ đọc.</param>
public record BookingStepperModel(BookingStatus Status)
{
    public static readonly BookingStatus[] Progress = [BookingStatus.AwaitingPayment, BookingStatus.PaymentSubmitted,
        BookingStatus.Deposited, BookingStatus.Confirmed, BookingStatus.CheckedIn, BookingStatus.Completed];
    public int Current => Array.IndexOf(Progress, Status);
}

/// <param name="ImageUrl">URL được VietQrService tạo, null thì hiện hướng dẫn dự phòng.</param>
/// <param name="Amount">Số tiền phải chuyển đã tính bởi service.</param>
/// <param name="Content">Nội dung chuyển khoản chính xác, gồm mã đơn hoặc CR.</param>
/// <param name="DeadlineUtc">Hạn nộp/giữ chỗ theo UTC.</param>
/// <param name="Bank">Tài khoản nhận tiền từ cấu hình sẵn có.</param>
/// <param name="Additional">Cọc bổ sung, giữ data-change-countdown.</param>
/// <param name="ReloadOnExpiry">Chỉ trang đơn thật mới tự tải lại khi hết hạn.</param>
public record PaymentQrModel(string? ImageUrl, decimal Amount, string Content, DateTime DeadlineUtc,
    BankAccountOptions? Bank = null, bool Additional = false, bool ReloadOnExpiry = false);


/// <summary>Ảnh khu vực được controller lấy từ homestay đang hoạt động; null dùng nền xanh.</summary>
public record AreaCardModel(int Id, string Name, int Count, string? CoverUrl = null, AreaGroup Group = AreaGroup.InnerCity);

/// <summary>Thẻ chọn phòng, giữ nguyên báo giá và query đã có; không tự tính nghiệp vụ.</summary>
public record RoomOptionModel(RoomTypeOffer Offer, HomestaySearchQuery Query);

/// <summary>Đánh giá công khai đã có, tên có thể trống khi service không nạp người viết.</summary>
public record ReviewCardModel(int Rating, DateTime CreatedAt, string? Comment, string? Reply, string? Name = null);

public record MotionStep(string Title, string Text);
public record CardDeckModel(IReadOnlyList<MotionStep> Steps);
public record AreaCarouselModel(IReadOnlyList<AreaCardModel> Areas);
public record PhotoStackModel(IReadOnlyList<FeaturedHomestay> Homes);
