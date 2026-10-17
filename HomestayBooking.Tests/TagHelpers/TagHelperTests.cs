using System.Text.Encodings.Web;
using HomestayBooking.Web;
using HomestayBooking.Web.Extensions;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Models.ViewModels;
using HomestayBooking.Web.TagHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;

namespace HomestayBooking.Tests.TagHelpers;

public sealed class TagHelperTests
{
    [Theory]
    [InlineData(0, "0đ")] [InlineData(1500000, "1.500.000đ")] [InlineData(350500, "350.500đ")] [InlineData(-150000, "-150.000đ")]
    public void MoneyUsesVietnameseGroupingEvenInEnglishUi(decimal value, string expected)
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            Assert.Equal(expected, MoneyTagHelper.Format(value));
        }
        finally { Thread.CurrentThread.CurrentCulture = previous; }
        Assert.Equal("—", MoneyTagHelper.Format(null));
    }

    public static TheoryData<Enum, string> Statuses()
    {
        var data = new TheoryData<Enum, string>();
        foreach (var s in Enum.GetValues<BookingStatus>()) data.Add(s, s.ToneClass());
        foreach (var s in Enum.GetValues<HomestayStatus>()) data.Add(s, s.ToneClass());
        foreach (var s in Enum.GetValues<OwnerRegistrationStatus>()) data.Add(s, s.ToneClass());
        foreach (var s in Enum.GetValues<BankMatchStatus>()) data.Add(s, s.ToneClass());
        foreach (var s in Enum.GetValues<VoucherStatus>()) data.Add(s, s.ToneClass());
        foreach (var s in Enum.GetValues<ChangeRequestStatus>()) data.Add(s, "tone-sky");
        return data;
    }

    [Theory, MemberData(nameof(Statuses))]
    public void StatusBadgeTakesToneFromStatusExtensions(Enum status, string tone)
    {
        var badge = StatusBadgeTagHelper.Describe(status);
        Assert.Equal(tone, badge.Tone);
        Assert.False(string.IsNullOrWhiteSpace(badge.Label));
    }

    [Fact]
    public async Task StatusBadgeRendersLocalizedLabelAndOptionalIcon()
    {
        var html = await RenderAsync(new StatusBadgeTagHelper(Localizer()) { Value = BookingStatus.Confirmed, Icon = true }, "status-badge");
        Assert.Contains("class=\"badge-tone tone-sky\"", html);
        Assert.Contains($"<i class=\"bi {BookingStatus.Confirmed.Icon()}\" aria-hidden=\"true\"></i>", html);
        Assert.Contains("[en] Đã xác nhận", html);
        Assert.Equal("", await RenderAsync(new StatusBadgeTagHelper(Localizer()) { Value = null }, "status-badge"));
    }

    [Theory]
    [InlineData(1, "10/11/2026 – 11/11/2026 · [en] 1 đêm")]
    [InlineData(7, "10/11/2026 – 17/11/2026 · [en] 7 đêm")]
    public async Task DateRangeCountsNightsAsCheckInToCheckOut(int nights, string expected)
    {
        var from = new DateOnly(2026, 11, 10);
        Assert.Equal(nights, DateRangeTagHelper.Nights(from, from.AddDays(nights)));
        var html = await RenderAsync(new DateRangeTagHelper(Localizer()) { From = from, To = from.AddDays(nights) }, "date-range");
        Assert.Contains(expected, html);
        Assert.Equal("10/11/2026 – 12/11/2026", DateRangeTagHelper.Format(from, from.AddDays(2), null));
    }

    [Fact]
    public async Task IconIsHiddenUnlessLabelled()
    {
        Assert.Equal("<i class=\"bi bi-calendar\" aria-hidden=\"true\"></i>", await RenderAsync(new IconTagHelper { Name = "calendar" }, "icon"));
        var labelled = await RenderAsync(new IconTagHelper { Name = "bi-info-circle", Label = "Thông tin" }, "icon");
        Assert.Contains("class=\"bi bi-info-circle\"", labelled); Assert.Contains("role=\"img\"", labelled); Assert.DoesNotContain("aria-hidden", labelled);
    }

    [Fact]
    public async Task FormFieldRendersLabelControlHintAndClientValidation()
    {
        var (generator, viewContext, provider) = Mvc();
        var html = await RenderAsync(new FormFieldTagHelper(generator)
        {
            For = provider.CreateModelExpression((ViewDataDictionary<UiKitViewModel>)viewContext.ViewData, m => m.Phone),
            Type = "tel", Hint = "Gợi ý", ViewContext = viewContext,
        }, "form-field");
        Assert.Contains("class=\"mb-3 fx-field\"", html);
        Assert.Contains("<label class=\"form-label\" for=\"Phone\">", html);
        Assert.Contains("type=\"tel\"", html); Assert.Contains("name=\"Phone\"", html); Assert.Contains("class=\"form-control\"", html);
        Assert.Contains("data-val-required=", html); Assert.Contains("data-val-regex=", html);
        Assert.Contains("aria-describedby=\"Phone-hint\"", html); Assert.Contains("id=\"Phone-hint\"", html);
        Assert.Contains("data-valmsg-for=\"Phone\"", html);
    }

    [Fact]
    public async Task FormFieldSelectKeepsPlaceholderAndItemsAndCheckboxPutsLabelAfter()
    {
        var (generator, viewContext, provider) = Mvc();
        var data = (ViewDataDictionary<UiKitViewModel>)viewContext.ViewData;
        var select = await RenderAsync(new FormFieldTagHelper(generator)
        {
            For = provider.CreateModelExpression(data, m => m.AreaId), Type = "select", Items = data.Model!.Areas,
            Placeholder = "— Chọn —", ViewContext = viewContext,
        }, "form-field");
        Assert.Contains("class=\"form-select\"", select);
        Assert.True(select.IndexOf("<option value=\"\">— Chọn —</option>", StringComparison.Ordinal) < select.IndexOf("Tây Hồ", StringComparison.Ordinal));
        var check = await RenderAsync(new FormFieldTagHelper(generator)
        {
            For = provider.CreateModelExpression(data, m => m.Subscribe), ViewContext = viewContext,
        }, "form-field");
        Assert.Contains("form-check", check); Assert.Contains("type=\"checkbox\"", check);
        Assert.True(check.IndexOf("type=\"checkbox\"", StringComparison.Ordinal) < check.IndexOf("<label", StringComparison.Ordinal));
    }

    private static IStringLocalizer<SharedResource> Localizer()
    {
        var mock = new Mock<IStringLocalizer<SharedResource>>();
        mock.Setup(l => l[It.IsAny<string>()]).Returns((string k) => new LocalizedString(k, "[en] " + k));
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string k, object[] a) => new LocalizedString(k, "[en] " + string.Format(k, a)));
        return mock.Object;
    }

    private static (IHtmlGenerator, ViewContext, ModelExpressionProvider) Mvc()
    {
        var listener = new System.Diagnostics.DiagnosticListener("tests");
        var sp = new ServiceCollection().AddLogging().AddSingleton(listener).AddSingleton<System.Diagnostics.DiagnosticSource>(listener)
            .AddMvc().Services.BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = sp };
        var metadata = sp.GetRequiredService<IModelMetadataProvider>();
        var viewData = new ViewDataDictionary<UiKitViewModel>(metadata, new ModelStateDictionary()) { Model = new UiKitViewModel() };
        var viewContext = new ViewContext(new ActionContext(http, new RouteData(), new ActionDescriptor()), Mock.Of<IView>(), viewData,
            Mock.Of<ITempDataDictionary>(), TextWriter.Null, new HtmlHelperOptions());
        return (sp.GetRequiredService<IHtmlGenerator>(), viewContext, new ModelExpressionProvider(metadata));
    }

    private static async Task<string> RenderAsync(TagHelper helper, string tag)
    {
        var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), Guid.NewGuid().ToString("N"));
        var output = new TagHelperOutput(tag, new TagHelperAttributeList(), (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
        helper.Init(context);
        await helper.ProcessAsync(context, output);
        using var writer = new StringWriter();
        output.WriteTo(writer, HtmlEncoder.Create(System.Text.Unicode.UnicodeRanges.All));
        return writer.ToString();
    }
}
