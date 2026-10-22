using HomestayBooking.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomestayBooking.Web.Data;

/// <summary>
/// Ảnh minh họa cho homestay mẫu (wwwroot/images/demo, nguồn và giấy phép ở CREDITS.md).
/// Chỉ chạy ở môi trường Development và chỉ thêm cho homestay chưa có ảnh nào: không đụng tới ảnh thật.
/// </summary>
public static partial class SeedData
{
    public const string DemoImageFolder = "/images/demo/";

    // Ảnh bìa nội thành / ngoại thành. Mỗi nhóm đủ ảnh cho số homestay mẫu (25 nội thành, 10 ngoại thành)
    // nên không homestay nào trùng ảnh bìa; thêm homestay thì xoay vòng lại trong nhóm.
    private static readonly string[] DemoCityCovers =
    [
        "veranda-garden", "garden-door", "bright-living", "living-plants", "wooden-dining", "loft-living", "window-nook",
        "sofa-corner", "breakfast-table", "room-warm-wood", "room-studio", "room-classic", "room-city", "room-timber",
        "room-desk", "room-bright", "room-rose", "room-navy", "room-heritage", "living-white", "dining-tulips",
        "dining-window", "dining-sunlit", "living-cane", "living-lounge",
    ];

    private static readonly string[] DemoSuburbCovers =
    [
        "valley-village", "garden-pool", "tea-hills", "rice-terraces", "terraces-golden",
        "green-hills", "terraces-green", "misty-hills", "mountain-sunrise", "sapa-terraces",
    ];

    private static readonly string[] DemoRooms = ["room-warm-wood", "room-studio", "room-classic", "room-city", "room-timber", "room-desk"];

    private static readonly string[] DemoCommonSpaces = ["sofa-corner", "breakfast-table", "living-plants", "window-nook", "wooden-dining"];

    private static async Task SeedDemoImagesAsync(ApplicationDbContext db, IServiceProvider services)
    {
        if (services.GetService(typeof(IWebHostEnvironment)) is not IWebHostEnvironment env)
        {
            return;
        }
        // Thiếu bộ ảnh (ví dụ chưa chép wwwroot/images/demo) thì để giao diện hiện ảnh thay thế
        if (!File.Exists(Path.Combine(env.WebRootPath, "images", "demo", DemoRooms[0] + ".webp")))
        {
            return;
        }

        var homestays = await db.Homestays
            .Where(h => !h.Images.Any())
            .Select(h => new { h.Id, h.Area.AreaGroup })
            .OrderBy(h => h.Id)
            .ToListAsync();
        if (homestays.Count == 0)
        {
            return;
        }

        // Đếm riêng từng nhóm để ảnh bìa xoay vòng trong đúng danh sách của nhóm đó
        int cityIndex = 0, suburbIndex = 0;
        for (var i = 0; i < homestays.Count; i++)
        {
            var h = homestays[i];
            var cover = h.AreaGroup == AreaGroup.Suburb
                ? DemoSuburbCovers[suburbIndex++ % DemoSuburbCovers.Length]
                : DemoCityCovers[cityIndex++ % DemoCityCovers.Length];
            var files = new[]
            {
                cover,
                DemoRooms[i % DemoRooms.Length],
                DemoRooms[(i + 3) % DemoRooms.Length],
                DemoCommonSpaces[(i + 1) % DemoCommonSpaces.Length],
                "bathroom-white",
            }.Distinct().ToList();
            for (var k = 0; k < files.Count; k++)
            {
                db.HomestayImages.Add(new HomestayImage
                {
                    HomestayId = h.Id,
                    Url = DemoImageFolder + files[k] + ".webp",
                    SortOrder = k,
                    IsCover = k == 0,
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
