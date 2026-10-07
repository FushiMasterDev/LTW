# TỔNG QUAN QUY TRÌNH THỰC HIỆN BÀI TẬP LỚN LẬP TRÌNH WEB

**Đề tài:** Hệ thống đặt phòng homestay cho công ty trung gian (Công ty Fuji)
**Công nghệ:** ASP.NET Core MVC (.NET 8) · Entity Framework Core 8 · ASP.NET Core Identity · SQL Server · Bootstrap 5 · jQuery
**Nhóm:** Phạm Ngọc Phú (trưởng nhóm) · Lã Quốc Thái · Nguyễn Quang Nghĩa · Trần Kỳ Vĩ
**Đầu vào:** Báo cáo *Phân tích thiết kế yêu cầu* (`phantichyeucaubanchinh.docx`): khảo sát, quy tắc nghiệp vụ BR-01…BR-11, yêu cầu FR-01…FR-24

---

## Mục lục

1. [Mục tiêu và tiêu chí hoàn thành](#1-mục-tiêu-và-tiêu-chí-hoàn-thành)
2. [Mô hình phát triển](#2-mô-hình-phát-triển)
3. [Phạm vi hệ thống](#3-phạm-vi-hệ-thống)
4. [Đối chiếu yêu cầu của hướng dẫn BTL](#4-đối-chiếu-yêu-cầu-của-hướng-dẫn-btl)
5. [Kiến trúc và công nghệ](#5-kiến-trúc-và-công-nghệ)
6. [Thiết kế cơ sở dữ liệu (tổng quan)](#6-thiết-kế-cơ-sở-dữ-liệu-tổng-quan)
7. [Quy tắc nghiệp vụ áp dụng trong code](#7-quy-tắc-nghiệp-vụ-áp-dụng-trong-code)
8. [Danh sách REST API](#8-danh-sách-rest-api)
9. [Sitemap](#9-sitemap)
10. [Kế hoạch triển khai theo giai đoạn](#10-kế-hoạch-triển-khai-theo-giai-đoạn)
11. [Phân công công việc](#11-phân-công-công-việc)
12. [Quy ước làm việc nhóm](#12-quy-ước-làm-việc-nhóm)
13. [Kế hoạch kiểm thử](#13-kế-hoạch-kiểm-thử)
14. [Bố cục báo cáo và sản phẩm nộp](#14-bố-cục-báo-cáo-và-sản-phẩm-nộp)
15. [Rủi ro và cách xử lý](#15-rủi-ro-và-cách-xử-lý)

---

## 1. Mục tiêu và tiêu chí hoàn thành

| Mục tiêu (theo hướng dẫn) | Cách nhóm đạt được |
|---|---|
| Vận dụng kiến thức Lập trình web để xây dựng hệ thống thực tiễn | Xây dựng hệ thống đặt phòng 4 lớp người dùng dựa trên khảo sát thực tế tại Fuji |
| Tiếp cận quy trình phát triển phần mềm và công cụ hỗ trợ | Agile/Scrum rút gọn, Visual Studio/VS Code, .NET CLI, NuGet, Git/GitHub, draw.io, xUnit, Swagger |
| Rèn kỹ năng làm việc nhóm, báo cáo, thuyết trình | Họp sprint hằng tuần, review code qua Pull Request, viết báo cáo song song với code |

**Hệ thống được coi là hoàn thành khi:**

- Đủ tất cả mục trong checklist ở [mục 4](#4-đối-chiếu-yêu-cầu-của-hướng-dẫn-btl).
- Chạy được trọn luồng: khách đặt phòng → chủ homestay xác nhận → admin xác nhận cọc → nhận phòng → hoàn thành → khách đánh giá.
- Không thể tạo hai đơn trùng phòng trùng ngày (kể cả khi gửi đồng thời).
- Có script `.sql` dựng lại được CSDL kèm dữ liệu mẫu trên máy khác.

---

## 2. Mô hình phát triển

Áp dụng **Agile/Scrum rút gọn**, mỗi sprint 1 tuần:

```
Đầu tuần: Sprint planning (30')  →  Code + review PR  →  Cuối tuần: Demo + Retrospective (30')
```

- **Product backlog:** lấy từ FR trong báo cáo PTTKYC, đã rút gọn ở [mục 3](#3-phạm-vi-hệ-thống).
- **Công cụ quản lý:** GitHub Projects (bảng Kanban: *Todo → In progress → Review → Done*).
- **Definition of Done** của một chức năng:
  1. Có Controller + View + Validation, giao diện responsive.
  2. Chuỗi hiển thị đã đưa vào file tài nguyên (i18n).
  3. Đã kiểm tra phân quyền.
  4. Có ít nhất 1 test case trong bảng kiểm thử.
  5. PR được 1 thành viên khác review và merge vào `develop`.
  6. Đã chụp màn hình cho mục 5.3 của báo cáo.

---

## 3. Phạm vi hệ thống

### 3.1. Lớp người dùng

| Lớp người dùng | Đăng nhập? | Chức năng chính | FR gốc |
|---|---|---|---|
| **Khách hàng** | Không bắt buộc | Tìm kiếm, lọc homestay; xem chi tiết, lịch trống, báo giá; đặt phòng bằng SĐT; tra cứu đơn bằng mã + SĐT; gửi yêu cầu hủy; đánh giá sau khi trả phòng | FR-01…06 |
| **Chủ homestay** | Có (`Owner`) | Quản lý homestay, phòng, ảnh, giá 3 mức, mô tả, chính sách; khóa ngày (khách từ kênh khác, bảo trì); xác nhận/từ chối đơn; xem lịch nhận/trả phòng và báo cáo của mình | FR-13…16 |
| **Nhân viên tư vấn** | Có (`Staff`) | Tạo đơn thay khách; tra cứu phòng trống toàn hệ thống; quản lý đơn (tìm, đổi trạng thái, giao lại); nhận cảnh báo đơn quá 30 phút chưa xác nhận; xem thống kê đơn của mình | FR-07…12 |
| **Quản trị viên** | Có (`Admin`, gộp vai trò quản lý và kế toán) | Quản lý tài khoản, danh mục, ngày lễ; quản lý đối tác và tỷ lệ hoa hồng; xác nhận cọc, hoàn cọc; báo cáo doanh thu, hoa hồng, đối soát theo kỳ | FR-17…21 |

### 3.2. Trong phạm vi và ngoài phạm vi

| Trong phạm vi | Đơn giản hóa | Ngoài phạm vi |
|---|---|---|
| Lịch phòng tập trung, chống trùng phòng | Mỗi đơn đặt **1 phòng** | Cổng thanh toán trực tuyến |
| Vòng đời đơn 8 trạng thái + lịch sử | Thông báo **trong hệ thống** (chuông) + email (tùy chọn) thay cho Zalo/SMS | Đồng bộ với OTA (Booking, Agoda) |
| Giá theo đêm 3 mức | Cập nhật "gần thời gian thực" bằng AJAX polling 15–30s | Ứng dụng di động cài đặt |
| Cọc 30%, hoàn cọc theo chính sách chung | Chính sách riêng của homestay chỉ **hiển thị** (văn bản), tính hoàn cọc theo chính sách chung | Voucher bồi thường, chuyển tiền thực tế |
| Báo cáo doanh thu, hoa hồng, đối soát theo kỳ | Đối soát là **báo cáo**, admin đánh dấu "đã chuyển tiền" | Môi trường chạy thử riêng (FR-24) |

---

## 4. Đối chiếu yêu cầu của hướng dẫn BTL

| # | Yêu cầu | Hiện thực trong hệ thống | Phụ trách |
|---|---|---|---|
| 3.1 | ≥ 2 lớp người dùng, quyền khác nhau | 4 role: Guest, Owner, Staff, Admin (ASP.NET Core Identity + `IdentityRole`); `[Authorize(Roles=…)]` trên Controller/Area + lọc dữ liệu theo chủ sở hữu | Phú |
| 3.2 | Template + framework front-end | Bootstrap 5, jQuery, Bootstrap Icons, Flatpickr (chọn ngày), Chart.js | Nghĩa |
| 3.3 | **Paging** | Trang tìm kiếm homestay; danh sách đơn, homestay, tài khoản. Lớp tự viết `PaginatedList<T>` (`Skip/Take` + tổng số trang) + partial `_Pager` dùng chung (hoặc gói `X.PagedList.Mvc.Core`) | Nghĩa, Vĩ |
| 3.3 | **Filtering** | Lọc homestay theo khu vực, ngày, giá, số người, tiện nghi; sắp xếp theo giá/điểm; lọc đơn theo trạng thái, ngày, mã, SĐT. Bộ lọc là ViewModel nhận qua query string (`[FromQuery]`), giữ nguyên khi chuyển trang | Nghĩa, Vĩ |
| 3.3 | **AJAX** | jQuery `$.ajax`/`fetch` gọi Web API: kiểm tra phòng trống + báo giá khi chọn ngày; xác nhận/từ chối đơn; đếm thông báo mới; khóa/mở ngày trên lịch. Lọc danh sách đơn không tải lại trang: action trả `PartialView`, jQuery thay nội dung bảng | Nghĩa, Thái |
| 3.3 | **Thêm/sửa/xóa** | Homestay, Phòng, Ảnh, Khu vực, Loại phòng, Tiện nghi, Ngày lễ, Tài khoản, Đơn đặt phòng | Cả nhóm |
| 3.3 | **Session** | `AddSession()` + `UseSession()`; extension `SetObject/GetObject` (JSON) lưu `BookingDraft`: bản nháp đặt phòng qua 3 bước (chọn phòng → thông tin khách → xác nhận); lưu danh sách homestay đã xem gần đây | Phú |
| 3.3 | **Cookies** | `Response.Cookies.Append(...)` với `CookieOptions` (`HttpOnly`, `Expires`, `SameSite`): ghi nhớ bộ lọc tìm kiếm gần nhất; cookie ngôn ngữ của `CookieRequestCultureProvider`; cookie đăng nhập Identity ("Ghi nhớ đăng nhập") | Nghĩa |
| 3.3 | **Validation + Annotation** | `[Required]`, `[StringLength]`, `[Range]`, `[RegularExpression]` cho SĐT VN, `[Display]`, `[DataType]`, `[EmailAddress]`; validation tùy biến kế thừa `ValidationAttribute` + `IClientModelValidator` (`CheckOut > CheckIn`, `CheckIn ≥ hôm nay`); `ModelState.IsValid`; client-side bằng jQuery Validation Unobtrusive (`_ValidationScriptsPartial`) | Cả nhóm |
| 3.3 | **Web API RESTful** | Controller kế thừa `ControllerBase` với `[ApiController]`, `[Route("api/[controller]")]`, nằm chung project MVC; các endpoint ở [mục 8](#8-danh-sách-rest-api); AJAX gọi chính các API này. Có Swagger (`Swashbuckle`) để thử API và chụp hình cho báo cáo | Phú |
| 3.3 | **Upload ảnh** | `List<IFormFile>` + `enctype="multipart/form-data"`; nhiều ảnh cho homestay/phòng; kiểm tra định dạng (jpg/png/webp), ≤ 5 MB (`[RequestSizeLimit]`), đổi tên file bằng GUID, lưu `wwwroot/uploads/` | Thái |
| 3.3 | **Editor cho textarea** | CKEditor 5 hoặc TinyMCE cho mô tả homestay, chính sách hủy; lọc HTML bằng `HtmlSanitizer` trước khi lưu; hiển thị bằng `@Html.Raw` chỉ cho nội dung đã lọc | Thái |
| 3.3 | **Kiểm thử** | Project xUnit cho các service nghiệp vụ (EF Core InMemory/SQLite) + bảng test case thủ công ([mục 13](#13-kế-hoạch-kiểm-thử)) | Vĩ (điều phối) |
| 3.4 | **Responsive** | Bootstrap grid; giao diện chủ homestay ưu tiên điện thoại, khóa ngày ≤ 3 thao tác | Thái, Nghĩa |
| 3.5 | **Bảo mật** | Xem [mục 5.4](#54-bảo-mật) | Phú |
| 3.5 | **Quốc tế hóa** | Tiếng Việt / English: `AddLocalization()` + `AddViewLocalization()` + `AddDataAnnotationsLocalization()`; file `SharedResource.vi.resx` / `SharedResource.en.resx`; `IViewLocalizer`/`IStringLocalizer<SharedResource>` trong View, Controller; `UseRequestLocalization` với `CookieRequestCultureProvider`; định dạng tiền và ngày theo culture | Nghĩa |

---

## 5. Kiến trúc và công nghệ

### 5.1. Công nghệ

| Thành phần | Lựa chọn |
|---|---|
| Framework | **ASP.NET Core MVC** trên **.NET 8 (LTS)**: mô hình Model – View – Controller, Razor Views |
| REST API | Web API của ASP.NET Core (`[ApiController]`), cùng project với MVC |
| ORM / CSDL | **Entity Framework Core 8** (Code First + Migrations) / **SQL Server** |
| Xác thực | **ASP.NET Core Identity** + Roles; `AccountController` tự viết theo MVC (đăng nhập, đăng xuất, đổi mật khẩu) dùng `SignInManager`, `UserManager` |
| Front-end | Razor Views + Tag Helpers, Bootstrap 5, jQuery, jQuery Validation Unobtrusive, Flatpickr, Chart.js |
| Phân trang | `PaginatedList<T>` tự viết (hoặc `X.PagedList.Mvc.Core`) |
| Editor | CKEditor 5 hoặc TinyMCE (self-host trong `wwwroot/lib`), lọc bằng `HtmlSanitizer` |
| Chạy nền | `BackgroundService` + `PeriodicTimer` (mỗi phút): cảnh báo đơn quá 30 phút, tự hủy đơn quá hạn cọc |
| Tài liệu API | Swagger UI (`Swashbuckle.AspNetCore`) |
| Kiểm thử | xUnit + EF Core InMemory/SQLite + Moq |
| Công cụ | Visual Studio 2022 (Windows) hoặc VS Code / JetBrains Rider (macOS), .NET CLI, `dotnet-ef`, Docker (SQL Server trên macOS), GitHub, draw.io |

**Khởi tạo project:**

```bash
dotnet new sln -n HomestayBooking
dotnet new mvc -n HomestayBooking.Web
dotnet new xunit -n HomestayBooking.Tests
dotnet sln add HomestayBooking.Web HomestayBooking.Tests
dotnet add HomestayBooking.Tests reference HomestayBooking.Web
dotnet new gitignore
dotnet tool install --global dotnet-ef
```

**Gói NuGet (project Web):** `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Swashbuckle.AspNetCore`, `HtmlSanitizer` (tùy chọn `X.PagedList.Mvc.Core`).
**Gói NuGet (project Tests):** `Microsoft.EntityFrameworkCore.InMemory` hoặc `Microsoft.EntityFrameworkCore.Sqlite`, `Moq`.

**SQL Server trên macOS** (máy Windows dùng LocalDB hoặc SQL Server Express):

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Fuji@12345" -p 1433:1433 \
  --name sql-homestay -d mcr.microsoft.com/mssql/server:2022-latest
```

### 5.2. Cấu trúc solution

```
HomestayBooking.sln
├── HomestayBooking.Web/                 # ASP.NET Core MVC (.NET 8)
│   ├── Areas/
│   │   ├── Admin/                       # Controllers + Views, [Area("Admin")] [Authorize(Roles="Admin")]
│   │   ├── Owner/                       # Chủ homestay
│   │   └── Staff/                       # Nhân viên tư vấn
│   ├── Controllers/                     # Trang công khai: Home, Homestay, Booking, Lookup, Account, Language
│   ├── Controllers/Api/                 # [ApiController]: HomestaysController, RoomsController, BookingsController, NotificationsController
│   ├── Data/
│   │   ├── ApplicationDbContext.cs      # IdentityDbContext<ApplicationUser>, Fluent API
│   │   ├── Migrations/                  # EF Core Migrations
│   │   └── SeedData.cs                  # role, tài khoản mẫu, 35 homestay mẫu
│   ├── Models/
│   │   ├── Entities/                    # Lớp thực thể
│   │   └── ViewModels/                  # Model cho View + Data Annotations
│   ├── Services/                        # PricingService, BookingService, RefundService, SettlementService, ...
│   ├── BackgroundJobs/                  # ConfirmTimeoutJob, DepositExpiryJob (BackgroundService)
│   ├── Validation/                      # DateAfterAttribute, NotPastDateAttribute (+ JS adapter)
│   ├── Extensions/                      # SessionExtensions, PaginatedList<T>
│   ├── Resources/                       # SharedResource.vi.resx, SharedResource.en.resx
│   ├── Views/Shared/                    # _Layout, _Pager, _SearchFilter, _Notifications, _ValidationScriptsPartial
│   ├── wwwroot/                         # css, js, lib (bootstrap, jquery, ckeditor), uploads/
│   ├── Program.cs                       # đăng ký service + middleware
│   └── appsettings.json                 # connection string, cấu hình upload
├── HomestayBooking.Tests/               # xUnit
└── database/
    ├── HomestayBooking.sql              # script tạo CSDL + dữ liệu mẫu (để nộp)
    └── ERD.drawio
```

**Nguyên tắc:** Controller mỏng. Logic nghiệp vụ (giá, hoàn cọc, chuyển trạng thái, chống trùng) đặt trong `Services/`, đăng ký qua Dependency Injection (`AddScoped`) để viết unit test được.

**Khung `Program.cs`:**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o => {
        o.Password.RequiredLength = 8;
        o.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews(o =>
        o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(o =>
        o.DataAnnotationLocalizerProvider = (t, f) => f.Create(typeof(SharedResource)));
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddSession(o => { o.IdleTimeout = TimeSpan.FromMinutes(30); o.Cookie.HttpOnly = true; });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<RefundService>();
builder.Services.AddScoped<SettlementService>();
builder.Services.AddHostedService<ConfirmTimeoutJob>();
builder.Services.AddHostedService<DepositExpiryJob>();

var app = builder.Build();

if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
else { app.UseSwagger(); app.UseSwaggerUI(); }

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("vi-VN").AddSupportedCultures("vi-VN", "en-US").AddSupportedUICultures("vi-VN", "en-US"));
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();   // Web API (attribute routing)

await SeedData.InitializeAsync(app.Services);
app.Run();
```

### 5.3. Luồng xử lý một yêu cầu

```
Trình duyệt ──(form)──► Controller (MVC)            ──┐
            ──(AJAX)──► API Controller ([ApiController]) ─┤  Middleware: Session → Authentication → Authorization
                                                       │  [Authorize] + Antiforgery + ModelState
                                                       ▼
                                        Service (nghiệp vụ, transaction) ← DI
                                                       ▼
                                        ApplicationDbContext (EF Core) ──► SQL Server
                                                       ▼
                    ViewResult / PartialViewResult (Razor)   hoặc   JSON (ActionResult<T>)
```

### 5.4. Bảo mật

| Nguy cơ | Biện pháp |
|---|---|
| Truy cập trái phép | `[Authorize(Roles=…)]` cho từng Area; Owner chỉ truy vấn dữ liệu có `OwnerId == currentUser`; khách tra cứu đơn phải có **cả mã đơn và SĐT**; `AccessDeniedPath` trả trang 403 |
| CSRF | Filter toàn cục `AutoValidateAntiforgeryTokenAttribute` cho mọi POST của MVC (Tag Helper `<form>` tự chèn token); API thay đổi dữ liệu gắn `[ValidateAntiForgeryToken]`, AJAX gửi token qua header `RequestVerificationToken` |
| SQL Injection | Chỉ dùng LINQ/EF Core (tham số hóa); nếu cần SQL thô dùng `FromSqlInterpolated` |
| XSS | Razor tự encode; chỉ nội dung từ editor được hiển thị bằng `@Html.Raw` và đã lọc bằng `HtmlSanitizer` trước khi lưu |
| Over-posting | ViewModel riêng cho từng form (hoặc `[Bind(...)]`), không bind Entity trực tiếp |
| Mật khẩu | Identity băm mật khẩu (PBKDF2); `PasswordOptions` (độ dài, độ phức tạp); `LockoutOptions`, khóa sau 5 lần sai |
| Upload độc hại | Kiểm tra phần mở rộng + `ContentType` + chữ ký file (magic bytes) + dung lượng; đổi tên GUID; chỉ phục vụ file tĩnh trong `wwwroot/uploads/` |
| Cookie | `HttpOnly`, `SecurePolicy = Always`, `SameSite = Lax` cho cookie đăng nhập và session |
| HTTPS | `UseHttpsRedirection()`, `UseHsts()` |
| Lộ lỗi | `UseExceptionHandler("/Home/Error")` ở môi trường Production, không hiện stack trace; `UseStatusCodePagesWithReExecute` cho 404/403 |
| Bí mật cấu hình | Chuỗi kết nối thật để trong `dotnet user-secrets` hoặc `appsettings.Development.json` (không đưa lên repo) |
| Truy vết | Bảng `BookingHistories` ghi ai đổi trạng thái, lúc nào |

---

## 6. Thiết kế cơ sở dữ liệu (tổng quan)

### 6.1. Danh sách bảng

| Bảng | Cột chính | Ghi chú |
|---|---|---|
| `AspNetUsers` (ApplicationUser) | FullName, PhoneNumber, Email | Bảng ASP.NET Core Identity (kèm `AspNetRoles`, `AspNetUserRoles`…); role: Admin, Staff, Owner |
| `Areas` | Id, Name | Hoàn Kiếm, Ba Đình, Tây Hồ, Sóc Sơn, Ba Vì… |
| `RoomTypes` | Id, Name | Phòng đôi, phòng gia đình, nguyên căn… |
| `Amenities` | Id, Name, Icon | Wifi, bể bơi, BBQ, thú cưng… |
| `Holidays` | Date (PK), Name | Ngày áp giá lễ Tết |
| `Homestays` | Id, OwnerId, AreaId, Name, Address, DescriptionHtml, PolicyHtml, CommissionRate, IsActive | CommissionRate 0.10–0.15 (BR-02) |
| `HomestayAmenities` | HomestayId, AmenityId | Bảng nối N-N |
| `HomestayImages` | Id, HomestayId, Url, IsCover | |
| `Rooms` | Id, HomestayId, RoomTypeId, Name, MaxGuests, PriceWeekday, PriceWeekend, PriceHoliday, IsActive | Giá 3 mức (BR-08) |
| `RoomImages` | Id, RoomId, Url | |
| `Bookings` | Id, **Code (unique)**, RoomId, CustomerName, CustomerPhone, CustomerEmail, CheckIn, CheckOut, Guests, TotalAmount, DepositAmount, Status, Source (Online/Staff), StaffId, Note, CreatedAt, ConfirmedAt, DepositDeadline | |
| `RoomNights` | Id, RoomId, Date, BookingId (null), BlockReason (null) | **Unique (RoomId, Date)**: cốt lõi chống trùng phòng (BR-05) |
| `BookingHistories` | Id, BookingId, FromStatus, ToStatus, ChangedByUserId, ChangedAt, Note | Lịch sử thay đổi |
| `Payments` | Id, BookingId, Type (Deposit/Refund), Amount, BankRef, ConfirmedByUserId, ConfirmedAt | BR-01, BR-10 |
| `Settlements` | Id, HomestayId, PeriodStart, PeriodEnd, TotalDeposit, TotalCommission, PayoutAmount, PaidAt | Đối soát theo kỳ (BR-03) |
| `Reviews` | Id, **BookingId (unique)**, Rating (1–5), Comment, CreatedAt | Mỗi đơn 1 đánh giá (BR-09) |
| `Notifications` | Id, UserId, Message, Url, IsRead, CreatedAt | Thay cho Zalo/SMS |

### 6.2. Cơ chế chống trùng phòng

Mỗi đêm bị chiếm của một phòng là **một dòng** trong `RoomNights`:

- Đơn được **xác nhận** → thêm các dòng (RoomId, Date, BookingId) cho từng đêm từ `CheckIn` đến `CheckOut - 1`.
- Chủ homestay **khóa ngày** → thêm dòng có `BlockReason` (khách kênh khác / bảo trì), `BookingId = null`.
- Đơn **bị hủy / không đến** → xóa các dòng của đơn đó, phòng được mở lại.
- Hai thao tác đồng thời cùng chiếm một đêm → thao tác sau vi phạm **unique index** và bị CSDL từ chối. Service bắt `DbUpdateException` có `InnerException` là `SqlException` với `Number` = 2601/2627 và báo "Phòng vừa được đặt".
- Khai báo trong EF Core: `modelBuilder.Entity<RoomNight>().HasIndex(x => new { x.RoomId, x.Date }).IsUnique();` trong `OnModelCreating` (hoặc attribute `[Index(nameof(RoomId), nameof(Date), IsUnique = true)]` trên lớp `RoomNight`). Kiểu `Date` dùng `DateOnly` (EF Core 8 ánh xạ sang cột `date`).

→ Đảm bảo **0 đơn trùng phòng** ở mức CSDL, không phụ thuộc code ứng dụng (NFR-01).

---

## 7. Quy tắc nghiệp vụ áp dụng trong code

### 7.1. Vòng đời đơn đặt phòng

```
                 ┌──────────► TuChoi (chủ HS từ chối)
                 │
ChoXacNhan ──────┼──► DaXacNhan ──► DaDatCoc ──► DaNhanPhong ──► HoanThanh
                 │        │             │
                 │        ▼             ├──► DaHuy (khách hủy → tính hoàn cọc)
                 └──► DaHuy  (quá hạn cọc)        └──► KhongDen
```

| Chuyển trạng thái | Người thực hiện | Điều kiện / tác động |
|---|---|---|
| (mới) → ChoXacNhan | Khách, Staff | Phòng còn trống tại thời điểm gửi; sinh mã đơn |
| ChoXacNhan → DaXacNhan | Owner | Thêm `RoomNights`; tự **từ chối** các đơn chờ khác trùng ngày; đặt `DepositDeadline = now + 24h` |
| ChoXacNhan → TuChoi | Owner | Thông báo cho khách và nhân viên |
| DaXacNhan → DaDatCoc | Admin | Ghi `Payments` loại Deposit, đúng mã đơn và số tiền (BR-10) |
| DaXacNhan → DaHuy | Hệ thống | Quá `DepositDeadline` chưa có cọc → xóa `RoomNights` |
| DaDatCoc → DaNhanPhong → HoanThanh | Owner / Staff | Đến ngày nhận/trả phòng |
| DaDatCoc → DaHuy | Khách, Staff | Tính hoàn cọc (BR-04), ghi `Payments` loại Refund, xóa `RoomNights` |
| DaDatCoc → KhongDen | Owner / Staff | Không hoàn cọc |

Mọi chuyển trạng thái đều đi qua `BookingService.ChangeStatus(...)`. Hàm này kiểm tra chuyển trạng thái có hợp lệ không, ghi `BookingHistories` và tạo `Notifications`.

### 7.2. Công thức

| Mã | Quy tắc | Hiện thực |
|---|---|---|
| BR-08 | Giá mỗi đêm: ngày lễ (có trong `Holidays`) → `PriceHoliday`; đêm thứ Sáu, thứ Bảy → `PriceWeekend`; còn lại → `PriceWeekday` | `PricingService.CalculateTotal(room, checkIn, checkOut)` |
| BR-01 | Cọc = 30% tổng tiền (làm tròn đến 1.000đ) | `PricingService.CalculateDeposit(total)` |
| BR-02 | Hoa hồng = tổng tiền × `CommissionRate` của homestay | `SettlementService` |
| BR-03 | Số tiền chuyển cho homestay = Σ cọc − Σ hoa hồng của các đơn **HoanThanh** trong kỳ (1–15, 16–cuối tháng) | `SettlementService.Generate(homestayId, period)` |
| BR-04 | Hoàn cọc: hủy trước ngày nhận ≥ 7 ngày → 100%; 3–6 ngày → 50%; < 3 ngày → 0% | `RefundService.CalculateRefund(booking, cancelDate)` |
| BR-06 | Đơn ChoXacNhan quá 30 phút → tạo cảnh báo cho nhân viên | `ConfirmTimeoutJob : BackgroundService` – `PeriodicTimer` 1 phút, tạo scope bằng `IServiceScopeFactory` để dùng `DbContext` |
| BR-07 | Mã đơn: `FJ` + `yyMMdd` + `-` + số thứ tự 3 chữ số trong ngày, ví dụ `FJ261015-003` | `BookingCodeGenerator`, có unique index trên `Code` |
| BR-09 | Chỉ đánh giá khi đơn HoanThanh, mỗi đơn 1 lần | Kiểm tra trạng thái + unique `BookingId` trên `Reviews` |

---

## 8. Danh sách REST API

| Method | Endpoint | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/homestays?area=&checkIn=&checkOut=&guests=&minPrice=&maxPrice=&amenities=&sort=&page=` | Tìm kiếm, lọc, phân trang | Công khai |
| GET | `/api/rooms/{id}/availability?from=&to=` | Danh sách ngày đã kín của phòng (cho lịch) | Công khai |
| GET | `/api/rooms/{id}/quote?checkIn=&checkOut=` | Báo giá: tổng tiền, cọc, giá từng đêm | Công khai |
| POST | `/api/bookings` | Tạo yêu cầu đặt phòng | Công khai / Staff |
| GET | `/api/bookings/{code}?phone=` | Tra cứu đơn | Công khai (cần đúng SĐT) |
| POST | `/api/bookings/{id}/confirm` | Xác nhận đơn | Owner |
| POST | `/api/bookings/{id}/reject` | Từ chối đơn | Owner |
| POST | `/api/bookings/{id}/cancel` | Hủy đơn, trả về số tiền hoàn | Khách (mã + SĐT) / Staff |
| POST | `/api/rooms/{id}/blocks` | Khóa ngày | Owner |
| DELETE | `/api/rooms/{id}/blocks/{date}` | Mở khóa ngày | Owner |
| GET | `/api/notifications/unread-count` | Số thông báo chưa đọc (polling) | Đã đăng nhập |

API viết bằng ASP.NET Core (`[ApiController]`, `[Route("api/[controller]")]`, `[HttpGet]`/`[HttpPost]`/`[HttpDelete]`), trả về JSON qua `ActionResult<T>`: `Ok()`, `CreatedAtAction()`, `NotFound()`, `Conflict()`; lỗi validation được `[ApiController]` tự trả 400 (`ValidationProblemDetails`). Mã trạng thái: 200, 201, 400, 401/403, 404, 409 (trùng phòng). Toàn bộ API hiển thị trên Swagger UI (`/swagger`).

---

## 9. Sitemap

```
Trang chủ
├── Tìm kiếm homestay (lọc + phân trang)
│   └── Chi tiết homestay
│       └── Chi tiết phòng (lịch trống, báo giá)
│           └── Đặt phòng: B1 Chọn ngày → B2 Thông tin khách → B3 Xác nhận → Thành công (mã đơn)
├── Tra cứu đơn (mã + SĐT) → Chi tiết đơn → Hủy đơn / Đánh giá
├── Đăng nhập (Owner, Staff, Admin)
└── Đổi ngôn ngữ VI / EN

Khu vực Chủ homestay (/Owner)
├── Tổng quan (đơn chờ xác nhận, lịch nhận/trả hôm nay)
├── Homestay của tôi → Phòng → Ảnh, Giá
├── Lịch phòng (khóa / mở ngày)
├── Đơn đặt phòng (xác nhận / từ chối / nhận phòng / trả phòng)
└── Báo cáo đối soát

Khu vực Nhân viên (/Staff)
├── Tra cứu phòng trống toàn hệ thống
├── Tạo đơn thay khách
├── Quản lý đơn (lọc, phân trang, lịch sử, giao lại)
├── Cảnh báo đơn quá hạn xác nhận
└── Thống kê đơn của tôi

Khu vực Quản trị (/Admin)
├── Dashboard (biểu đồ đơn, doanh thu, hoa hồng)
├── Tài khoản & phân quyền
├── Danh mục: Khu vực, Loại phòng, Tiện nghi, Ngày lễ
├── Homestay đối tác (tỷ lệ hoa hồng, kích hoạt)
├── Xác nhận cọc / Hoàn cọc
├── Đối soát theo kỳ
└── Báo cáo
```

---

## 10. Kế hoạch triển khai theo giai đoạn

> Mỗi giai đoạn tương ứng 1 sprint (≈ 1 tuần). Điều chỉnh theo lịch học kỳ, đảm bảo xong trước **2 buổi thuyết trình cuối kỳ**.

| GĐ | Tên | Công việc | Đầu ra | Mục báo cáo |
|---|---|---|---|---|
| **0** | Khởi động | Chốt đề tài; cài .NET 8 SDK, Visual Studio 2022 / VS Code + C# Dev Kit / Rider, SQL Server (LocalDB trên Windows, Docker trên macOS), `dotnet-ef`; tạo repo GitHub, bảng Kanban; thống nhất quy ước ([mục 12](#12-quy-ước-làm-việc-nhóm)) | Repo + backlog | — |
| **1** | Phân tích | Tóm tắt file PTTKYC: mô tả hệ thống, nghiệp vụ, biểu mẫu khảo sát; xác định chức năng 4 lớp người dùng; vẽ **use-case** tổng quát + phân rã | Use-case (draw.io) | 5.1, 5.2 |
| **2** | Thiết kế | **ERD**, mô tả bảng và ràng buộc; **sitemap**; **wireframe** các màn hình chính (tìm kiếm, chi tiết, đặt phòng, lịch phòng, quản lý đơn, dashboard) | ERD, sitemap, wireframe | 5.2 |
| **3** | Nền tảng | Tạo solution bằng `dotnet new mvc` + `dotnet new xunit`; cấu hình `Program.cs`; entity, `ApplicationDbContext`, `dotnet ef migrations add Init` / `dotnet ef database update`, seed trong `SeedData` (35 homestay mẫu, tài khoản mỗi role); Identity + 4 role; `_Layout` Bootstrap responsive; cấu hình i18n + cookie ngôn ngữ; partial phân trang dùng chung; CRUD danh mục (Admin) | Chạy được, đăng nhập theo role | 5.3 |
| **4** | Chủ homestay | CRUD homestay, phòng; upload nhiều ảnh; editor mô tả/chính sách; giá 3 mức; trang lịch phòng khóa/mở ngày bằng AJAX | Module Owner | 5.3 |
| **5** | Khách: tìm kiếm | Trang tìm kiếm (filter + paging + sort, lưu bộ lọc vào cookie); chi tiết homestay/phòng; API availability + quote; lịch trống và báo giá AJAX | Module tìm kiếm | 5.3 |
| **6** | Đặt phòng | Đặt phòng 3 bước (session); `BookingService` + `RoomNights`; sinh mã đơn; tra cứu đơn; Owner xác nhận/từ chối; lịch sử đơn; thông báo trong hệ thống | Luồng đặt phòng end-to-end | 5.3 |
| **7** | Nhân viên + Admin | Staff: tạo đơn thay khách, quản lý đơn, giao lại, cảnh báo 30 phút (job); Admin: xác nhận cọc, hủy và hoàn cọc, tự hủy đơn quá hạn cọc (job), đánh giá, đối soát, dashboard Chart.js | Đủ chức năng | 5.3 |
| **8** | Kiểm thử | Unit test xUnit (`dotnet test`); chạy bảng test case; test responsive (điện thoại/máy tính), test bảo mật cơ bản, test i18n; sửa lỗi | Bảng test case + kết quả | 5.4 |
| **9** | Đóng gói | Xuất script `.sql`: schema bằng `dotnet ef migrations script --idempotent -o database/HomestayBooking.sql`, dữ liệu mẫu bằng *Generate Scripts → Schema and data* của SSMS/Azure Data Studio; viết `README` hướng dẫn cài đặt và cấu hình; hoàn thiện báo cáo (kết luận, phân công, TLTK, bài học, hướng phát triển); làm slide, tập thuyết trình | File `.zip` nộp | 5.5, 5.6 |

**Mốc kiểm tra:**

- **Hết GĐ 2:** có đủ hình cho chương 5.2. Không bắt đầu code khi ERD chưa chốt.
- **Hết GĐ 6:** demo được luồng đặt phòng hoàn chỉnh (mốc quan trọng nhất).
- **Hết GĐ 8:** đóng băng tính năng (feature freeze), chỉ sửa lỗi và viết báo cáo.

---

## 11. Phân công công việc

| Thành viên | Vai trò | Phụ trách chức năng | Phụ trách báo cáo |
|---|---|---|---|
| **Phạm Ngọc Phú** | Trưởng nhóm, tích hợp | Nền tảng (solution, Identity, phân quyền, DbContext), đặt phòng + `BookingService` + `RoomNights`, REST API, bảo mật | 5.1, tổng hợp và biên tập báo cáo, slide |
| **Lã Quốc Thái** | Thành viên | Khu vực Chủ homestay: CRUD homestay/phòng, upload ảnh, editor, giá 3 mức, lịch khóa ngày | Use-case, wireframe; 5.3 phần Owner |
| **Nguyễn Quang Nghĩa** | Thành viên | Trang khách: layout, tìm kiếm (filter/paging/sort), chi tiết, AJAX báo giá, tra cứu đơn, i18n, cookie | Sitemap; 5.3 phần Khách |
| **Trần Kỳ Vĩ** | Thành viên, phụ trách kiểm thử | Khu vực Nhân viên + Admin: quản lý đơn, cảnh báo, cọc/hoàn cọc, đánh giá, đối soát, dashboard; background job | ERD + mô tả CSDL; 5.4 kiểm thử; script `.sql` |

Tất cả thành viên: viết unit test cho phần mình làm, review PR của người khác, chụp màn hình chức năng của mình cho báo cáo.

---

## 12. Quy ước làm việc nhóm

**Git:**

```
main      ← chỉ merge bản ổn định (cuối mỗi giai đoạn)
develop   ← nhánh tích hợp
feature/<tên-chức-năng>   ví dụ: feature/owner-room-crud, feature/search-paging
fix/<mô-tả-lỗi>
```

- Không commit thẳng vào `main` hoặc `develop`; mọi thay đổi đi qua Pull Request và cần ≥ 1 người review.
- Commit message: `feat: thêm lọc theo tiện nghi`, `fix: sai giá đêm lễ`, `docs: cập nhật ERD`, `test: thêm test hoàn cọc`.
- Migration EF Core: **chỉ một người chạy `dotnet ef migrations add` tại một thời điểm** (báo trước trên nhóm chat) vì file `ApplicationDbContextModelSnapshot.cs` rất dễ xung đột khi merge.
- Dùng `.gitignore` tạo bởi `dotnet new gitignore`; không đưa `bin/`, `obj/`, `.vs/`, `wwwroot/uploads/` lên repo.
- Chuỗi kết nối riêng của từng máy (LocalDB hoặc Docker) để trong `dotnet user-secrets`; `appsettings.json` trên repo chỉ chứa chuỗi mẫu.

**Code:**

- Đặt tên tiếng Anh cho class, biến, bảng; giao diện hiển thị qua file `.resx`.
- ViewModel riêng cho từng form, không bind trực tiếp Entity vào View.
- Mọi danh sách trên giao diện đều có phân trang.

**Họp:** Planning đầu tuần và demo cuối tuần (online được); cập nhật Kanban hằng ngày.

---

## 13. Kế hoạch kiểm thử

### 13.1. Unit test (xUnit)

| Nhóm | Ca kiểm thử tiêu biểu |
|---|---|
| `PricingService` | 2 đêm ngày thường; đêm thứ Sáu tính giá cuối tuần; đêm trùng ngày lễ tính giá lễ; cọc = 30% làm tròn |
| `RefundService` | Hủy trước 10 ngày → 100%; trước 5 ngày → 50%; trước 1 ngày → 0%; biên đúng 7 ngày và đúng 3 ngày |
| `BookingService` | Chuyển trạng thái hợp lệ / không hợp lệ (HoanThanh → DaHuy phải lỗi); xác nhận đơn thứ hai trùng ngày → lỗi trùng phòng; xác nhận một đơn tự từ chối đơn chờ trùng ngày |
| `BookingCodeGenerator` | Đúng định dạng; số thứ tự tăng dần trong ngày, quay về 001 khi sang ngày mới |
| `SettlementService` | Chỉ tính đơn HoanThanh trong kỳ; áp dụng đúng tỷ lệ hoa hồng của homestay |

### 13.2. Test case thủ công (mẫu cho mục 5.4)

| Mã | Mục tiêu | Dữ liệu vào / ràng buộc | Đầu ra dự kiến | Đầu ra thực tế | Kết quả | Ghi chú sửa lỗi |
|---|---|---|---|---|---|---|
| TC01 | Đặt phòng hợp lệ | Phòng trống 20–22/11, 2 khách, SĐT 0912345678 | Tạo đơn ChoXacNhan, hiện mã `FJ…` | | | |
| TC02 | Chặn ngày trả trước ngày nhận | CheckIn 22/11, CheckOut 20/11 | Báo lỗi validation, không gửi form | | | |
| TC03 | Chặn SĐT sai định dạng | SĐT `12345` | Thông báo "Số điện thoại không hợp lệ" | | | |
| TC04 | Chống trùng phòng | 2 đơn chờ cùng phòng, cùng ngày; Owner xác nhận cả hai | Đơn thứ hai bị từ chối tự động / không xác nhận được | | | |
| TC05 | Phân quyền | Owner A truy cập URL đơn của Owner B | 403 / không tìm thấy | | | |
| TC06 | Tra cứu đơn | Đúng mã, sai SĐT | Không hiển thị thông tin đơn | | | |
| TC07 | Upload ảnh | File `.exe` đổi đuôi `.jpg`, file 10 MB | Từ chối, báo lỗi | | | |
| TC08 | XSS qua editor | Mô tả chứa `<script>alert(1)</script>` | Script bị loại bỏ khi lưu | | | |
| TC09 | Phân trang + lọc | Lọc Tây Hồ, giá ≤ 1 triệu, trang 2 | Đúng kết quả, giữ bộ lọc khi chuyển trang | | | |
| TC10 | Đổi ngôn ngữ | Chọn English, tải lại trang | Giao diện tiếng Anh, cookie được lưu | | | |
| TC11 | Responsive | Màn hình 375px | Không cuộn ngang, menu thu gọn | | | |
| TC12 | Hủy và hoàn cọc | Đơn DaDatCoc, hủy trước 5 ngày | Hoàn 50% cọc, phòng được mở lại | | | |

Danh sách đầy đủ do Vĩ tổng hợp, khoảng 30–40 test case, mỗi module ít nhất 5.

---

## 14. Bố cục báo cáo và sản phẩm nộp

### 14.1. Bố cục báo cáo (15–35 trang)

| Mục | Nội dung | Nguồn |
|---|---|---|
| Bìa | Tên trường, môn học, tên nhóm, lớp, giảng viên, năm | |
| 5.1 Tổng quan đề tài | Mô tả hệ thống, nghiệp vụ; khảo sát, biểu mẫu; phân tích chức năng 4 lớp người dùng | Rút gọn từ file PTTKYC (≈ 5–6 trang) |
| 5.2 Phân tích và thiết kế | Use-case; ERD + mô tả bảng, ràng buộc; sitemap; wireframe | GĐ 1–2 |
| 5.3 Thiết kế hệ thống | Ảnh chụp từng chức năng + giải thích, nêu rõ chỗ dùng paging/AJAX/session/cookie/API/upload/editor/i18n | GĐ 3–7 |
| 5.4 Kiểm thử | Mục tiêu, bảng test case, lỗi phát hiện và cách sửa | GĐ 8 |
| 5.5 | Kết luận, bảng phân công, tài liệu tham khảo | GĐ 9 |
| 5.6 | Kết quả so với mục tiêu, bài học rút ra, hướng phát triển (Zalo/SMS, cổng thanh toán, đồng bộ OTA, app di động) | GĐ 9 |

**Định dạng:** Times New Roman 13, giãn dòng 1.1, lề trên 2 cm, dưới 2 cm, trái 1.5 cm, phải 1.5 cm, gáy 2 cm; **không có header/footer**.

### 14.2. Checklist nộp bài

- [ ] Báo cáo `.docx`/`.pdf` đúng bố cục và định dạng
- [ ] Toàn bộ mã nguồn (đã xóa `bin/`, `obj/`, `.vs/`; chạy được bằng `dotnet restore` + `dotnet run`)
- [ ] `database/HomestayBooking.sql`: script tạo CSDL + dữ liệu mẫu, đã chạy thử trên máy sạch
- [ ] `README.md`: yêu cầu môi trường (.NET 8 SDK, SQL Server), cách chạy script SQL hoặc `dotnet ef database update`, chỗ sửa chuỗi kết nối, tài khoản mẫu cho từng role
- [ ] File nén duy nhất đặt tên theo quy tắc: `QLHomestay_Nhom<số>_<Lớp>.zip`
- [ ] Slide thuyết trình + kịch bản demo (luồng đặt phòng end-to-end + các mục kỹ thuật)

---

## 15. Rủi ro và cách xử lý

| Rủi ro | Mức độ | Cách xử lý |
|---|---|---|
| Phạm vi quá lớn so với thời gian | Cao | Ưu tiên luồng chính (GĐ 3–6); chức năng ở GĐ 7 làm theo thứ tự: cọc → quản lý đơn → cảnh báo → đánh giá → đối soát → dashboard |
| Xung đột migration / merge | Trung bình | Một người tạo migration tại một thời điểm; merge `develop` thường xuyên |
| Thành viên chậm tiến độ | Trung bình | Kanban cập nhật hằng ngày; trưởng nhóm điều chỉnh phân công ở buổi planning |
| Môi trường khác nhau giữa Windows và macOS | Trung bình | .NET 8 chạy được trên cả hai; máy macOS dùng SQL Server qua Docker; thống nhất phiên bản SDK bằng `global.json`; cài đặt ngay ở GĐ 0 |
| Giảng viên yêu cầu đúng ASP.NET MVC 5 | Thấp | Xác nhận với giảng viên ở GĐ 0; ASP.NET Core MVC vẫn là mô hình MVC của ASP.NET nên mọi yêu cầu ở mục 3.3 đều đáp ứng được |
| Mất dữ liệu demo trước buổi thuyết trình | Thấp | Seed data + script `.sql`; tập demo trên máy sẽ dùng để trình bày |
| Báo cáo dồn về cuối | Cao | Mỗi giai đoạn hoàn thành phần báo cáo tương ứng (cột "Mục báo cáo" ở [mục 10](#10-kế-hoạch-triển-khai-theo-giai-đoạn)) |
