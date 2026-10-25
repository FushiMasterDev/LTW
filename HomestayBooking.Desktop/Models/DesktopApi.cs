using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace HomestayBooking.Desktop.Models;

internal static class DesktopApi
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static async Task<T?> GetAsync<T>(string path) => await SendAsync<T>(HttpMethod.Get, path, null);
    public static async Task<T?> PostAsync<T>(string path, object body) => await SendAsync<T>(HttpMethod.Post, path, body);
    public static async Task<T?> PutAsync<T>(string path, object body) => await SendAsync<T>(HttpMethod.Put, path, body);
    private static async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body)
    {
        var session = AppSession.Current ?? throw new InvalidOperationException("Phiên đăng nhập đã kết thúc.");
        using var request = new HttpRequestMessage(method, DesktopSettings.ApiBaseUrl + "/api/desktop/" + path.TrimStart('/'));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        using var response = await Client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            AppSession.Current = null;
            MessageBox.Show("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", "Fuji Homestay", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Application.Restart();
            throw new OperationCanceledException();
        }
        if (response.StatusCode == HttpStatusCode.Forbidden) throw new InvalidOperationException("Bạn không có quyền thực hiện thao tác này.");
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(text);
                var detail = doc.RootElement.TryGetProperty("detail", out var d) ? d.GetString() : null;
                var title = doc.RootElement.TryGetProperty("title", out var t) ? t.GetString() : null;
                throw new InvalidOperationException(detail ?? title ?? $"Yêu cầu thất bại ({(int)response.StatusCode}).");
            }
            catch (JsonException) { throw new InvalidOperationException($"Yêu cầu thất bại ({(int)response.StatusCode})."); }
        }
        if (typeof(T) == typeof(object) || response.Content.Headers.ContentLength == 0) return default;
        return await response.Content.ReadFromJsonAsync<T>(Json);
    }
    public static void ShowError(IWin32Window owner, Exception ex) => MessageBox.Show(owner, ex.Message, "Fuji Homestay", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
