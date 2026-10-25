namespace HomestayBooking.Desktop.Models;

internal sealed record EmployeeSession(string EmployeeCode, string FullName, string Position, string Department, string AccessToken, DateTime ExpiresAtUtc);

internal static class AppSession
{
    public static EmployeeSession? Current { get; set; }
}

internal static class DesktopSettings
{
    public static string ApiBaseUrl => Properties.Settings.Default.ApiBaseUrl.TrimEnd('/');
}
