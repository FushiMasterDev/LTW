namespace HomestayBooking.Web.Data;

public sealed class DatabaseOptions
{
    public const string Section = "Database";

    /// <summary>Cho phép tự áp migration khi khởi động; mặc định phải triển khai schema trước.</summary>
    public bool AutoMigrate { get; set; }
}
