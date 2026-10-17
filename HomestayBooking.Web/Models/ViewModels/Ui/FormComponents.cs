using Microsoft.AspNetCore.Mvc.Rendering;

namespace HomestayBooking.Web.Models.ViewModels.Ui;

/// <param name="Name">Tên trường query string hiện có.</param>
/// <param name="Label">Khóa resource của nhãn.</param>
/// <param name="Value">Giá trị ban đầu nếu query chưa có trường này.</param>
/// <param name="Type">Loại input HTML; "checkbox" kèm Options dựng nhóm ô chọn nhiều (tiện nghi).</param>
/// <param name="Options">Có giá trị thì dựng select thay input.</param>
/// <param name="Multiple">Cho chọn nhiều giá trị trong select.</param>
public record FilterField(string Name, string Label, string? Value = null, string Type = "text", IReadOnlyList<SelectListItem>? Options = null, bool Multiple = false);

/// <param name="Id">Id duy nhất cho ngăn kéo lọc.</param>
/// <param name="Fields">Các trường lọc được controller hỗ trợ.</param>
/// <param name="Action">Đường dẫn GET, null là trang hiện tại.</param>
public record FilterBarModel(string Id, IReadOnlyList<FilterField> Fields, string? Action = null);

/// <param name="Id">Id duy nhất của bộ chọn ngày.</param>
/// <param name="CheckIn">Ngày nhận phòng.</param>
/// <param name="CheckOut">Ngày trả phòng, không tính là đêm lưu trú.</param>
/// <param name="CheckInName">Tên trường nhận phòng theo hợp đồng form hiện có.</param>
/// <param name="CheckOutName">Tên trường trả phòng theo hợp đồng form hiện có.</param>
/// <param name="BlockedUrl">Endpoint GET trả các đêm hết phòng, có thể bỏ trống.</param>
/// <param name="Error">Lỗi cần hiển thị ngay dưới điều khiển.</param>
/// <param name="Disabled">Trạng thái chỉ xem/đang tải.</param>
public record DateRangePickerModel(string Id, DateOnly? CheckIn = null, DateOnly? CheckOut = null,
    string CheckInName = "CheckIn", string CheckOutName = "CheckOut", string? BlockedUrl = null, string? Error = null, bool Disabled = false);

/// <param name="Id">Id của input và vùng tương tác.</param>
/// <param name="Name">Tên trường form thật.</param>
/// <param name="Value">Số khách ban đầu.</param>
/// <param name="Min">Số khách ít nhất.</param>
/// <param name="Max">Sức chứa tối đa.</param>
/// <param name="Error">Thông báo lỗi từ server.</param>
/// <param name="Disabled">Không cho thao tác khi đang tải.</param>
public record GuestPickerModel(string Id, string Name = "Guests", int Value = 2, int Min = 1, int Max = 20, string? Error = null, bool Disabled = false);

/// <param name="Id">Id của ô chọn file.</param>
/// <param name="Name">Tên trường file server đang đọc.</param>
/// <param name="Multiple">Cho chọn nhiều file.</param>
/// <param name="AllowPdf">Cho PDF cùng JPG/PNG/WEBP.</param>
/// <param name="Error">Thông báo lỗi từ server.</param>
public record UploadZoneModel(string Id, string Name = "file", bool Multiple = false, bool AllowPdf = true, string? Error = null);

/// <param name="Message">Nội dung văn bản, Razor encode trước khi hiển thị.</param>
/// <param name="Tone">success/danger/warning/info, ánh xạ sang tone hiện có.</param>
/// <param name="Dismissible">Có nút đóng hay không.</param>
public record AlertModel(string Message, string Tone = "info", bool Dismissible = true);

/// <param name="Id">Tiền tố id của các ô chọn.</param>
/// <param name="Name">Tên trường form server đọc.</param>
/// <param name="Value">Điểm đã chọn trước (1–5), null nếu chưa chọn.</param>
/// <param name="Error">Thông báo lỗi từ server.</param>
public record RatingInputModel(string Id, string Name = "rating", int? Value = null, string? Error = null);
