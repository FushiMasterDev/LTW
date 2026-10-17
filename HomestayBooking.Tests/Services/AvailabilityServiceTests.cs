using System.Data.Common;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomestayBooking.Tests.Services;

public sealed class AvailabilityServiceTests
{
    [Theory]
    [InlineData(null, true)] [InlineData(1000000, true)] [InlineData(700000, false)]
    public async Task ThreeNightSearchDisplaysAndFiltersNightlyAverage(int? maximum, bool included)
    {
        using var h = await BackofficeTestHost.CreateAsync();
        foreach (var price in h.Db.RoomTypePrices) price.Price = 800000;
        await h.Db.SaveChangesAsync();
        var start = h.Today.AddDays(1); var end = start.AddDays(3);
        var query = new HomestaySearchQuery { CheckIn = start, CheckOut = end, MaxPrice = maximum };
        var result = (await h.Availability().SearchAsync(query)).Results;
        if (included) Assert.Equal(800000, Assert.Single(result).FromPrice);
        else Assert.Empty(result);
        var quote = await h.H.Pricing().QuoteAsync(h.H.RoomType.Id, start, end);
        Assert.Equal(2400000, quote.Total);
        var offer = Assert.Single(await h.Availability().StaffAvailabilityAsync(start, end, null, 2, h.Manager));
        Assert.Equal(quote.Total, offer.TotalPrice);
        query.MinPrice = 801000; query.MaxPrice = null;
        Assert.Empty((await h.Availability().SearchAsync(query)).Results);
        query.MinPrice = 800000; Assert.Single((await h.Availability().SearchAsync(query)).Results);
    }

    [Theory]
    [InlineData(HomestaySort.PriceAscending)] [InlineData(HomestaySort.PriceDescending)]
    public async Task DatedPriceSortUsesRoundedNightlyCardPrice(HomestaySort sort)
    {
        using var h = await BackofficeTestHost.CreateAsync();
        foreach (var price in h.Db.RoomTypePrices) price.Price = 800100;
        var other = await h.TypeAsync(800400, sameHome: false, name: "A cơ sở");
        await h.Db.SaveChangesAsync();
        var result = (await h.Availability().SearchAsync(new() { CheckIn = h.Today.AddDays(1), CheckOut = h.Today.AddDays(4), Sort = sort })).Results;
        Assert.Equal(2, result.Count); Assert.All(result, card => Assert.Equal(800000, card.FromPrice));
        // Cùng giá đã làm tròn thì xếp tên; dùng tổng chưa làm tròn sẽ cho thứ tự khác khi tăng giá.
        Assert.Equal(other.HomestayId, result[0].Id);
    }

    [Theory]
    [InlineData("normal", true)] [InlineData("room", false)] [InlineData("type", false)]
    [InlineData("pending", false)] [InlineData("suspended", false)] [InlineData("ended", false)]
    [InlineData("contract", false)] [InlineData("contract-end", true)]
    [InlineData("block", false)] [InlineData("change-hold", false)] [InlineData("booking", false)]
    public async Task AllAvailabilityPathsShareSellability(string kind, bool expected)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var start = h.Today.AddDays(5); var end = start.AddDays(2);
        var rooms = await h.Db.Rooms.ToListAsync();
        if (kind == "room") foreach (var room in rooms) room.IsActive = false;
        if (kind == "type") (await h.Db.RoomTypes.SingleAsync()).IsActive = false;
        if (kind is "pending" or "suspended" or "ended") (await h.Db.Homestays.SingleAsync()).Status = kind == "pending" ? HomestayStatus.PendingReview : kind == "suspended" ? HomestayStatus.Suspended : HomestayStatus.Ended;
        if (kind.StartsWith("contract")) (await h.Db.Contracts.SingleAsync()).EndDate = end.AddDays(kind == "contract" ? -1 : 0);
        if (kind is "block" or "change-hold" or "booking")
        {
            var b = await h.BookingAsync();
            if (kind == "booking")
            {
                await h.H.Bookings().CreateAsync(h.H.Request(start, end));
            }
            else if (kind == "block")
            {
                var block = new RoomBlock { RoomId = rooms.Single(r => r.Id != b.RoomId).Id, StartDate = start, EndDate = end, Reason = RoomBlockReason.Maintenance, CreatedByAccountId = h.Owner.AccountId! };
                block.Nights.Add(new() { RoomId = block.RoomId, Date = start }); h.Db.Add(block);
            }
            else
            {
                var r = new BookingChangeRequest { BookingId = b.Id, RequestType = ChangeRequestType.ChangeDates,
                    RequestedByType = ActorType.Customer, NewCheckIn = start, NewCheckOut = end };
                r.RoomNights.Add(new() { RoomId = rooms.Single(room => room.Id != b.RoomId).Id, Date = start }); h.Db.Add(r);
            }
        }
        await h.Db.SaveChangesAsync();
        var query = new HomestaySearchQuery { CheckIn = start, CheckOut = end, Guests = 2 };
        Assert.Equal(expected, (await h.Availability().SearchAsync(query)).Results.Any());
        Assert.Equal(expected, (await h.H.Bookings().FreeRoomIdsAsync(h.H.RoomType.Id, start, end)).Any());
        var staff = await h.Availability().StaffAvailabilityAsync(start, end, null, 2, h.Consultant);
        Assert.Equal(expected, staff.Any(o => o.FreeRooms > 0));
        if (kind is "block" or "change-hold" or "booking") { Assert.Single(staff); Assert.Equal(0, staff[0].FreeRooms); Assert.Equal(2, staff[0].TotalRooms); }
    }

    [Fact]
    public async Task DatedSearchPricesOnlyFreeCapacityEligibleTypesAndMatchesQuoteAcrossHolidayAndWeekend()
    {
        using var h = await BackofficeTestHost.CreateAsync(); var start = new DateOnly(2026, 12, 31); var end = new DateOnly(2027, 1, 4);
        var cheap = await h.TypeAsync(100000); cheap.MaxGuests = 1;
        var busy = await h.TypeAsync(200000); await h.Db.SaveChangesAsync();
        await h.H.Bookings().CreateAsync(h.H.Request(start, end) with { RoomTypeId = busy.Id });
        var quote = await h.H.Pricing().QuoteAsync(h.H.RoomType.Id, start, end);
        var query = new HomestaySearchQuery { CheckIn = start, CheckOut = end, Guests = 2 };
        var card = Assert.Single((await h.Availability().SearchAsync(query)).Results);
        Assert.Equal(613000, card.FromPrice); Assert.Equal(2450000, quote.Total);
        Assert.Equal(PricingService.RoundToThousand(quote.Total / 4), card.FromPrice);
        var staff = await h.Availability().StaffAvailabilityAsync(start, end, null, 2, h.Manager);
        Assert.Equal(quote.Total, staff.Single(o => o.RoomTypeId == h.H.RoomType.Id).TotalPrice);
        Assert.Equal(0, staff.Single(o => o.RoomTypeId == busy.Id).FreeRooms);
        Assert.DoesNotContain(staff, o => o.RoomTypeId == cheap.Id);
    }

    [Theory]
    [InlineData("area")] [InlineData("keyword")] [InlineData("category")] [InlineData("amenities")] [InlineData("min")] [InlineData("max")]
    public async Task SearchFiltersAcceptAndReject(string filter)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var amenity = new Amenity { Name = "Bếp" }; h.Db.Add(amenity); await h.Db.SaveChangesAsync();
        h.Db.HomestayAmenities.Add(new() { HomestayId = h.H.Homestay.Id, AmenityId = amenity.Id }); await h.Db.SaveChangesAsync();
        var query = new HomestaySearchQuery();
        if (filter == "area") query.AreaId = h.H.Homestay.AreaId;
        if (filter == "keyword") query.Q = "Tây Hồ";
        if (filter == "category") query.CategoryId = h.H.RoomType.CategoryId;
        if (filter == "amenities") query.AmenityIds = [amenity.Id, amenity.Id];
        if (filter == "min") query.MinPrice = 500000;
        if (filter == "max") query.MaxPrice = 500000;
        Assert.Single((await h.Availability().SearchAsync(query)).Results);
        if (filter == "area") query.AreaId = 99999;
        if (filter == "keyword") query.Q = "Không tồn tại";
        if (filter == "category") query.CategoryId = 99999;
        if (filter == "amenities") query.AmenityIds.Add(99999);
        if (filter == "min") query.MinPrice = 500001;
        if (filter == "max") query.MaxPrice = 499999;
        Assert.Empty((await h.Availability().SearchAsync(query)).Results);
    }

    [Theory]
    [InlineData(HomestaySort.Default)] [InlineData(HomestaySort.PriceAscending)]
    [InlineData(HomestaySort.PriceDescending)] [InlineData(HomestaySort.Rating)]
    public async Task SearchSortsAndIgnoresHiddenReviews(HomestaySort sort)
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var b = await h.BookingAsync(BookingStatus.Completed);
        h.Db.Reviews.Add(new() { BookingId = b.Id, HomestayId = b.HomestayId, Rating = 5, IsHidden = true });
        var other = await h.TypeAsync(300000, sameHome: false, name: "A cơ sở");
        var second = await h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(1), h.Today.AddDays(2)) with { RoomTypeId = other.Id });
        // Chỉ dựng dữ liệu đánh giá: thứ tự điểm phải bỏ bản bị ẩn của cơ sở còn lại.
        second.Status = BookingStatus.Completed; second.ConfirmedAt = h.H.Clock.Now.UtcDateTime;
        h.Db.Reviews.Add(new() { BookingId = second.Id, HomestayId = second.HomestayId, Rating = 3 }); await h.Db.SaveChangesAsync();
        var result = (await h.Availability().SearchAsync(new() { Sort = sort })).Results;
        Assert.Equal(2, result.Count);
        Assert.Equal(sort == HomestaySort.PriceDescending ? h.H.Homestay.Id : other.HomestayId, result[0].Id);
    }

    [Fact]
    public async Task BlockedDatesListsOnlyNightsWithEveryRoomTaken()
    {
        using var h = await BackofficeTestHost.CreateAsync(); var typeId = h.H.RoomType.Id;
        var rooms = await h.Availability().SellableRooms().Where(r => r.RoomTypeId == typeId).Select(r => r.Id).ToListAsync();
        Assert.Equal(2, rooms.Count); var full = h.Today.AddDays(10); var partial = h.Today.AddDays(11);
        foreach (var _ in rooms) await h.H.Bookings().CreateAsync(h.H.Request(full, full.AddDays(1)));
        await h.H.Bookings().CreateAsync(h.H.Request(partial, partial.AddDays(1)));
        var blocked = await h.Availability().BlockedDatesAsync(typeId, h.Today.AddDays(-3), h.Today.AddDays(20));
        Assert.Contains(full, blocked);
        Assert.DoesNotContain(partial, blocked);
        Assert.All(blocked, d => Assert.True(d >= h.Today));
        await Assert.ThrowsAsync<BookingException>(() => h.Availability().BlockedDatesAsync(typeId, h.Today, h.Today.AddDays(93)));
    }

    [Fact]
    public async Task SearchCardsCarryVisibleRatingAndFiltersListOnlyOptionsInUse()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var b = await h.BookingAsync(BookingStatus.Completed);
        h.Db.Reviews.Add(new() { BookingId = b.Id, HomestayId = b.HomestayId, Rating = 4 });
        var hidden = await h.H.Bookings().CreateAsync(h.H.Request(h.Today.AddDays(20), h.Today.AddDays(21)));
        hidden.Status = BookingStatus.Completed; hidden.ConfirmedAt = h.H.Clock.Now.UtcDateTime;
        h.Db.Reviews.Add(new() { BookingId = hidden.Id, HomestayId = hidden.HomestayId, Rating = 1, IsHidden = true });
        var used = new Amenity { Name = "Wifi" }; var unused = new Amenity { Name = "Hồ bơi" };
        var draftOnly = new RoomCategory { Name = "Chỉ ở nháp" };
        h.Db.AddRange(used, unused, draftOnly);
        h.Db.HomestayAmenities.Add(new() { HomestayId = h.H.Homestay.Id, Amenity = used });
        h.Db.Add(new RoomType { Name = "Nháp", Category = draftOnly, MaxGuests = 2, IsActive = true,
            Homestay = new Homestay { Name = "Nháp", Address = "X", Status = HomestayStatus.Draft, OwnerId = h.H.Homestay.OwnerId, AreaId = h.H.Homestay.AreaId } });
        await h.Db.SaveChangesAsync();
        var vm = await h.Availability().SearchAsync(new());
        var card = vm.Results.Single(c => c.Id == h.H.Homestay.Id);
        Assert.Equal(4, card.Rating); Assert.Equal(1, card.ReviewCount);
        Assert.Contains(vm.Amenities, a => a.Name == "Wifi"); Assert.DoesNotContain(vm.Amenities, a => a.Name == "Hồ bơi");
        Assert.Contains(vm.Categories, c => c.Id == h.H.RoomType.CategoryId); Assert.DoesNotContain(vm.Categories, c => c.Name == "Chỉ ở nháp");
    }

    [Fact]
    public async Task StaffLoadsNightsPricesAndHolidaysOnceForWholeRangeAndReflectsChangesImmediately()
    {
        using var h = await BackofficeTestHost.CreateAsync(); await h.TypeAsync(800000);
        var counter = new ReadCounter(); await using var db = h.H.NewContext(counter);
        var start = h.Today.AddDays(2); var end = start.AddDays(31);
        Assert.Equal(2, (await h.Availability(db).StaffAvailabilityAsync(start, end, null, 2, h.Manager)).Count);
        Assert.Equal(1, counter.Nights); Assert.Equal(1, counter.Prices); Assert.Equal(1, counter.Holidays);
        var roomId = await h.Db.Rooms.OrderBy(r => r.Id).Select(r => r.Id).FirstAsync();
        var block = new RoomBlock { RoomId = roomId, StartDate = start, EndDate = start.AddDays(1), Reason = RoomBlockReason.Maintenance, CreatedByAccountId = h.Owner.AccountId! };
        block.Nights.Add(new() { RoomId = roomId, Date = start }); h.Db.Add(block); await h.Db.SaveChangesAsync();
        var refreshed = await h.Availability(db).StaffAvailabilityAsync(start, end, null, 2, h.Manager);
        Assert.Equal(1, refreshed.Single(o => o.RoomTypeId == h.H.RoomType.Id).FreeRooms);
    }

    [Theory]
    [InlineData("accountant")] [InlineData("owner")] [InlineData("customer")] [InlineData("locked")]
    public async Task StaffAvailabilityRejectsWrongActor(string who)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var actor = who == "accountant" ? h.P.Accountant : who == "owner" ? h.Owner : who == "customer" ? new Actor(ActorType.Customer) : h.Consultant;
        if (who == "locked") { (await h.Db.Employees.SingleAsync(e => e.Id == actor.EmployeeId)).EmploymentStatus = EmploymentStatus.Resigned; await h.Db.SaveChangesAsync(); }
        await Assert.ThrowsAsync<BookingException>(() => h.Availability().StaffAvailabilityAsync(h.Today, h.Today.AddDays(1), null, 2, actor));
    }

    [Theory]
    [InlineData(-1, 1)] [InlineData(0, 0)] [InlineData(0, 32)]
    public async Task StaffRejectsInvalidDateRange(int start, int length)
    {
        using var h = await BackofficeTestHost.CreateAsync(); var from = h.Today.AddDays(start);
        await Assert.ThrowsAsync<BookingException>(() => h.Availability().StaffAvailabilityAsync(from, from.AddDays(length), null, 2, h.Manager));
    }

    private sealed class ReadCounter : DbCommandInterceptor
    {
        public int Nights, Prices, Holidays;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"RoomNights\"")) Nights++;
            if (command.CommandText.Contains("FROM \"RoomTypePrices\"")) Prices++;
            if (command.CommandText.Contains("FROM \"Holidays\"")) Holidays++;
            return ValueTask.FromResult(result);
        }
    }
}
