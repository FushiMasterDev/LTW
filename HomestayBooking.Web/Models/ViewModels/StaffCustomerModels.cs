using System.ComponentModel.DataAnnotations;
using HomestayBooking.Web.Services;
namespace HomestayBooking.Web.Models.ViewModels;
public class StaffCustomerForm
{
    public int Id {get;set;}
    [Display(Name="Họ tên khách"), StringLength(100)] public string? FullName {get;set;}
    [Display(Name="Email"), EmailAddress, StringLength(256)] public string? Email {get;set;}
    [Display(Name="Ghi chú"), StringLength(1000)] public string? Note {get;set;}
    public CustomerDetails? Details {get;set;}
}
