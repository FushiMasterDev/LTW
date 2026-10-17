using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HomestayBooking.Web.TagHelpers;

/// <summary>&lt;money value="@x" /&gt; → &lt;span class="fx-money"&gt;1.500.000đ&lt;/span&gt;. VND luôn định dạng theo vi-VN, kể cả khi giao diện tiếng Anh.</summary>
[HtmlTargetElement("money", TagStructure = TagStructure.WithoutEndTag)]
public class MoneyTagHelper : TagHelper
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    public decimal? Value { get; set; }

    public static string Format(decimal? value) => value is decimal v ? v.ToString("N0", Vietnamese) + "đ" : "—";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("fx-money", HtmlEncoder.Default);
        output.Content.SetContent(Format(Value));
    }
}
