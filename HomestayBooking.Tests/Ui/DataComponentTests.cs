using HomestayBooking.Tests.TagHelpers;
using HomestayBooking.Web.Models.ViewModels.Ui;
using HomestayBooking.Web.TagHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace HomestayBooking.Tests.Ui;

public class DataComponentTests
{
    [Theory]
    [InlineData(4.76, 12, "4.8")]
    [InlineData(4.75, 1, "4.8")]
    public async Task RatingRoundsAndAnnouncesBothScoreAndCount(double value, int count, string expected)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            var html = await TagHelperTests.RenderAsync(new RatingTagHelper(TagHelperTests.Localizer()) { Value = value, Count = count }, "rating");
            Assert.Contains(expected, html); Assert.Contains("aria-label=", html);
            Assert.Contains($"[en] Điểm {expected} trên 5, từ {count} đánh giá", html);
            Assert.Contains("aria-hidden=\"true\"", html); Assert.Contains("fx-rating", html);
            Assert.Contains("role=\"img\"", html);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public async Task NoReviewsShowsHonestEmptyStateInsteadOfAnArtificialScore()
    {
        var html = await TagHelperTests.RenderAsync(new RatingTagHelper(TagHelperTests.Localizer()) { Value = 4.8, Count = 0 }, "rating");
        Assert.Contains("[en] Chưa có đánh giá", html); Assert.DoesNotContain("4.8", html);
    }

    [Fact]
    public async Task SingleReviewShowsScoreOutOfFiveWithoutCount()
    {
        var html = await TagHelperTests.RenderAsync(new RatingTagHelper(TagHelperTests.Localizer()) { Value = 4, Single = true }, "rating");
        Assert.Contains("4/5", html); Assert.Contains("[en] Điểm 4 trên 5", html);
        Assert.Contains("role=\"img\"", html); Assert.DoesNotContain("Chưa có đánh giá", html);
    }

    [Theory]
    [InlineData("asc", "desc")] [InlineData("desc", "asc")] [InlineData("", "desc")]
    public void SortLinkKeepsRepeatedFiltersAndResetsPagination(string direction, string expected)
    {
        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["Q"] = "Tây Hồ & cây xanh", ["AmenityIds"] = new[] { "1", "2" }, ["sort"] = "price", ["dir"] = direction, ["page"] = "7",
        });
        var link = TableSortLink.Build("/Homestays", query, "price");
        Assert.StartsWith("/Homestays?", link);
        var parsed = QueryHelpers.ParseQuery(link[link.IndexOf('?')..]);
        Assert.Equal("Tây Hồ & cây xanh", parsed["Q"]);
        Assert.Equal(new[] { "1", "2" }, parsed["AmenityIds"].ToArray());
        Assert.Equal(expected, parsed["dir"]); Assert.Equal("price", parsed["sort"]); Assert.False(parsed.ContainsKey("page"));
        var other = QueryHelpers.ParseQuery(TableSortLink.Build("/Homestays", query, "rating").Split('?')[1]);
        Assert.Equal("asc", other["dir"]); Assert.Equal("rating", other["sort"]);
    }
}
