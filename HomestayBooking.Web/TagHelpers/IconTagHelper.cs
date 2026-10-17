using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HomestayBooking.Web.TagHelpers;

/// <summary>
/// &lt;icon name="calendar" /&gt; → &lt;i class="bi bi-calendar" aria-hidden="true"&gt;. Có label thì icon đứng một mình mang nghĩa:
/// role="img" + aria-label thay cho aria-hidden.
/// </summary>
[HtmlTargetElement("icon", TagStructure = TagStructure.WithoutEndTag)]
public class IconTagHelper : TagHelper
{
    public string Name { get; set; } = "";
    public string? Label { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "i";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.AddClass("bi", HtmlEncoder.Default);
        output.AddClass(Name.StartsWith("bi-", StringComparison.Ordinal) ? Name : "bi-" + Name, HtmlEncoder.Default);
        if (string.IsNullOrEmpty(Label)) output.Attributes.SetAttribute("aria-hidden", "true");
        else { output.Attributes.SetAttribute("role", "img"); output.Attributes.SetAttribute("aria-label", Label); }
    }
}
