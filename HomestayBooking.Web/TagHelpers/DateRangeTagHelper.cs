using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.TagHelpers;

/// <summary>&lt;date-range from="@b.CheckIn" to="@b.CheckOut" /&gt; → "10/11/2026 – 12/11/2026 · 2 đêm" (lưu trú [nhận, trả)).</summary>
[HtmlTargetElement("date-range", TagStructure = TagStructure.WithoutEndTag)]
public class DateRangeTagHelper(IStringLocalizer<SharedResource> localizer) : TagHelper
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }

    /// <summary>Ẩn phần "· N đêm" (ví dụ ở bảng đã có cột số đêm).</summary>
    public bool HideNights { get; set; }

    public static int Nights(DateOnly from, DateOnly to) => Math.Max(0, to.DayNumber - from.DayNumber);

    public static string Format(DateOnly from, DateOnly to, string? nightsText) =>
        $"{from:dd/MM/yyyy} – {to:dd/MM/yyyy}" + (nightsText is null ? "" : " · " + nightsText);

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("fx-date-range", HtmlEncoder.Default);
        output.Content.SetContent(Format(From, To, HideNights ? null : localizer["{0} đêm", Nights(From, To)].Value));
    }
}
