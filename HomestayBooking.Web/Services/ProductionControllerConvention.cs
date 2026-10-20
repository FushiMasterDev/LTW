using HomestayBooking.Web.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace HomestayBooking.Web.Services;

/// <summary>Không đăng ký endpoint Dev ngoài Development, kể cả với khách chưa đăng nhập.</summary>
public sealed class ProductionControllerConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers.Where(c => c.ControllerType == typeof(DevController)).ToArray())
            application.Controllers.Remove(controller);
    }
}
