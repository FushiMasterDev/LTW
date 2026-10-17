using System.Globalization;
using HomestayBooking.Web.Models;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;

namespace HomestayBooking.Web.TagHelpers;

[HtmlTargetElement("rating")]
public class RatingTagHelper(IStringLocalizer<SharedResource> localizer) : TagHelper
{
    public double? Value { get; set; }
    public int Count { get; set; }
    /// <summary>Điểm của một đánh giá đơn lẻ: hiện "4/5", không cần số lượng.</summary>
    public bool Single { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "fx-rating");
        if (Single && Value is double score)
        {
            var text = Math.Round(score, 1, MidpointRounding.AwayFromZero).ToString("0.#", CultureInfo.CurrentCulture);
            output.Attributes.SetAttribute("role", "img");
            output.Attributes.SetAttribute("aria-label", localizer["Điểm {0} trên 5", text].Value);
            output.Content.AppendHtml("<i class=\"bi bi-star-fill\" aria-hidden=\"true\"></i> ");
            output.Content.Append($"{text}/5");
            return;
        }
        if (Count <= 0 || Value is null)
        {
            output.Content.SetContent(localizer["Chưa có đánh giá"].Value);
            return;
        }
        output.Attributes.SetAttribute("role", "img");
        var rating = Math.Round(Value.Value, 1, MidpointRounding.AwayFromZero).ToString("0.0", CultureInfo.CurrentCulture);
        output.Attributes.SetAttribute("aria-label", localizer["Điểm {0} trên 5, từ {1} đánh giá", rating, Count].Value);
        output.Content.AppendHtml("<i class=\"bi bi-star-fill\" aria-hidden=\"true\"></i> ");
        output.Content.Append(rating).Append($" ({Count})");
    }
}
