using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HomestayBooking.Web.DesktopApi;

/// <summary>Chỉ áp Bearer cho nhóm desktop được bảo vệ; không đổi bảo mật mười endpoint cũ hay login.</summary>
public sealed class DesktopSwaggerFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath;
        if (path?.StartsWith("api/desktop/", StringComparison.OrdinalIgnoreCase) != true) return;
        operation.Description = (operation.Description ?? "") + " Ngày: yyyy-MM-dd. Tất cả timestamp: UTC với hậu tố Z. Enum desktop: tên dạng chữ.";
        if (path.StartsWith("api/desktop/owner/", StringComparison.OrdinalIgnoreCase))
            operation.Tags = new HashSet<OpenApiTagReference> { new("Desktop Owner", context.Document) };
        if (path.Equals("api/desktop/auth/login", StringComparison.OrdinalIgnoreCase)
            || path.Equals("api/desktop/owner/auth/login", StringComparison.OrdinalIgnoreCase)) return;
        operation.Security = [new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = [],
        }];
    }
}
