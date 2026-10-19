using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Services;
namespace HomestayBooking.Web.Models.ViewModels;
public record StaffRegistrationsModel(RegistrationQuery Query,PaginatedList<RegistrationRow> Registrations);

/// <summary>Private one-time invitation handoff. The URL must never be logged.</summary>
public record StaffInvitationModel(int RegistrationId,string? Url);
