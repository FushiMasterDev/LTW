namespace HomestayBooking.Web.Models.ViewModels;

/// <param name="Title">Tiêu đề trang.</param>
/// <param name="Icon">Biểu tượng tùy chọn, không có nền màu theo module.</param>
/// <param name="Tone">Giữ tham số cũ để tương thích, không dùng để tô đầu trang.</param>
/// <param name="Subtitle">Mô tả ngắn.</param>
/// <param name="ShowCreate">Giữ tên cũ, có nghĩa là hiện hành động chính.</param>
/// <param name="Action">Tên action của hành động chính.</param>
/// <param name="Controller">Controller, null là controller hiện tại.</param>
/// <param name="Area">Area, null giữ area hiện tại, chuỗi rỗng về trang công khai.</param>
/// <param name="RouteValues">Tham số route của hành động chính.</param>
/// <param name="ActionLabel">Nhãn nút, mặc định giữ Thêm mới.</param>
public record PageHeadModel(string Title, string Icon, string Tone, string? Subtitle = null, bool ShowCreate = true,
    string Action = "Create", string? Controller = null, string? Area = null,
    IReadOnlyDictionary<string, string>? RouteValues = null, string ActionLabel = "Thêm mới");

/// <summary>Dữ liệu cho partial _RowActions (nút Sửa/Xóa trên mỗi dòng).</summary>
public record RowActionsModel(string Id, string Name, string ConfirmMessage);
