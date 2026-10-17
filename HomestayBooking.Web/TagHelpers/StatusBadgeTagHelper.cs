using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.TagHelpers;

/// <summary>
/// &lt;status-badge value="@Model.Status" icon="true" /&gt; → &lt;span class="badge-tone tone-…"&gt;nhãn&lt;/span&gt;.
/// Nhãn và tone lấy từ *StatusExtensions sẵn có; enum chưa hỗ trợ thì in tên enum với tone-slate.
/// </summary>
[HtmlTargetElement("status-badge", TagStructure = TagStructure.WithoutEndTag)]
public class StatusBadgeTagHelper(IStringLocalizer<SharedResource> localizer) : TagHelper
{
    public Enum? Value { get; set; }

    /// <summary>Hiện biểu tượng trạng thái (chỉ trạng thái đơn có biểu tượng riêng).</summary>
    public bool Icon { get; set; }

    public record Badge(string Label, string Tone, string? Icon);

    public static Badge Describe(Enum value) => value switch
    {
        BookingStatus s => new(s.DisplayName(), s.ToneClass(), s.Icon()),
        HomestayStatus s => new(s.DisplayName(), s.ToneClass(), null),
        OwnerRegistrationStatus s => new(s.DisplayName(), s.ToneClass(), null),
        BankMatchStatus s => new(s.DisplayName(), s.ToneClass(), null),
        VoucherStatus s => new(s.DisplayName(), s.ToneClass(), null),
        // _ChangeHistory hiện dùng một tone cố định cho yêu cầu đổi/hủy
        ChangeRequestStatus s => new(s.DisplayName(), "tone-sky", null),
        _ => new(value.ToString(), "tone-slate", null),
    };

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Value is null) { output.SuppressOutput(); return; }
        var badge = Describe(Value);
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("badge-tone", System.Text.Encodings.Web.HtmlEncoder.Default);
        output.AddClass(badge.Tone, System.Text.Encodings.Web.HtmlEncoder.Default);
        if (Icon && badge.Icon is not null) output.Content.AppendHtml($"<i class=\"bi {badge.Icon}\" aria-hidden=\"true\"></i> ");
        output.Content.Append(localizer[badge.Label]);
    }
}
