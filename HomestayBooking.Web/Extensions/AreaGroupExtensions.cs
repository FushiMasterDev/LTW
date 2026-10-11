using HomestayBooking.Web.Models.Entities;

namespace HomestayBooking.Web.Extensions;

public static class AreaGroupExtensions
{
    /// <summary>Tên hiển thị tiếng Việt, đồng thời là khóa trong SharedResource để dịch.</summary>
    public static string DisplayName(this AreaGroup group) => group switch
    {
        AreaGroup.InnerCity => "Nội thành",
        AreaGroup.Suburb => "Ngoại thành",
        AreaGroup.OtherProvince => "Tỉnh khác",
        _ => group.ToString(),
    };
}
