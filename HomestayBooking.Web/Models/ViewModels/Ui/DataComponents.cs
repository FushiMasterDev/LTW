using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;

namespace HomestayBooking.Web.Models.ViewModels.Ui;

/// <param name="Label">Nhãn của số liệu.</param>
/// <param name="Value">Giá trị đã định dạng để hiển thị.</param>
/// <param name="Icon">Tên Bootstrap Icon, không gắn màu theo module.</param>
/// <param name="Href">Đường dẫn tùy chọn khi toàn thẻ là một liên kết.</param>
/// <param name="Hint">Mô tả bổ sung.</param>
/// <param name="Tone">Chỉ truyền tone khi số liệu mang ý nghĩa trạng thái.</param>
public record StatCardModel(string Label, string Value, string Icon = "graph-up", string? Href = null, string? Hint = null, string? Tone = null);

/// <param name="Title">Tiêu đề, mặc định tương thích các lời gọi cũ.</param>
/// <param name="Description">Hướng dẫn khi chưa có dữ liệu.</param>
/// <param name="Icon">Tên biểu tượng.</param>
/// <param name="ActionLabel">Nhãn hành động tùy chọn.</param>
/// <param name="ActionUrl">Đường dẫn hành động.</param>
public record EmptyStateModel(string Title = "Không có dữ liệu.", string? Description = null, string Icon = "inbox", string? ActionLabel = null, string? ActionUrl = null);

/// <param name="Label">Nhãn đã dịch hoặc khóa resource.</param>
/// <param name="Value">Nội dung HTML tin cậy, dùng HtmlContentBuilder.Append cho văn bản người dùng.</param>
public record DescriptionItem(string Label, IHtmlContent Value);

/// <param name="Label">Tiêu đề cột.</param>
/// <param name="SortKey">Khóa sắp xếp được controller hỗ trợ, null nếu không sắp xếp.</param>
/// <param name="Numeric">Căn phải cột số.</param>
public record TableColumn(string Label, string? SortKey = null, bool Numeric = false);

/// <param name="Cells">Nội dung từng ô theo thứ tự cột, không dùng HtmlString cho dữ liệu người dùng.</param>
/// <param name="Actions">Nút thao tác theo partial RowActions hiện có.</param>
public record TableRow(IReadOnlyList<IHtmlContent> Cells, RowActionsModel? Actions = null);

/// <param name="Caption">Tên bảng đọc được bằng trình đọc màn hình.</param>
/// <param name="Columns">Các cột hiển thị.</param>
/// <param name="Rows">Các dòng trong trang hiện tại.</param>
/// <param name="Pager">Thông tin phân trang đã có, không tự nạp thêm dữ liệu.</param>
/// <param name="Empty">Thông điệp khi bảng rỗng.</param>
public record DataTableModel(string Caption, IReadOnlyList<TableColumn> Columns, IReadOnlyList<TableRow> Rows, IPaginatedList? Pager = null, EmptyStateModel? Empty = null);

/// <summary>Dựng query string sắp xếp; giữ cả bộ lọc nhiều giá trị và đặt lại trang.</summary>
public static class TableSortLink
{
    public static string Build(PathString path, IQueryCollection query, string key)
    {
        var direction = string.Equals(query["sort"], key, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(query["dir"], "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
        var omitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "sort", "dir", "page" };
        var values = query.Where(q => !omitted.Contains(q.Key))
            .SelectMany(q => q.Value.Select(v => new KeyValuePair<string, string?>(q.Key, v))).ToList();
        values.Add(new("sort", key)); values.Add(new("dir", direction));
        return path + QueryString.Create(values).ToString();
    }
}

/// <param name="At">Thời điểm UTC của sự kiện.</param>
/// <param name="Title">Tên sự kiện.</param>
/// <param name="Description">Nội dung văn bản đã được Razor encode.</param>
public record TimelineItem(DateTime At, string Title, string? Description = null);

/// <param name="Changes">Lịch sử yêu cầu đổi có đầy đủ hook cũ.</param>
/// <param name="Events">Lịch sử trạng thái hoặc sự kiện khác.</param>
public record TimelineModel(IReadOnlyList<ChangeRequestHistoryRow>? Changes = null, IReadOnlyList<TimelineItem>? Events = null);

/// <param name="Label">Nhãn dòng tiền.</param>
/// <param name="Amount">Số tiền cần định dạng qua money.</param>
/// <param name="QuotedTotal">Giữ data-quote-total cho báo giá thay đổi.</param>
/// <param name="Additional">Giữ data-additional-payment cho cọc bổ sung.</param>
public record PriceLine(string Label, decimal Amount, bool QuotedTotal = false, bool Additional = false);

/// <param name="Lines">Các dòng giá theo dữ liệu báo giá có sẵn, không tính nghiệp vụ tại view.</param>
/// <param name="Rules">Quy tắc hủy có sẵn, giữ nguyên lời giải thích của PolicyRules.</param>
public record PriceBreakdownModel(IReadOnlyList<PriceLine>? Lines = null, IReadOnlyList<PolicyRuleView>? Rules = null)
{
    public static PriceBreakdownModel From(BookingChangeQuote quote) => new([
        new("Tổng tiền mới", quote.NewTotalAmount, QuotedTotal: true), new("Phí đổi", quote.ChangeFee),
        new("Cọc chuyển sang", quote.DepositCarriedOver), new("Tiền phải nộp thêm", quote.AdditionalDeposit, Additional: true),
        .. (quote.RefundAmount is { } refund ? new[] { new PriceLine("Tiền hoàn khi hủy", refund) } : []),
    ]);
}

/// <param name="Cards">Hiển thị khung thẻ thay vì dòng.</param>
/// <param name="Count">Số phần tử chờ tải.</param>
public record SkeletonModel(bool Cards = false, int Count = 3);
