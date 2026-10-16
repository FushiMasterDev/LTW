using HomestayBooking.Web.Data;
using HomestayBooking.Web.Controllers;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Services;

public sealed class RoomSearchSelectionTests
{
    [Theory]
    [InlineData(true, true, false, true)] [InlineData(true, true, true, true)]
    [InlineData(false, true, false, false)] [InlineData(false, true, true, false)]
    [InlineData(true, false, false, false)] [InlineData(true, false, true, false)]
    public async Task SearchFeaturedAndDetailsCannotOfferDisabledRoomOrType(bool roomActive, bool typeActive, bool dated, bool expected)
    {
        // Projection giao diện sẵn có cần APPLY; như B1, kiểm controller trên InMemory, nghiệp vụ ghi vẫn dùng SQLite.
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var clock = new TestClock { Now = new(2026, 11, 10, 3, 0, 0, TimeSpan.Zero) }; var today = VietnamTime.Today(clock);
        var home = new Homestay { Name = "Tìm kiếm", Address = "Hà Nội", Status = HomestayStatus.Active,
            Area = new Area { Name = "Tây Hồ", Province = "Hà Nội" }, Owner = new Owner { Person = new PersonProfile { PhoneNumber = "+84912345678" } } };
        home.Contracts.Add(new() { ContractNo = "HD-SEARCH-B2", StartDate = today.AddYears(-1), EndDate = today.AddYears(1), CommissionRate = .12m });
        var type = new RoomType { Name = "Đôi", MaxGuests = 2, Category = new RoomCategory { Name = "Đôi" }, IsActive = typeActive };
        type.Rooms.Add(new() { RoomCode = "101", IsActive = roomActive });
        foreach (var day in Enum.GetValues<DayType>()) type.Prices.Add(new() { DayType = day, Price = 500000, EffectiveFrom = today });
        home.RoomTypes.Add(type); db.Add(home); await db.SaveChangesAsync();
        var options = Options.Create(new BookingOptions()); var controller = new HomestaysController(db, new PricingService(db, options), clock, options);
        var query = new HomestaySearchQuery { Guests = 2, CheckIn = dated ? today.AddDays(1) : null, CheckOut = dated ? today.AddDays(2) : null };
        var result = Assert.IsType<HomestaySearchViewModel>(Assert.IsType<ViewResult>(await controller.Index(query)).Model);
        Assert.Equal(expected ? 1 : 0, result.Results.Count);
        var detail = Assert.IsType<HomestayDetailsViewModel>(Assert.IsType<ViewResult>(await controller.Details(home.Id, query)).Model);
        Assert.Equal(expected ? 1 : 0, detail.RoomTypes.Count);
        var featured = Assert.IsType<HomeIndexViewModel>(Assert.IsType<ViewResult>(await new HomeController(db, clock).Index()).Model);
        Assert.Equal(expected ? 1 : 0, featured.Featured.Count);
    }
}
