using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.DesktopApi;

/// <summary>Chỉ gắn ở base controller desktop, không thay đổi lỗi MVC hay API cookie cũ.</summary>
public sealed class DesktopExceptionFilter(IStringLocalizer<SharedResource> localizer, ILogger<DesktopExceptionFilter> logger) : IExceptionFilter
{
    private static readonly HashSet<string> Missing =
    [
        "Không tìm thấy loại phòng.", "Không tìm thấy đơn đặt phòng.", "Không tìm thấy khách hàng.", "Không tìm thấy khoản thanh toán.",
        "Không tìm thấy ảnh bill.", "Không tìm thấy bill.", "Không tìm thấy khu vực.", "Không tìm thấy tiện nghi.",
        "Không tìm thấy danh mục loại phòng.", "Không tìm thấy ngày lễ.", "Không tìm thấy homestay.",
    ];

    public void OnException(ExceptionContext context)
    {
        var (status, detail) = context.Exception switch
        {
            CatalogConflictException ex => (409, localizer[ex.ResourceKey, ex.Arguments].Value),
            KeyNotFoundException ex => (404, localizer[ex.Message].Value),
            BookingException ex => (Missing.Contains(ex.ResourceKey) ? 404 : 400, localizer[ex.ResourceKey, ex.Arguments].Value),
            DbUpdateConcurrencyException => (409, localizer["Đơn vừa được cập nhật bởi thao tác khác, vui lòng xem lại."].Value),
            DbUpdateException ex when ex.IsUniqueConstraintViolation() => (409, localizer["Tên này đã tồn tại."].Value),
            _ => (500, "Đã xảy ra lỗi khi xử lý yêu cầu."),
        };
        if (status == 500) logger.LogError(context.Exception, "Desktop API request failed.");
        context.Result = new ObjectResult(new ProblemDetails { Status = status, Detail = detail,
            Title = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(status), Instance = context.HttpContext.Request.Path })
        { StatusCode = status, ContentTypes = { "application/problem+json" } };
        context.ExceptionHandled = true;
    }
}
