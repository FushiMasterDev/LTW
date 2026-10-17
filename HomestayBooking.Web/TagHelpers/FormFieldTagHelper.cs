using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HomestayBooking.Web.TagHelpers;

/// <summary>
/// Một dòng form: label + ô nhập + gợi ý + lỗi validation, cùng markup các form hiện có
/// (form-label, form-control/form-select, span.text-danger.small). Dựng lại bằng chính Label/Input/Select/TextArea/
/// ValidationMessage TagHelper của ASP.NET nên tên trường, data-val-* và giá trị đã nhập giữ nguyên như viết tay.
/// <code>&lt;form-field asp-for="Name" hint="..." /&gt;  &lt;form-field asp-for="AreaId" type="select" asp-items="Model.Areas" /&gt;</code>
/// </summary>
[HtmlTargetElement("form-field", TagStructure = TagStructure.WithoutEndTag)]
public class FormFieldTagHelper(IHtmlGenerator generator) : TagHelper
{
    [HtmlAttributeName("asp-for")] public ModelExpression For { get; set; } = null!;

    /// <summary>Kiểu input HTML (text, email, tel, number, date, password…), hoặc "textarea", "select". Bỏ trống thì suy từ kiểu dữ liệu như input thường.</summary>
    public string? Type { get; set; }

    [HtmlAttributeName("asp-items")] public IEnumerable<SelectListItem>? Items { get; set; }

    /// <summary>Dòng đầu tiên của select (ví dụ "— Chọn khu vực —"), giá trị rỗng.</summary>
    public string? Placeholder { get; set; }

    public string? Hint { get; set; }
    public int Rows { get; set; } = 4;
    public bool Autofocus { get; set; }

    /// <summary>Class thêm cho khối bọc (mặc định mb-3).</summary>
    public string? Wrapper { get; set; }

    [ViewContext, HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var id = TagBuilder.CreateSanitizedId(For.Name, "_");
        var hintId = id + "-hint";
        var isCheckbox = Type == "checkbox" || (Type is null && For.Metadata.UnderlyingOrModelType == typeof(bool));

        var label = await RunAsync(new LabelTagHelper(generator) { For = For, ViewContext = ViewContext }, "label",
            new() { { "class", isCheckbox ? "form-check-label" : "form-label" } });

        var controlAttributes = new TagHelperAttributeList();
        var tag = Type switch { "textarea" => "textarea", "select" => "select", _ => "input" };
        controlAttributes.Add("class", tag == "select" ? "form-select" : isCheckbox ? "form-check-input" : "form-control");
        if (Hint is not null) controlAttributes.Add("aria-describedby", hintId);
        if (Autofocus) controlAttributes.Add("autofocus", "autofocus");
        if (tag == "textarea") controlAttributes.Add("rows", Rows.ToString());
        foreach (var attribute in output.Attributes.Where(a => a.Name is "placeholder" or "autocomplete" or "inputmode" or "min" or "max" or "step" or "readonly" or "disabled"))
            controlAttributes.Add(attribute);
        if (tag == "input" && Placeholder is not null) controlAttributes.SetAttribute("placeholder", Placeholder);
        // InputTagHelper đọc lại thuộc tính type từ context khi InputTypeName được đặt
        var inputType = isCheckbox ? "checkbox" : Type;
        if (tag == "input" && inputType is not null) controlAttributes.SetAttribute("type", inputType);

        TagHelperOutput control;
        if (tag == "textarea")
            control = await RunAsync(new TextAreaTagHelper(generator) { For = For, ViewContext = ViewContext }, tag, controlAttributes);
        else if (tag == "select")
        {
            var select = new SelectTagHelper(generator) { For = For, Items = Items, ViewContext = ViewContext };
            var selectContext = Context(controlAttributes);
            select.Init(selectContext);
            var placeholder = Placeholder is null ? "" : $"<option value=\"\">{HtmlEncoder.Default.Encode(Placeholder)}</option>";
            control = new TagHelperOutput(tag, controlAttributes, (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            control.Content.AppendHtml(placeholder);
            await select.ProcessAsync(selectContext, control);
        }
        else
            control = await RunAsync(new InputTagHelper(generator) { For = For, InputTypeName = inputType, ViewContext = ViewContext },
                tag, controlAttributes);

        var message = await RunAsync(new ValidationMessageTagHelper(generator) { For = For, ViewContext = ViewContext }, "span",
            new() { { "class", "text-danger small" } });

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.RemoveAll("placeholder"); output.Attributes.RemoveAll("autocomplete"); output.Attributes.RemoveAll("inputmode");
        foreach (var name in new[] { "min", "max", "step", "readonly", "disabled" }) output.Attributes.RemoveAll(name);
        output.AddClass(Wrapper ?? "mb-3", HtmlEncoder.Default);
        if (isCheckbox) output.AddClass("form-check", HtmlEncoder.Default);
        output.AddClass("fx-field", HtmlEncoder.Default);
        if (isCheckbox) output.Content.AppendHtml(control).AppendHtml(label);
        else output.Content.AppendHtml(label).AppendHtml(control);
        if (Hint is not null) output.Content.AppendHtml($"<div class=\"form-text\" id=\"{hintId}\">{HtmlEncoder.Default.Encode(Hint)}</div>");
        output.Content.AppendHtml(message);
    }

    private static TagHelperContext Context(TagHelperAttributeList attributes) =>
        new(new TagHelperAttributeList(attributes), new Dictionary<object, object>(), Guid.NewGuid().ToString("N"));

    private static async Task<TagHelperOutput> RunAsync(TagHelper helper, string tag, TagHelperAttributeList attributes)
    {
        var context = Context(attributes);
        var output = new TagHelperOutput(tag, attributes, (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()))
        {
            TagMode = tag == "input" ? TagMode.SelfClosing : TagMode.StartTagAndEndTag,
        };
        helper.Init(context);
        await helper.ProcessAsync(context, output);
        return output;
    }
}
