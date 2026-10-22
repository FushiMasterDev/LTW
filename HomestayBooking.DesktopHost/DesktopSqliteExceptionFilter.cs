using HomestayBooking.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.DesktopHost;

/// <summary>Nhận diện mã unique của provider SQLite trước bộ lọc dùng mã lỗi SQL Server của Web.</summary>
public sealed class DesktopSqliteExceptionFilter(IStringLocalizer<SharedResource> localizer) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DbUpdateException { InnerException: SqliteException { SqliteExtendedErrorCode: 1555 or 2067 } }) return;
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = 409, Title = "Conflict", Detail = localizer["Tên này đã tồn tại."].Value,
            Instance = context.HttpContext.Request.Path,
        }) { StatusCode = 409, ContentTypes = { "application/problem+json" } };
        context.ExceptionHandled = true;
    }
}
