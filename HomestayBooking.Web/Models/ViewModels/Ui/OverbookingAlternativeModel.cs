using HomestayBooking.Web.Services;
namespace HomestayBooking.Web.Models.ViewModels.Ui;
/// <summary>Comparison from the service; preview suppresses all mutation forms in UI Kit.</summary>
public record OverbookingAlternativeModel(int BookingId,OverbookingAlternative Alternative,bool SameRoomType=false,bool Preview=false);
