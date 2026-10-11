namespace HomestayBooking.Web.Models.ViewModels;

/// <summary>Dữ liệu cho partial _PageHead (tiêu đề trang danh sách).</summary>
public record PageHeadModel(string Title, string Icon, string Tone, string? Subtitle = null, bool ShowCreate = true);

/// <summary>Dữ liệu cho partial _RowActions (nút Sửa/Xóa trên mỗi dòng).</summary>
public record RowActionsModel(string Id, string Name, string ConfirmMessage);
