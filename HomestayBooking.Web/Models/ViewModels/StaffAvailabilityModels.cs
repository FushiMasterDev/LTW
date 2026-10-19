using HomestayBooking.Web.Services;
using HomestayBooking.Web.Extensions;
namespace HomestayBooking.Web.Models.ViewModels;
public record StaffNightAvailability(DateOnly Night, NightAvailability Offer);
public record StaffAvailabilityModel(DateOnly CheckIn,DateOnly CheckOut,int? AreaId,int Guests,
    IReadOnlyList<AreaOption> Areas,PaginatedList<StaffNightAvailability> Nights,DateTime UpdatedAt);
