using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HomestayBooking.Web.DesktopApi;

public sealed class DesktopApiOptions
{
    public const string Section = "DesktopApi";
    public string? SigningKey { get; set; }
    [Required] public string Issuer { get; set; } = "FujiHomestay";
    [Required] public string Audience { get; set; } = "FujiDesktop";
    [Range(1, 1440)] public int TokenMinutes { get; set; } = 60;
    [Range(1, 1000)] public int LoginPermitLimit { get; set; } = 10;
    [Range(1, 3600)] public int LoginWindowSeconds { get; set; } = 60;
}

/// <summary>Cấu hình bất biến trong một lần chạy. Cấu hình desktop sai chỉ tắt desktop, không làm web ngừng khởi động.</summary>
public sealed class DesktopApiRuntime
{
    public DesktopApiOptions Options { get; }
    public bool Enabled { get; }
    public SymmetricSecurityKey Key { get; }

    public DesktopApiRuntime(IOptions<DesktopApiOptions> options, IHostEnvironment environment, ILogger<DesktopApiRuntime> logger)
    {
        try { Options = options.Value; }
        catch (Exception ex) when (ex is OptionsValidationException or InvalidOperationException)
        {
            Options = new();
            Key = new(RandomNumberGenerator.GetBytes(64));
            logger.LogWarning("Desktop API is disabled: invalid DesktopApi configuration.");
            return;
        }
        var configured = Options.SigningKey;
        var ephemeral = string.IsNullOrWhiteSpace(configured)
            && (environment.IsDevelopment() || environment.IsEnvironment("Testing") || environment.IsEnvironment("Test"));
        Enabled = ephemeral || (!string.IsNullOrWhiteSpace(configured) && Encoding.UTF8.GetByteCount(configured) >= 32);
        Key = new(Enabled && !ephemeral ? Encoding.UTF8.GetBytes(configured!) : RandomNumberGenerator.GetBytes(64));
        if (!Enabled) logger.LogWarning("Desktop API is disabled: DesktopApi:SigningKey must contain at least 32 UTF-8 bytes. Web remains available.");
        else if (ephemeral) logger.LogInformation("Desktop API uses an ephemeral development/test signing key; restarting invalidates tokens.");
    }
}
