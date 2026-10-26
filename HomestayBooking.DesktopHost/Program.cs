using HomestayBooking.Web;
using HomestayBooking.Web.Controllers.Api.Desktop;
using HomestayBooking.Web.Data;
using HomestayBooking.Web.DesktopApi;
using HomestayBooking.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

namespace HomestayBooking.DesktopHost;

public partial class DesktopHostProgram
{
    public static async Task Main(string[] args)
    {
        await using var app = await BuildAsync(args);
        await app.RunAsync();
    }

    /// <summary>Dùng cùng host cho lệnh chạy và kiểm thử trên Kestrel.</summary>
    public static async Task<WebApplication> BuildAsync(string[] args)
    {
        var reset = args.Contains("--reset", StringComparer.OrdinalIgnoreCase);
        var hostArgs = args.Where(a => !a.Equals("--reset", StringComparison.OrdinalIgnoreCase)).ToArray();
        var bootstrap = new ConfigurationBuilder().AddEnvironmentVariables("ASPNETCORE_")
            .AddEnvironmentVariables("DOTNET_").AddCommandLine(hostArgs).Build();
        var contentRoot = Path.GetFullPath(bootstrap["contentRoot"] ?? AppContext.BaseDirectory);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            // Configuration is read once; no file watcher is needed for an offline host.
            Args = ["--hostBuilder:reloadConfigOnChange", "false", .. hostArgs],
            ContentRootPath = contentRoot,
            WebRootPath = Path.Combine(contentRoot, "App_Data", "public"),
            EnvironmentName = bootstrap["environment"] ?? Environments.Development,
        });
        // Separate filename avoids inheriting Web's deployment/SQL Server configuration.
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddJsonFile("desktopsettings.json", optional: true)
            .AddJsonFile($"desktopsettings.{builder.Environment.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables("ASPNETCORE_").AddEnvironmentVariables("DOTNET_")
            .AddEnvironmentVariables().AddCommandLine(hostArgs);
        if (string.IsNullOrWhiteSpace(builder.Configuration["urls"])) builder.WebHost.UseUrls("http://localhost:5080");

        var dbPath = Path.GetFullPath(builder.Configuration["DesktopHost:DatabasePath"] ?? Path.Combine("App_Data", "fuji-desktop.db"), contentRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        Directory.CreateDirectory(builder.Environment.WebRootPath);
        builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(contentRoot, "App_Data", "keys")))
            .SetApplicationName("FujiDesktopHost");
        var connection = new SqliteConnectionStringBuilder { DataSource = dbPath, ForeignKeys = true, DefaultTimeout = 30 }.ToString();
        builder.Services.AddHomestayCore(builder.Configuration, o => o.UseSqlite(connection), registerBackgroundJobs: false);
        builder.Services.AddScoped<ApplicationDbContext, DesktopDbContext>();
        builder.Services.AddHostedService<DesktopMaintenanceJob>();
        builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
        builder.Services.AddControllers(o => o.Filters.Add<DesktopSqliteExceptionFilter>(int.MaxValue)).AddApplicationPart(typeof(DesktopAuthController).Assembly)
            .ConfigureApplicationPartManager(parts => parts.FeatureProviders.Add(new DesktopControllerFeatureProvider()))
            .AddDataAnnotationsLocalization(o => o.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new() { Title = "Fuji Desktop API (SQLite)", Version = "v1" });
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
                Description = "Đăng nhập nhân viên hoặc chủ homestay, sau đó nhập accessToken.",
            });
            o.OperationFilter<DesktopSwaggerFilter>();
            o.DocInclusionPredicate((_, api) => api.RelativePath?.StartsWith("api/desktop/", StringComparison.OrdinalIgnoreCase) == true);
            var xml = Path.Combine(AppContext.BaseDirectory, "HomestayBooking.Web.xml");
            if (File.Exists(xml)) o.IncludeXmlComments(xml);
        });

        var app = builder.Build();
        _ = app.Services.GetRequiredService<DesktopApiRuntime>();
        app.UseExceptionHandler(handler => handler.Run(async context =>
        {
            var error = context.Features.Get<IExceptionHandlerFeature>();
            app.Logger.LogError(error?.Error, "Desktop host request failed.");
            await DesktopApiRegistration.WriteProblemAsync(context, 500, "Internal Server Error");
        }));
        app.UseStatusCodePages(async context => await DesktopApiRegistration.WriteProblemAsync(context.HttpContext,
            context.HttpContext.Response.StatusCode, Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(context.HttpContext.Response.StatusCode)));
        // Như web: thông báo luôn tiếng Việt, không theo ngôn ngữ của Windows chạy host.
        app.UseRequestLocalization(o =>
        {
            o.SetDefaultCulture("vi-VN").AddSupportedCultures("vi-VN").AddSupportedUICultures("vi-VN");
            o.RequestCultureProviders.Clear();
        });
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await DesktopDatabase.InitializeAsync(app.Services, reset);
        app.Logger.LogInformation("Desktop SQLite: {DatabasePath}", dbPath);
        return app;
    }
}
