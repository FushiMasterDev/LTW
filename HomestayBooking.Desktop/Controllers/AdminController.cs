using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomestayBooking.Desktop.Models;

namespace HomestayBooking.Desktop.Controllers;

internal sealed class AdminController
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly string _baseUrl = DesktopSettings.ApiBaseUrl.TrimEnd('/') + "/api/desktop/";
    private readonly string _token;

    public AdminController(EmployeeSession session) => _token = session.AccessToken;

    public Task<T?> GetAsync<T>(string path) => SendAsync<T>(HttpMethod.Get, path);
    public Task<T?> PostAsync<T>(string path, object body) => SendAsync<T>(HttpMethod.Post, path, body);
    public Task<T?> PutAsync<T>(string path, object body) => SendAsync<T>(HttpMethod.Put, path, body);
    public Task<T?> DeleteAsync<T>(string path) => SendAsync<T>(HttpMethod.Delete, path);

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, _baseUrl + path.TrimStart('/'));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        using var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            string? detail = null;
            try
            {
                using var document = JsonDocument.Parse(text);
                if (document.RootElement.TryGetProperty("detail", out var d)) detail = d.GetString();
                if (string.IsNullOrWhiteSpace(detail) && document.RootElement.TryGetProperty("message", out var m)) detail = m.GetString();
            }
            catch (JsonException) { }
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                AppSession.Current = null;
                Application.Restart();
                throw new OperationCanceledException("Phiên đăng nhập đã hết hạn.");
            }
            if (response.StatusCode == HttpStatusCode.NotFound) throw new InvalidOperationException("Máy chủ chưa cung cấp API này cho ứng dụng Desktop.");
            throw new InvalidOperationException(detail ?? $"Yêu cầu thất bại ({(int)response.StatusCode}).");
        }
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0) return default;
        return await response.Content.ReadFromJsonAsync<T>(Json);
    }
}
