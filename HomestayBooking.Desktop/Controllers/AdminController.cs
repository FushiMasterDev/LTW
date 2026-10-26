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
    public Task<List<T>> GetAllAsync<T>(string path) => DesktopApi.AllPagesAsync(page => GetAsync<Models.Accountant.PagedDto<T>>(DesktopApi.WithPage(path, page)));

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, _baseUrl + path.TrimStart('/'));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        using var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var message = DesktopApi.ErrorMessage(response.StatusCode, await response.Content.ReadAsStringAsync());
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                AppSession.Current = null;
                Application.Restart();
                throw new OperationCanceledException("Phiên đăng nhập đã hết hạn.");
            }
            throw new InvalidOperationException(message);
        }
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0) return default;
        return await response.Content.ReadFromJsonAsync<T>(Json);
    }
}
