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
    public static Task<List<T>> GetAllAsync<T>(string path) => AllPagesAsync(page => GetAsync<Accountant.PagedDto<T>>(WithPage(path, page)));

    /// <summary>API trả trang 10 mục; gom hết các trang để đổ ô chọn hoặc bảng danh mục.</summary>
    public static async Task<List<T>> AllPagesAsync<T>(Func<int, Task<Accountant.PagedDto<T>?>> getPage)
    {
        var all = new List<T>();
        for (var page = 1; ; page++)
        {
            var result = await getPage(page);
            if (result is null) return all;
            all.AddRange(result.Items);
            if (page >= result.TotalPages) return all;
        }
    }

    public static string WithPage(string path, int page) => $"{path}{(path.Contains('?') ? '&' : '?')}page={page}";

    /// <summary>Đọc ProblemDetails của API; 404 không có detail nghĩa là máy chủ chưa có endpoint đó.</summary>
    public static string ErrorMessage(HttpStatusCode status, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var detail = doc.RootElement.TryGetProperty("detail", out var d) ? d.GetString() : null;
            if (!string.IsNullOrWhiteSpace(detail)) return detail;
            if (status != HttpStatusCode.NotFound && doc.RootElement.TryGetProperty("title", out var t) && t.GetString() is { Length: > 0 } title) return title;
        }
        catch (JsonException) { }
        return status switch
        {
            HttpStatusCode.NotFound => "Máy chủ chưa cung cấp API này cho ứng dụng Desktop.",
            HttpStatusCode.Forbidden => "Bạn không có quyền thực hiện thao tác này.",
            HttpStatusCode.TooManyRequests => "Thao tác quá nhiều lần, vui lòng thử lại sau ít phút.",
            _ => $"Yêu cầu thất bại ({(int)status}).",
        };
    }
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
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(ErrorMessage(response.StatusCode, await response.Content.ReadAsStringAsync()));
        if (typeof(T) == typeof(object) || response.Content.Headers.ContentLength == 0) return default;
        return await response.Content.ReadFromJsonAsync<T>(Json);
    }
    public static void ShowError(IWin32Window owner, Exception ex) => MessageBox.Show(owner, ex.Message, "Fuji Homestay", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
