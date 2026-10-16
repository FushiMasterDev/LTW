using HomestayBooking.Tests.Data;
using HomestayBooking.Web.Controllers;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Services;

public sealed class AvailabilitySeedRegressionTests
{
    [Fact]
    public async Task ControllersKeepUndatedResultsAndFeaturedOrderingOnCompleteSeed()
    {
        var databaseName = "B3-seed-" + Guid.NewGuid();
        await using var services = SeedTestServices.Build(s => s.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(databaseName)));
        await SeedData.InitializeAsync(services);
        await using var scope = services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clock = TimeProvider.System; var today = VietnamTime.Today(clock); var options = Options.Create(new BookingOptions());
        var homes = await db.Homestays.Include(h => h.RoomTypes).ThenInclude(t => t.Rooms).Include(h => h.RoomTypes).ThenInclude(t => t.Prices)
            .Where(h => h.Status == HomestayStatus.Active && db.Contracts.Covering(today, today).Any(c => c.HomestayId == h.Id)
                && h.RoomTypes.Any(t => t.IsActive && t.MaxGuests >= 2 && t.Rooms.Any(r => r.IsActive))).ToListAsync();
        Assert.NotEmpty(homes);
        var availability = new AvailabilityService(db, clock, options);
        var controller = new HomestaysController(db, new(db, options), clock, options, availability);
        var results = Assert.IsType<HomestaySearchViewModel>(Assert.IsType<ViewResult>(await controller.Index(new())).Model).Results;
        Assert.Equal(homes.Count, results.TotalCount);
        Assert.Equal(homes.OrderBy(h => h.Name).Take(9).Select(h => h.Id), results.Select(h => h.Id));
        foreach (var card in results)
        {
            var home = homes.Single(h => h.Id == card.Id); var types = home.RoomTypes.Where(t => t.IsActive && t.MaxGuests >= 2 && t.Rooms.Any(r => r.IsActive)).ToList();
            var current = PricingService.CurrentPrices(types.SelectMany(t => t.Prices), today);
            Assert.Equal(types.Select(t => current.TryGetValue((t.Id, DayType.Weekday), out var p) ? (decimal?)p : null).Min(), card.FromPrice);
        }
        var ratings = await db.Reviews.Where(r => !r.IsHidden).ToListAsync();
        var expectedFeatured = homes.OrderByDescending(h => ratings.Where(r => r.HomestayId == h.Id).Select(r => (double?)r.Rating).Average() ?? 0)
            .ThenByDescending(h => ratings.Count(r => r.HomestayId == h.Id)).ThenBy(h => h.Name).Take(6).Select(h => h.Id);
        var featured = Assert.IsType<HomeIndexViewModel>(Assert.IsType<ViewResult>(await new HomeController(db, clock, availability).Index()).Model).Featured;
        Assert.Equal(expectedFeatured, featured.Select(h => h.Id));
        var first = homes.First(); var detail = Assert.IsType<HomestayDetailsViewModel>(Assert.IsType<ViewResult>(await controller.Details(first.Id, new())).Model);
        Assert.Equal(first.RoomTypes.Count(t => t.IsActive && t.Rooms.Any(r => r.IsActive)), detail.RoomTypes.Count);
    }
}
