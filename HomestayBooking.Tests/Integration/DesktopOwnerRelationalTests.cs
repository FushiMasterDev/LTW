using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;

namespace HomestayBooking.Tests.Integration;

/// <summary>Kiểm truy vấn mới, transaction và ExecuteUpdate trên SQLite trong bộ nhớ, không kết nối SQL Server.</summary>
public sealed class DesktopOwnerRelationalTests
{
    [Fact]
    public async Task OwnedReadsAndRoomBlockSettlementWritesWorkOnRelationalProvider()
    {
        using var fixture = await DesktopSqliteHost.CreateAsync();
        var c = fixture.Host.CreateClient();
        var login = await c.PostAsJsonAsync("/api/desktop/owner/auth/login", new { phoneNumber = "0912000001", password = SeedData.DefaultPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        c.DefaultRequestHeaders.Authorization = new("Bearer", (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString());
        var own = fixture.WithDb(db => db.Rooms.Where(r => r.RoomType.Homestay.Owner.Person.PhoneNumber == "+84912000001")
            .OrderBy(r => r.Id).Select(r => new { r.Id, r.RoomType.HomestayId }).First());
        var foreignHome = fixture.WithDb(db => db.Homestays.Where(h => h.Owner.Person.PhoneNumber != "+84912000001").Select(h => h.Id).First());
        foreach (var path in new[] { "dashboard", "bookings", "schedule?period=week", $"homestays/{own.HomestayId}", $"calendar?homestayId={own.HomestayId}", "reviews", "settlements" })
        {
            var response = await c.GetAsync("/api/desktop/owner/" + path);
            Assert.True(response.StatusCode == HttpStatusCode.OK, path + ": " + await response.Content.ReadAsStringAsync());
        }
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/desktop/owner/homestays/{foreignHome}")).StatusCode);
        var start = VietnamTime.Today(TimeProvider.System).AddDays(700);
        var pathBlock = $"/api/desktop/owner/homestays/{own.HomestayId}/rooms/{own.Id}/blocks";
        var range = new { start, end = start.AddDays(2), reason = "Maintenance" };
        var created = await c.PostAsJsonAsync(pathBlock, range);
        Assert.True(created.StatusCode == HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var blockId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal(2, fixture.WithDb(db => db.RoomNights.Count(n => n.RoomBlockId == blockId)));
        var conflict = await c.PostAsJsonAsync(pathBlock, range);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(2, (await conflict.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("conflicts").GetArrayLength());
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync(pathBlock + "/" + blockId)).StatusCode);
        Assert.Equal(0, fixture.WithDb(db => db.RoomNights.Count(n => n.RoomBlockId == blockId)));
        var settlement = fixture.WithDb(db =>
        {
            var s = db.Settlements.First(s => s.HomestayId == own.HomestayId); s.Status = SettlementStatus.Sent; s.SentAt = DateTime.UtcNow.AddDays(-1); s.ConfirmedAt = s.PaidAt = null; s.PaidByEmployeeId = null; s.PaidBankRef = null; db.SaveChanges(); return s.Id;
        });
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync($"/api/desktop/owner/settlements/{settlement}/confirm", null)).StatusCode);
        Assert.Equal(SettlementStatus.Confirmed, fixture.WithDb(db => db.Settlements.Single(s => s.Id == settlement).Status));
        var ownDocument = fixture.WithDb(db => db.VerificationDocuments.Where(d => d.HomestayId == own.HomestayId).Select(d => d.Id).First());
        var foreignDocument = fixture.WithDb(db => db.VerificationDocuments.Where(d => d.HomestayId == foreignHome).Select(d => d.Id).First());
        // Seed file is private and unavailable in this host's isolated file store; ownership query must still translate.
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/desktop/owner/documents/{ownDocument}/file")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/desktop/owner/documents/{foreignDocument}/file")).StatusCode);
    }
}
