using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
namespace HomestayBooking.Web.Models.ViewModels;
public record StaffDashboardModel(int Waiting,int Changes,int Registrations,IReadOnlyList<Booking> Overdue,
    EmployeePerformance? Performance,DateOnly From,DateOnly To);
