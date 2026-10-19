using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using HomestayBooking.Web.Extensions;
namespace HomestayBooking.Web.Models.ViewModels;
public record StaffOverbookingModel(Booking Booking,PaginatedList<OverbookingAlternative> Alternatives,IReadOnlyList<int> FreeRooms);
