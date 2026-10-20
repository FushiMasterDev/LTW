using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models;
using HomestayBooking.Web.Services;
using HomestayBooking.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
namespace HomestayBooking.Web.Areas.Staff.Controllers;
public class OwnerRegistrationsController(ApplicationDbContext db,OwnerRegistrationQueryService queries,
    OwnerRegistrationService registrations,PublicUrlService publicUrls,IStringLocalizer<SharedResource> l) : StaffController(db,l)
{
    public async Task<IActionResult> Index([FromQuery] RegistrationQuery query)
    {
        if(!ModelState.IsValid) return BadRequest();
        return View(new StaffRegistrationsModel(query,await queries.ListAsync(query,await CurrentActorAsync())));
    }
    [HttpGet("/Staff/OwnerRegistrations/{id:int}")]
    [HttpGet("/Staff/OwnerRegistrations/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        try {return View(await queries.DetailsAsync(id,await CurrentActorAsync()));}
        catch(BookingException){return NotFound();}
    }
    public async Task<IActionResult> Document(int id)
    {
        try {var file=await queries.OpenDocumentAsync(id,await CurrentActorAsync());return PhysicalFile(file.Path,file.ContentType,file.DownloadName);}
        catch(BookingException){return NotFound();}
    }
    [HttpGet,ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
    public async Task<IActionResult> Invitation(int id)
    {
        try {await queries.DetailsAsync(id,await CurrentActorAsync());}
        catch(BookingException){return NotFound();}
        Response.Headers["Referrer-Policy"]="no-referrer";
        return View(new StaffInvitationModel(id,TempData["OwnerInvitation:"+id] as string));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public Task<IActionResult> Invite(int id) => InviteCoreAsync(id,false);
    [HttpPost,ValidateAntiForgeryToken]
    public Task<IActionResult> ResendInvite(int id) => InviteCoreAsync(id,true);
    private async Task<IActionResult> InviteCoreAsync(int id,bool resend)
    {
        OwnerActivationInvitation? invitation=null;
        var saved=await SaveAsync(async()=>invitation=resend
            ? await registrations.ResendInviteAsync(id,await CurrentActorAsync())
            : await registrations.InviteAsync(id,await CurrentActorAsync()));
        if(!saved) return RedirectToAction(nameof(Details),new {id});
        // Encrypted TempData carries this private handoff once; never log or audit its token.
        TempData["OwnerInvitation:"+id]=publicUrls.Absolute(Url.Action("Activate","Partner",new {area="",id,token=invitation!.Token})!);
        return RedirectToAction(nameof(Invitation),new {id});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(int id)
    {
        await SaveAsync(async()=>await registrations.TakeAsync(id,await CurrentActorAsync()));
        return RedirectToAction(nameof(Details),new {id});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Revision(int id,string note)
    {
        await SaveAsync(async()=>await registrations.RequestRevisionAsync(id,note,await CurrentActorAsync()));
        return RedirectToAction(nameof(Details),new {id});
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy=AppPolicies.Management)]
    public async Task<IActionResult> Approve(int id)
    {
        await SaveAsync(async()=>await registrations.ApproveAsync(id,await CurrentActorAsync()));
        return RedirectToAction(nameof(Details),new {id});
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy=AppPolicies.Management)]
    public async Task<IActionResult> Reject(int id,string note)
    {
        await SaveAsync(async()=>await registrations.RejectAsync(id,note,await CurrentActorAsync()));
        return RedirectToAction(nameof(Details),new {id});
    }
}
