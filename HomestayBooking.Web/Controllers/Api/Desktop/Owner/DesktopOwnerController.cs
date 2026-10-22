using System.Security.Claims;
using HomestayBooking.Web.Services;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;

namespace HomestayBooking.Web.Controllers.Api.Desktop.Owner;

/// <summary>Chủ đã duyệt, chỉ Bearer; dùng chung hợp đồng lỗi và bỏ antiforgery riêng cho desktop.</summary>
[Authorize(Policy = AppPolicies.ApprovedOwner)]
public abstract class DesktopOwnerController : DesktopApiController
{
    protected Actor Actor => new(ActorType.Owner, User.FindFirstValue(ClaimTypes.NameIdentifier));
}
