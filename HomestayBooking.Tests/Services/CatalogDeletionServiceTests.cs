using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Services;

public sealed class CatalogDeletionServiceTests
{
    [Fact]
    public async Task DeleteAuditReloadsValueChangedSinceControllerRead()
    {
        using var h = await PartnerTestHost.CreateAsync(); var area = new Area { Name = "Tên cũ đã đọc", Province = "Hà Nội" };
        h.Db.Add(area); await h.Db.SaveChangesAsync();
        await using (var other = h.H.NewContext())
        {
            (await other.Areas.SingleAsync(a => a.Id == area.Id)).Name = "Tên mới trong CSDL"; await other.SaveChangesAsync();
        }
        await new CatalogDeletionService(h.Db, h.H.Clock).DeleteAsync(CatalogKind.Area, area.Id, h.Admin);
        var log = await h.Db.AuditLogs.SingleAsync();
        Assert.Equal("Tên mới trong CSDL", System.Text.Json.JsonDocument.Parse(log.OldValues!).RootElement.GetProperty("Name").GetString());
    }
    [Theory]
    [InlineData(CatalogKind.Area)] [InlineData(CatalogKind.RoomCategory)] [InlineData(CatalogKind.Amenity)]
    public async Task UsedCatalogIsRejectedWithoutRemovingRelationships(CatalogKind kind)
    {
        using var h = await PartnerTestHost.CreateAsync(); int id;
        if (kind == CatalogKind.Area) id = h.H.Homestay.AreaId;
        else if (kind == CatalogKind.RoomCategory) id = h.H.RoomType.CategoryId;
        else
        {
            var amenity = new Amenity { Name = "Đang dùng" }; h.Db.Amenities.Add(amenity); await h.Db.SaveChangesAsync();
            h.Db.HomestayAmenities.Add(new() { HomestayId = h.H.Homestay.Id, AmenityId = amenity.Id }); await h.Db.SaveChangesAsync(); id = amenity.Id;
        }
        var ex = await Assert.ThrowsAsync<BookingException>(() => new CatalogDeletionService(h.Db, h.H.Clock).DeleteAsync(kind, id, h.Admin));
        Assert.Contains("đang được sử dụng", ex.Message); Assert.Empty(await h.Db.AuditLogs.ToListAsync());
        if (kind == CatalogKind.Amenity) Assert.True(await h.Db.HomestayAmenities.AnyAsync(a => a.AmenityId == id));
    }
    [Theory]
    [InlineData(CatalogKind.Area)] [InlineData(CatalogKind.RoomCategory)] [InlineData(CatalogKind.Amenity)] [InlineData(CatalogKind.Holiday)]
    public async Task UnusedCatalogMayBeDeletedWithAudit(CatalogKind kind)
    {
        using var h = await PartnerTestHost.CreateAsync(); object entity = kind switch {
            CatalogKind.Area => new Area { Name = "Khu mới", Province = "Hà Nội" }, CatalogKind.RoomCategory => new RoomCategory { Name = "Loại mới" },
            CatalogKind.Amenity => new Amenity { Name = "Tiện nghi mới" }, _ => new Holiday { Date = new(2030, 1, 1), Name = "Ngày lễ thử" } };
        h.Db.Add(entity); await h.Db.SaveChangesAsync(); var id = h.Db.Entry(entity).Properties.Single(p => p.Metadata.IsPrimaryKey()).CurrentValue!;
        await new CatalogDeletionService(h.Db, h.H.Clock).DeleteAsync(kind, id, h.Manager);
        var log = await h.Db.AuditLogs.SingleAsync(); Assert.Equal(AuditAction.Delete, log.Action);
        Assert.Equal(id is DateOnly date ? date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : id.ToString(), log.RecordId); Assert.Equal(h.Manager.AccountId, log.AccountId);
    }
    [Theory]
    [InlineData("consultant")] [InlineData("accountant")] [InlineData("owner")]
    public async Task OtherActorsCannotDeleteCatalog(string type)
    {
        using var h = await PartnerTestHost.CreateAsync(); var actor = type == "consultant" ? h.Consultant : type == "accountant" ? h.Accountant : new Actor(ActorType.Owner);
        await Assert.ThrowsAsync<BookingException>(() => new CatalogDeletionService(h.Db, h.H.Clock).DeleteAsync(CatalogKind.Area, h.H.Homestay.AreaId, actor));
    }
}
