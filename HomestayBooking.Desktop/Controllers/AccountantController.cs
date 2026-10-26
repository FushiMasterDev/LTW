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

    public Task<PagedDto<ReceiptDto>?> GetReceiptsAsync(string status, int page) => SendAsync<PagedDto<ReceiptDto>>(HttpMethod.Get, $"accountant/receipts?status={Uri.EscapeDataString(status)}&page={page}");
    public Task<object?> ApproveReceiptAsync(int id) => SendAsync<object>(HttpMethod.Post, $"accountant/receipts/{id}/approve");
    public Task<object?> RejectReceiptAsync(int id, string reason) => SendAsync<object>(HttpMethod.Post, $"accountant/receipts/{id}/reject", new RejectReceiptRequest { Reason = reason });
    public Task<List<HolidayDto>?> GetHolidaysAsync() => SendAsync<List<HolidayDto>>(HttpMethod.Get, "accountant/holidays");
    public Task<List<AreaDto>?> GetAreasAsync() => SendAsync<List<AreaDto>>(HttpMethod.Get, "accountant/areas");
    public Task<List<CatalogItemDto>?> GetRoomCategoriesAsync() => SendAsync<List<CatalogItemDto>>(HttpMethod.Get, "accountant/room-categories");
    public Task<List<CatalogItemDto>?> GetAmenitiesAsync() => SendAsync<List<CatalogItemDto>>(HttpMethod.Get, "accountant/amenities");
    public Task<PagedDto<BankTransactionDto>?> GetBankTransactionsAsync(string? status, int page) => SendAsync<PagedDto<BankTransactionDto>>(HttpMethod.Get, $"accountant/bank-transactions?status={Uri.EscapeDataString(status ?? "")}&page={page}");
    public Task<PagedDto<AdditionalDepositDto>?> GetAdditionalDepositsAsync(int page) => SendAsync<PagedDto<AdditionalDepositDto>>(HttpMethod.Get, $"accountant/additional-deposits?page={page}");
    public Task<object?> RefundTransactionAsync(int id, string bankRef, string? note) => SendAsync<object>(HttpMethod.Post, $"accountant/bank-transactions/{id}/refund", new { bankRef, note });
    public Task<object?> ConfirmAdditionalDepositAsync(int id, string note) => SendAsync<object>(HttpMethod.Post, $"accountant/additional-deposits/{id}/confirm", new { note });
    public Task<object?> RejectAdditionalDepositAsync(int id, string note) => SendAsync<object>(HttpMethod.Post, $"accountant/additional-deposits/{id}/reject", new { note });
    public async Task<(byte[] Bytes, string ContentType)> GetReceiptFileAsync(int id)
    {
        using var request = CreateRequest(HttpMethod.Get, $"accountant/receipts/{id}/file");
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
        var text = await response.Content.ReadAsStringAsync();
        string? detail = null;
        try { var error = JsonSerializer.Deserialize<AccountantApiError>(text, Json); detail = error?.Detail ?? error?.Message; } catch (JsonException) { }
        var message = detail ?? $"Yêu cầu thất bại ({(int)response.StatusCode}).";
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            AppSession.Current = null;
            Application.Restart();
            throw new OperationCanceledException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }
        if (response.StatusCode == HttpStatusCode.NotFound) message = "Backend chưa cung cấp REST API này cho Desktop.";
        throw new InvalidOperationException(message);
    }
}
