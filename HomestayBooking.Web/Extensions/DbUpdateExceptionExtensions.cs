using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Extensions;

public static class DbUpdateExceptionExtensions
{
    /// <summary>
    /// True khi lỗi do vi phạm unique index/constraint của SQL Server (2601, 2627).
    /// Dùng khi hai request ghi trùng cùng lúc vượt qua bước kiểm tra trước (QUY_TRINH_BTL mục 6.2).
    /// </summary>
    public static bool IsUniqueConstraintViolation(this DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
