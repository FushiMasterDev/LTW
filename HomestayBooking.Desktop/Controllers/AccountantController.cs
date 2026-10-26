using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HomestayBooking.Desktop.Models;
using HomestayBooking.Desktop.Models.Accountant;

namespace HomestayBooking.Desktop.Controllers;

internal sealed class AccountantController
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly string _baseUrl;
    private readonly string _token;

    public AccountantController(EmployeeSession session)
    {
        _baseUrl = DesktopSettings.ApiBaseUrl.TrimEnd('/') + "/api/desktop/";
        _token = session.AccessToken;
    }

    public Task<PagedDto<ReceiptDto>?> GetReceiptsAsync(string status, int page) => SendAsync<PagedDto<ReceiptDto>>(HttpMethod.Get, $"receipts?status={Uri.EscapeDataString(status)}&page={page}");
    public Task<object?> ApproveReceiptAsync(int id) => SendAsync<object>(HttpMethod.Post, $"receipts/{id}/approve");
    public Task<object?> RejectReceiptAsync(int id, string reason) => SendAsync<object>(HttpMethod.Post, $"receipts/{id}/reject", new RejectReceiptRequest { Reason = reason });
    public Task<List<HolidayDto>> GetHolidaysAsync() => GetAllAsync<HolidayDto>("holidays");
    public Task<List<AreaDto>> GetAreasAsync() => GetAllAsync<AreaDto>("areas");
    public Task<List<CatalogItemDto>> GetRoomCategoriesAsync() => GetAllAsync<CatalogItemDto>("room-categories");
    public Task<List<CatalogItemDto>> GetAmenitiesAsync() => GetAllAsync<CatalogItemDto>("amenities");
    /// <summary>status null = các khoản cần xử lý tay (chuyển thiếu, đến muộn, không rõ đơn).</summary>
    public Task<PagedDto<BankTransactionDto>?> GetBankTransactionsAsync(string? status, int page) => SendAsync<PagedDto<BankTransactionDto>>(HttpMethod.Get, status is null ? $"bank-transactions?page={page}" : $"bank-transactions?status={Uri.EscapeDataString(status)}&page={page}");
    public Task<PagedDto<AdditionalDepositDto>?> GetAdditionalDepositsAsync(int page) => SendAsync<PagedDto<AdditionalDepositDto>>(HttpMethod.Get, $"additional-deposits?page={page}");
    public Task<object?> MatchTransactionAsync(int id, string bookingCode, string? note) => SendAsync<object>(HttpMethod.Post, $"bank-transactions/{id}/match", new { bookingCode, note });
    public Task<object?> IgnoreTransactionAsync(int id, string reason) => SendAsync<object>(HttpMethod.Post, $"bank-transactions/{id}/ignore", new { reason });
    public Task<object?> RefundTransactionAsync(int id, string bankRef, string? note) => SendAsync<object>(HttpMethod.Post, $"bank-transactions/{id}/refund", new { bankRef, note });
    public Task<object?> ConfirmAdditionalDepositAsync(int changeRequestId, string note) => SendAsync<object>(HttpMethod.Post, $"additional-deposits/{changeRequestId}/confirm", new { note });
    public Task<object?> RejectAdditionalDepositAsync(int changeRequestId, string reason) => SendAsync<object>(HttpMethod.Post, $"additional-deposits/{changeRequestId}/reject", new { reason });
    private Task<List<T>> GetAllAsync<T>(string path) => DesktopApi.AllPagesAsync(page => SendAsync<PagedDto<T>>(HttpMethod.Get, DesktopApi.WithPage(path, page)));
    public async Task<(byte[] Bytes, string ContentType)> GetReceiptFileAsync(int id)
    {
        using var request = CreateRequest(HttpMethod.Get, $"receipts/{id}/file");
        using var response = await Http.SendAsync(request);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadAsByteArrayAsync(), response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    private async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = CreateRequest(method, path);
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        using var response = await Http.SendAsync(request);
        await EnsureSuccessAsync(response);
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0) return default;
        return await response.Content.ReadFromJsonAsync<T>(Json);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, _baseUrl + path.TrimStart('/'));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return request;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var message = DesktopApi.ErrorMessage(response.StatusCode, await response.Content.ReadAsStringAsync());
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            AppSession.Current = null;
            Application.Restart();
            throw new OperationCanceledException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }
        throw new InvalidOperationException(message);
    }
}
