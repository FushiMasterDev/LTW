using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Extensions;

/// <summary>Thông tin phân trang cho partial _Pager, không phụ thuộc kiểu phần tử.</summary>
public interface IPaginatedList
{
    int PageIndex { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    bool HasPreviousPage { get; }
    bool HasNextPage { get; }
}

public class PaginatedList<T> : List<T>, IPaginatedList
{
    public const int DefaultPageSize = 10;

    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public PaginatedList(IEnumerable<T> items, int totalCount, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        AddRange(items);
    }

    /// <summary>Đếm tổng rồi lấy đúng một trang (Skip/Take). Số trang ngoài phạm vi được kẹp về trang hợp lệ.</summary>
    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int? pageIndex, int pageSize = DefaultPageSize)
    {
        var count = await source.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        var page = Math.Clamp(pageIndex ?? 1, 1, totalPages);
        var items = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PaginatedList<T>(items, count, page, pageSize);
    }
}
