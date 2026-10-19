using HomestayBooking.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HomestayBooking.Web.Areas.Staff.Controllers;
/// <summary>Guard legacy notification URLs. Content review belongs to Admin in step 05d.</summary>
[Area("Staff"),Authorize(Policy=AppPolicies.Employee),Authorize(Policy=AppPolicies.Management)]
public class HomestaysController : Controller
{
    public IActionResult Index() => NotFound();
    [HttpGet("/Staff/Homestays/{id:int}")]
    [HttpGet("/Staff/Homestays/Details/{id:int}")]
    public IActionResult Details(int id) => NotFound();
}
