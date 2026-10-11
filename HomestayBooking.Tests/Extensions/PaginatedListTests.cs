using HomestayBooking.Web.Data;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Tests.Extensions;

public class PaginatedListTests
{
    private static async Task<ApplicationDbContext> CreateDbWithAreasAsync(int count)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        db.Areas.AddRange(Enumerable.Range(1, count).Select(i => new Area { Name = $"Khu {i:D2}" }));
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task CreateAsync_ReturnsRequestedPage()
    {
        await using var db = await CreateDbWithAreasAsync(25);

        var page = await PaginatedList<Area>.CreateAsync(db.Areas.OrderBy(a => a.Name), pageIndex: 2, pageSize: 10);

        Assert.Equal(2, page.PageIndex);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(10, page.Count);
        Assert.Equal("Khu 11", page[0].Name);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public async Task CreateAsync_LastPage_HasRemainingItems()
    {
        await using var db = await CreateDbWithAreasAsync(25);

        var page = await PaginatedList<Area>.CreateAsync(db.Areas.OrderBy(a => a.Name), pageIndex: 3, pageSize: 10);

        Assert.Equal(5, page.Count);
        Assert.False(page.HasNextPage);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(99, 3)]
    public async Task CreateAsync_ClampsOutOfRangePage(int? requested, int expected)
    {
        await using var db = await CreateDbWithAreasAsync(25);

        var page = await PaginatedList<Area>.CreateAsync(db.Areas.OrderBy(a => a.Name), requested, pageSize: 10);

        Assert.Equal(expected, page.PageIndex);
        Assert.NotEmpty(page);
    }

    [Fact]
    public async Task CreateAsync_EmptySource_ReturnsSingleEmptyPage()
    {
        await using var db = await CreateDbWithAreasAsync(0);

        var page = await PaginatedList<Area>.CreateAsync(db.Areas.OrderBy(a => a.Name), pageIndex: 1);

        Assert.Empty(page);
        Assert.Equal(0, page.TotalCount);
        Assert.False(page.HasPreviousPage);
        Assert.False(page.HasNextPage);
    }
}
