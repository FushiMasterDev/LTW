using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
namespace HomestayBooking.Web.Areas.Staff.Controllers;
public class CustomersController(ApplicationDbContext db, CustomerService customers, IStringLocalizer<SharedResource> l) : StaffController(db,l)
{
    public async Task<IActionResult> Index(string? q,int? page) => View(await customers.SearchAsync(q,page,await CurrentActorAsync()));
    public async Task<IActionResult> Details(int id)
    {
        try {
            var d=await customers.DetailsAsync(id,await CurrentActorAsync());
            return View(new StaffCustomerForm {Id=id,FullName=d.Customer.FullName,Email=d.Customer.Email,Note=d.Customer.Note,Details=d});
        } catch(BookingException) {return NotFound();}
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id,StaffCustomerForm form)
    {
        await SaveAsync(async () => {
            var actor=await CurrentActorAsync();var customer=(await customers.DetailsAsync(id,actor)).Customer;
            await customers.UpdateAsync(id,new(form.Note ?? "",customer.HasAccount ? null : form.FullName,customer.HasAccount ? null : form.Email),actor);
        });
        return RedirectToAction(nameof(Details),new {id});
    }
}
