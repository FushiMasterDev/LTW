using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace HomestayBooking.DesktopHost;

/// <summary>Chạy sau provider mặc định để loại cả MVC, area, API cũ và webhook.</summary>
internal sealed class DesktopControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        const string root = "HomestayBooking.Web.Controllers.Api.Desktop";
        foreach (TypeInfo controller in feature.Controllers.ToArray())
            if (controller.Namespace != root && controller.Namespace?.StartsWith(root + ".", StringComparison.Ordinal) != true)
                feature.Controllers.Remove(controller);
    }
}
