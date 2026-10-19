using System.Data.Common;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HomestayBooking.Tests.Services;

public sealed class B9StaffTests
{
    [Fact]
    public async Task SearchAndDetailsLoadHistoricalNamesAndCatalogIncludesInactiveHomes()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var b = await h.BookingAsync();
        var employee = await h.Db.Employees.Include(e => e.Person).SingleAsync(e => e.Id == h.Consultant.EmployeeId);
        employee.EmploymentStatus = EmploymentStatus.Resigned; employee.Person.FullName = "Người phụ trách cũ";
        b.Homestay.Status = HomestayStatus.Suspended;
        await h.Db.SaveChangesAsync(); h.Db.ChangeTracker.Clear();
        var row = Assert.Single(await h.Staff().SearchAsync(new() { HomestayId = b.HomestayId }, h.Manager));
        Assert.Equal("Người phụ trách cũ", row.AssignedEmployee!.Person.FullName); Assert.NotEmpty(row.Homestay.Name); Assert.NotEmpty(row.Room.RoomCode);
        var details = await h.Staff().DetailsAsync(b.Id, h.Manager);
        Assert.Equal("Người phụ trách cũ", details.Booking.AssignedEmployee!.Person.FullName);
        Assert.All(details.History.Where(x => x.ActorAccountId != null), x => Assert.NotNull(x.ActorAccount!.Person));
        Assert.Contains(await h.Staff().HomestaysAsync(h.Manager), x => x.Id == b.HomestayId && x.Name == row.Homestay.Name);
        await Assert.ThrowsAsync<BookingException>(() => h.Staff().HomestaysAsync(h.Owner));
    }

    [Fact]
    public async Task BatchNightsEqualIndividualReadsWithFixedQueriesAndRoomCodes()
    {
        using var h = await BackofficeTestHost.CreateAsync();
        var b = await h.BookingAsync(); var start = h.Today.AddDays(1); var end = start.AddDays(31);
        var counter = new ReadCounter();
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(h.Db.Database.GetDbConnection()).AddInterceptors(counter).Options);
        var batch = await h.Availability(db).StaffNightAvailabilityAsync(start, end, null, 2, h.Consultant);
        var queries = counter.Reads;
        Assert.NotEmpty(batch); Assert.InRange(queries, 1, 6);
        foreach (var group in batch.GroupBy(x => x.Night))
        {
            var single = await h.Availability().StaffAvailabilityAsync(group.Key, group.Key.AddDays(1), null, 2, h.Consultant);
            Assert.Equal(single.Select(x => (x.RoomTypeId, x.FreeRooms)), group.Select(x => (x.RoomTypeId, x.FreeRooms)));
        }
        counter.Reads = 0;
        await h.Availability(db).StaffNightAvailabilityAsync(start, start.AddDays(1), null, 2, h.Consultant);
        Assert.Equal(queries, counter.Reads);
        var free = await h.Availability().FreeRoomsAsync(b.RoomTypeId, end, end.AddDays(1), h.Consultant);
        Assert.NotEmpty(free); Assert.All(free, r => Assert.NotEmpty(r.RoomCode));
        await Assert.ThrowsAsync<BookingException>(() => h.Availability().FreeRoomsAsync(b.RoomTypeId, start, end, h.Owner));
        await Assert.ThrowsAsync<BookingException>(() => h.Availability().StaffNightAvailabilityAsync(start, end, null, 2, h.Owner));
        await Assert.ThrowsAsync<BookingException>(() => h.Availability().StaffNightAvailabilityAsync(start, end.AddDays(1), null, 2, h.Consultant));
    }

    private sealed class ReadCounter : DbCommandInterceptor
    {
        public int Reads;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Reads++; return ValueTask.FromResult(result); }
    }
}
