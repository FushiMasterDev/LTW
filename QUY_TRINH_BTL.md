# TỔNG QUAN QUY TRÌNH THỰC HIỆN BÀI TẬP LỚN LẬP TRÌNH WEB

**Đề tài:** Hệ thống đặt phòng homestay cho công ty trung gian (Công ty Fuji)
**Công nghệ:** ASP.NET Core MVC (.NET 10) · Entity Framework Core 10 · ASP.NET Core Identity · SQL Server · Bootstrap 5 · jQuery
**Nhóm:** Phạm Ngọc Phú (trưởng nhóm) · Lã Quốc Thái · Nguyễn Quang Nghĩa · Trần Kỳ Vĩ
**Đầu vào:** Báo cáo *Phân tích thiết kế yêu cầu* (`phantichyeucaubanchinh.docx`): khảo sát, quy tắc nghiệp vụ BR-01…BR-13, yêu cầu FR-01…FR-27

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
- Chạy được trọn luồng: khách đặt phòng/giữ đêm → nộp cọc (QR tự khớp hoặc kế toán duyệt bill) → chủ homestay xác nhận → nhận phòng → hoàn thành → khách đánh giá.
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

| Nhóm | Định danh/quyền | Phạm vi |
|---|---|---|
| Khách | Guest hoặc role Customer | Tìm/đặt/tra cứu đơn; khách đăng nhập gửi yêu cầu đổi; vẫn tự hủy ngay |
| Chủ homestay | Role Owner; quyền quản lý kiểm hồ sơ đã Approved | Chỉ dữ liệu homestay của mình; xác nhận đơn, quản lý cơ sở/lịch, xem đối soát |
| Tư vấn | Role Employee, Position Consultant | Tạo đơn/gửi yêu cầu đổi hoặc hủy thay khách, giao lại và xử lý đơn |
| Kế toán | Role Employee, Position Accountant | Bill/giao dịch/hoàn tiền, cọc bổ sung, đối soát |
| Quản lý | Role Employee, Position Manager | Duyệt nghiệp vụ đổi/hủy, đối tác/hợp đồng, voucher, báo cáo |
| Quản trị | Role Employee, Position Administrator | Tài khoản và danh mục; không có quyền Manager/Accountant ở hai policy đổi/hủy chuyên biệt |

Không dùng role Guest/Staff/Admin để thay cho ba role Customer/Owner/Employee trong code. Chức vụ và trạng thái làm việc của Employee quyết định quyền chi tiết. Policy ChangeRequestApprove chỉ Manager, ChangeRequestFinance chỉ Accountant.

### 3.2. Phạm vi đã chốt

- Đặt theo **đêm**, mỗi đơn một phòng, khoảng `[CheckIn, CheckOut)`; giữ phòng ngay lúc đặt, 15 phút để nộp tiền.
- Chủ xác nhận sau khi có cọc, hạn 30 phút; QR tự khớp 24/7, bill là dự phòng. Quá hạn chủ xác nhận chỉ cảnh báo nhân viên.
- Chính sách hủy riêng được ưu tiên; có voucher, giao dịch ngân hàng giả lập và luồng cọc bổ sung.
- Thông báo trong ứng dụng đã có; tích hợp gửi Zalo/SMS cần cấu hình và backend riêng. Không có cổng thanh toán hoặc đồng bộ OTA trong phạm vi hiện tại.
- Đối soát hai kỳ theo CheckOut; chủ xác nhận số liệu tùy chọn, kế toán đánh dấu chuyển tiền. Demo chỉ seed tài khoản/dữ liệu mẫu ở Development.

### 3.3. Đối chiếu đủ FR-01…FR-27

Bảng sau truy vết đề bài gốc; kế hoạch backend phải đối chiếu cùng các quyết định mới ở mục 7 và tài liệu database, không coi toàn bộ FR là đã triển khai xong.

| Mã | Yêu cầu đề bài | Tác nhân |
|---|---|---|
| FR-01 | Tìm kiếm homestay theo khu vực, ngày nhận/trả, khoảng giá, loại phòng, số người, tiện nghi; sắp xếp theo giá và điểm đánh giá | Khách hàng |
| FR-02 | Xem chi tiết homestay/phòng: hình ảnh, tiện nghi, giá theo ngày, chính sách hủy, đánh giá, lịch phòng trống; hiển thị tổng tiền cuối cùng và số tiền cọc trước khi gửi yêu cầu (BR-01, BR-08) | Khách hàng |
| FR-03 | Gửi yêu cầu đặt phòng trực tuyến bằng số điện thoại, không bắt buộc tạo tài khoản | Khách hàng |
| FR-04 | Tra cứu đơn bằng mã đơn + SĐT; xem trạng thái đơn và trạng thái cọc; nhận thông báo qua Zalo/SMS | Khách hàng |
| FR-05 | Gửi yêu cầu đổi/hủy đơn, xem trước số tiền được hoàn | Khách hàng |
| FR-06 | Đánh giá homestay sau khi trả phòng (BR-09); chủ homestay được trả lời đánh giá | Khách hàng, Chủ homestay |
| FR-07 | Tạo đơn thay khách (đơn nhận qua điện thoại/Zalo) | Nhân viên |
| FR-08 | Tra cứu phòng trống của tất cả homestay, cập nhật gần thời gian thực (≤ 5 giây – NFR-02) | Nhân viên |
| FR-09 | Quản lý đơn: tìm theo mã/tên/SĐT, cập nhật trạng thái, xem lịch sử, giao đơn cho nhân viên khác | Nhân viên |
| FR-10 | Xử lý đổi/hủy đơn; đề xuất phòng/homestay thay thế khi trùng phòng (BR-11) | Nhân viên |
| FR-11 | Nhận cảnh báo khi homestay quá 30 phút chưa xác nhận đơn | Nhân viên |
| FR-12 | Quản lý thông tin khách hàng | Nhân viên |
| FR-13 | Quản lý thông tin homestay, phòng, hình ảnh, tiện nghi, giá 3 mức, chính sách hủy riêng | Chủ homestay |
| FR-14 | Cập nhật lịch phòng (trống/đã đặt/bảo trì); khóa ngày cho khách từ kênh khác | Chủ homestay |
| FR-15 | Nhận thông báo; xác nhận/từ chối yêu cầu đặt, đổi, hủy | Chủ homestay |
| FR-16 | Xem danh sách đơn, lịch nhận – trả phòng, bảng đối soát của mình | Chủ homestay |
| FR-17 | Ghi nhận và xác nhận tiền cọc, hoàn cọc gắn với từng đơn | Kế toán |
| FR-18 | Tự động lập bảng đối soát theo kỳ (BR-03), xác nhận đã chuyển tiền | Kế toán |
| FR-19 | Quản lý homestay đối tác, hợp đồng, tỷ lệ hoa hồng | Quản lý |
| FR-20 | Báo cáo số đơn, doanh thu, hoa hồng, tỷ lệ hủy, hiệu suất nhân viên, điểm đánh giá homestay (nhân viên chỉ xem thống kê đơn của mình – X6) | Quản lý |
| FR-21 | Quản lý tài khoản, phân quyền, danh mục (khu vực, loại phòng, tiện nghi) | Quản trị viên |
| FR-22 | Tự động khóa lịch phòng khi homestay xác nhận đơn; tự động mở lại khi đơn đã xác nhận bị hủy (BR-05). Quy tắc giữ chỗ tạm thời cho các yêu cầu đang chờ xác nhận chưa được chốt (mục 1.10.6) | Hệ thống |
| FR-23 | Tự động gửi khách lời nhắc trước ngày nhận phòng kèm định vị bản đồ, hướng dẫn nhận phòng, số điện thoại homestay; nhắc chủ homestay lịch đón khách (KS khách câu 6: 49,6% và 28,3%); thông tin liên hệ của homestay chỉ gửi sau khi đơn được xác nhận (BR-13) | Hệ thống |
| FR-24 | Cung cấp môi trường chạy thử với dữ liệu mẫu để hướng dẫn nhân viên, chủ homestay và kiểm thử trước khi vận hành | Quản trị viên |
| FR-25 | Gửi đăng ký trở thành đối tác trực tuyến (thông tin cơ sở, số phòng, khu vực, liên hệ, ảnh giấy phép kinh doanh lưu trú, PCCC, ảnh thực tế); theo dõi trạng thái hồ sơ | Chủ homestay chưa hợp tác |
| FR-26 | Thẩm định hồ sơ đăng ký, cập nhật trạng thái hồ sơ (chờ thẩm định / cần bổ sung / từ chối / đã ký hợp đồng), kích hoạt tài khoản homestay (BR-12) | Nhân viên, Quản lý |
| FR-27 | Duyệt nội dung homestay (ảnh, mô tả, giá) trước khi hiển thị cho khách; cảnh báo hồ sơ quá 3 ngày làm việc chưa xử lý (BR-12) | Nhân viên, Hệ thống |

---

## 4. Đối chiếu yêu cầu của hướng dẫn BTL

| # | Yêu cầu | Hiện thực trong hệ thống | Phụ trách |
|---|---|---|---|
| 3.1 | ≥ 2 lớp người dùng, quyền khác nhau | Role Customer/Owner/Employee; Employee.Position Consultant/Accountant/Manager/Administrator, policy và lọc sở hữu ở service/controller | Phú |
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
| Framework | **ASP.NET Core MVC** trên **.NET 10**: mô hình Model – View – Controller, Razor Views |
| REST API | Web API của ASP.NET Core (`[ApiController]`), cùng project với MVC |
| ORM / CSDL | **Entity Framework Core 10** (Code First + Migrations) / **SQL Server** |
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
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<mat-khau-manh>" -p 1433:1433 \
  --name sql-homestay -d mcr.microsoft.com/mssql/server:2022-latest
```

### 5.2. Cấu trúc solution

```
HomestayBooking.sln
├── HomestayBooking.Web/                 # ASP.NET Core MVC (.NET 10)
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
| Truy vết | `BookingStatusHistories` ghi người/thời điểm đổi trạng thái; `AuditLogs` ghi thay đổi dữ liệu nghiệp vụ |

---

## 6. Thiết kế cơ sở dữ liệu (tổng quan)

Nguồn chuẩn: `ApplicationDbContext`, entity và migrations; chi tiết cột ở `fuji_homestay_database_v1.md`. Mô hình code/database thử có **9 migration, 45 bảng gồm __EFMigrationsHistory, 382 cột, 76 FK, 60 CHECK, 93 index ngoài PK** sau EnumAndReviewChecks. **v5 mới áp ChangeRequestWorkflow: 8 migration/44 CHECK; chưa áp migration thứ 9.**

### 6.1. Danh sách bảng hiện tại

| Nhóm | Bảng | Vai trò/ràng buộc chính |
|---|---|---|
| Identity (7) | AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims | Một Account/Person, nhiều vai trò; role có người/thời điểm cấp |
| Hồ sơ (7) | PersonProfiles, Customers, CustomerProfiles, Owners, OwnerRegistrations, Employees, AccountActivityLogs | SĐT unique, Customer không bắt buộc Account; một hồ sơ Owner mở; mã nhân viên unique |
| Pháp lý (1) | VerificationDocuments | Đúng một chủ sở hữu; pháp lý mới gắn từng Homestay, giữ lịch sử cũ |
| Đối tác/danh mục (6) | Areas, Homestays, Contracts, Amenities, HomestayAmenities, HomestayImages | Homestay.Status; hợp đồng có hoa hồng/ngày hiệu lực, một Active; một ảnh bìa |
| Phòng/giá/chính sách (7) | RoomCategories, RoomTypes, Rooms, RoomTypePrices, Holidays, CancellationPolicies, CancellationRules | Giá theo RoomType, DayType, EffectiveFrom; chính sách riêng ưu tiên chung |
| Lịch (2) | RoomBlocks, RoomNights | Khoảng khóa; PK (RoomId, Date), đúng một chủ giữ đêm |
| Đơn (5) | Bookings, BookingNights, BookingCodeCounters, BookingStatusHistories, BookingChangeRequests | Code unique; snapshot giá từng đêm; yêu cầu đổi/hủy hai bước |
| Tiền/voucher (3) | Payments, BankTransactions, Vouchers | Cọc đầu/cọc bổ sung/hoàn/bù; webhook idempotent, voucher có số dư |
| Đối soát (2) | Settlements, SettlementItems | UNIQUE kỳ/homestay; mỗi Booking một Item; snapshot tiền và phí đổi |
| Đánh giá/thông báo/audit (4) | Reviews, Notifications, MessageLogs, AuditLogs | Một đánh giá/đơn; log gửi ngoài và log nghiệp vụ riêng |
| Metadata (1) | __EFMigrationsHistory | Lịch sử migration; không phải dữ liệu nghiệp vụ |

EnumAndReviewChecks chỉ thêm 13 whitelist và 3 CHECK Reply/Year/Decision, không thêm bảng/cột/index. OwnerRegistration NeedsRevision/Approved/Rejected phải có người và thời điểm duyệt; NeedsRevision/Rejected phải có lý do. Năm đối soát 2020–2100; OwnerReply/RepliedAt cùng có hoặc cùng không có.

### 6.2. Cơ chế chống trùng phòng theo đêm

- Từ lúc tạo AwaitingPayment: trong một transaction chọn phòng đang bán, thêm RoomNights cho từng đêm `[CheckIn, CheckOut)`; PK `(RoomId, Date)` chặn đặt đồng thời.
- Mỗi RoomNight thuộc đúng một Booking, RoomBlock hoặc BookingChangeRequest. Khóa ngày và giữ phòng mới khi đổi dùng chung PK này.
- Đổi lịch dùng lại đêm giao nhau; chuyển đêm giữ của yêu cầu về Booking khi Applied, nhả phần cũ không dùng trong cùng transaction.
- Expired/Cancelled/Rejected/NoShow nhả đêm của đơn; Completed giữ đêm lịch sử theo code hiện tại. Không lấy số dòng lịch quá khứ làm số phòng đang bán.
- CheckIn 16/10, CheckOut 18/10 nghĩa là hai đêm 16 và 17; đơn khác được nhận ngày 18. Ngày trả phòng không tính thêm giá hoặc chiếm lịch.
- Service kiểm đủ số đêm, đúng Room/RoomType/Homestay và tổng BookingNights.Price bằng TotalAmount. CHECK/FK riêng lẻ không thay thế kiểm tra liên bảng này.

---

## 7. Quy tắc nghiệp vụ áp dụng trong code

### 7.1. Vòng đời đơn và yêu cầu đổi/hủy

```text
AwaitingPayment → PaymentSubmitted → Deposited → Confirmed → CheckedIn → Completed
       └──────── QR tự khớp ────────────┘
```

- AwaitingPayment giữ phòng 15 phút; gửi bill đúng hạn thì giữ trong lúc chờ kế toán. Cọc QR đầu tự khớp hoặc kế toán duyệt bill rồi mới Deposited.
- Chủ xác nhận Deposited → Confirmed trong 30 phút; không có chuyển Confirmed → Deposited chung cho mọi caller.
- Khách vẫn tự hủy ngay bằng BookingsController.Cancel. Nhân viên được gửi Cancel request thay khách; khách gửi Cancel request bị chặn.
- Yêu cầu đổi: Pending → Manager duyệt → Applied khi không cần tiền thêm; có thêm tiền thì AwaitingPayment → nộp QR/bill CR{id} → Accountant xác nhận → Applied. Có Rejected và Expired.
- Hạn PaymentDeadlineAt là hạn khách nộp, không phải hạn kế toán; đã nộp hợp lệ đúng hạn thì được duyệt sau hạn. Manager/Accountant không được là người gửi; Accountant không được là Manager đã duyệt.
- Applied sửa đơn tại chỗ, giữ mã, thay giá/đêm phòng trong một transaction. Đơn Confirmed về Deposited, ConfirmedAt NULL và chủ xác nhận lại. Phí đổi cộng dồn vào ChangeFeeAmount.
- Booking, Request.Status là concurrency token. Chỉ một yêu cầu Pending/AwaitingPayment mỗi Booking; một khoản AdditionalDeposit chưa bị từ chối mỗi Request.
- Ba Pending cũ thiếu người gửi/tiền chỉ được Manager từ chối có lý do thật, không duyệt hoặc tự bù dữ liệu.

### 7.2. Đối chiếu BR-01…BR-13

| Mã | Quy tắc đang áp dụng | Database/backend |
|---|---|---|
| BR-01 | Cọc mặc định 30%, làm tròn nghìn; phần còn lại trả homestay | PricingService; DepositRate snapshot và CHECK (0,1] |
| BR-02 | Hoa hồng theo hợp đồng, snapshot lúc đặt | Contracts.CommissionRate → Bookings → SettlementItems, CHECK (0,1) |
| BR-03 | Hai kỳ ngày 1–15 / 16–cuối tháng, xếp theo CheckOut | SettlementCalculator; cọc giữ lại trừ hoa hồng, cộng phí đổi theo tỷ lệ trên phần phí |
| BR-04 | >=7 ngày hoàn 100%, >=3 ngày hoàn 50%, dưới 3 ngày 0%; ưu tiên chính sách riêng | RefundService.QuoteCancellationAsync; CancellationPolicyId lưu trên đơn |
| BR-05 | Không trùng phòng/đêm, kể cả khóa ngày và chờ cọc | PK RoomNights; service ghi mọi đêm trong transaction |
| BR-06 | Chủ xác nhận trong 30 phút từ khi có cọc; quá hạn cảnh báo, không tự hủy | ConfirmDeadlineAt/ConfirmAlertSentAt, BookingTimeoutJob |
| BR-07 | FJ + yyMMdd + thứ tự duy nhất | BookingCodeGenerator.NextAsync, counter và UNIQUE Code |
| BR-08 | Giá từng đêm: lễ ưu tiên, cuối tuần thứ Sáu/thứ Bảy, còn lại ngày thường | PricingService.QuoteAsync; phiên bản EffectiveFrom gần nhất <= đêm đó, BookingNights snapshot |
| BR-09 | Đơn Completed mới được đánh giá một lần; chủ trả lời | UNIQUE Review.BookingId, CHECK 1–5 và Reply; service kiểm người/đơn |
| BR-10 | Mã đơn + đúng số tiền; QR tự đối chiếu 24/7, bill dự phòng | BankMatchingService, ReceiptReviewService; khoản bổ sung có CR{id}, không coi là cọc đầu |
| BR-11 | Fuji xử lý thay thế/bù; không tìm được thì hoàn cọc và voucher | ReplacedByBookingId, Payment.Compensation/Refund, Vouchers; backend sự cố cần hoàn thiện |
| BR-12 | Duyệt chủ và từng Homestay riêng; giấy phép/PCCC gắn cơ sở, có hợp đồng và duyệt nội dung | OwnerRegistration, VerificationDocuments, Homestay.Status, Contracts; SLA 3 ngày làm việc từ SubmittedAt đủ hồ sơ |
| BR-13 | Ẩn liên hệ/địa chỉ chính xác trước Confirmed | Backend lọc dữ liệu trả về; nhắc trước nhận phòng chỉ gửi đúng điều kiện |

### 7.3. Các quyết định bổ sung ngày 16/10/2026

Áp dụng D15–D24 ở mục 1 tài liệu database: chủ xác nhận đối soát tùy chọn (Sent hoặc Confirmed → Paid), actor ghi AuditLog; không thêm cột actor/liên kết hồ sơ hoặc mốc đủ hồ sơ. SubmittedAt là mốc đủ hồ sơ. Giá/phòng/ảnh/mô tả sửa trực tiếp có audit; địa chỉ/pháp lý đưa cơ sở về PendingReview. Hợp đồng phải Active và EndDate >= CheckOut, chưa có hợp đồng tương lai. Tắt phòng chỉ chặn bán mới. Doanh số theo CreatedByEmployeeId, xử lý theo AssignedEmployeeId. Chủ từ chối lịch mới sau đổi không tự hoàn phí; Manager có thể cấp voucher.

AuditLog ghi trực tiếp trong service và transaction nghiệp vụ; không làm interceptor. Đây là yêu cầu cho backend tiếp theo, không khẳng định mọi thao tác đã có API/service. Không thêm CHECK Settlements.ConfirmedAt; Paid không bắt buộc chủ đã xác nhận.

### 7.4. Backend tiếp nhận đối tác và hợp đồng (B1)

Đã có service và test, chưa cung cấp giao diện/API tiếp nhận đối tác mới:

Sau review B1: có `ResendInviteAsync` cho WaitingOwnerActivation + Account PendingActivation chưa có mật khẩu; đổi security stamp để vô hiệu token cũ, không ghi token vào audit. Nhân viên chỉ sửa thông tin/ngân hàng và giấy tờ của hồ sơ EmployeeAssisted khi chủ chưa kích hoạt (chưa Account hoặc PendingActivation); sau kích hoạt chỉ chủ được sửa. Thông báo chủ dùng tiếng Việt, không chứa ReviewNote, tối đa 500 ký tự. Audit Insert của OwnerRegistrations/Homestays/Contracts/VerificationDocuments dùng Id thật, lưu entity rồi audit trong cùng transaction mặc định; unique index vẫn chặn trùng.

- `OwnerRegistrationService`: chủ tự đăng ký tạo Person/Owner/Account role Owner + Draft + Homestay Draft; không tự ghép SĐT có chủ/tài khoản khác. Nhân viên tạo thay không có mật khẩu; gửi mời sang WaitingOwnerActivation, chủ tự đặt mật khẩu bằng token mời + SĐT rồi trở về Draft. Chưa có OTP, cần nâng cấp SMS sau.
- Nộp mới đủ họ tên/SĐT/ngân hàng và ít nhất một cơ sở có giấy phép kinh doanh, PCCC, ảnh thực tế còn hạn → PendingReview; chỉ cơ sở đủ chuyển PendingReview. SubmittedAt ghi lúc nộp đủ, nộp lại sau NeedsRevision tính lại SLA. Chủ tự bấm nộp, nhân viên không nộp thay.
- PendingReview → UnderReview do Consultant/Manager nhận; UnderReview → NeedsRevision (Consultant/Manager, có ghi chú), Approved/Rejected (Manager/Administrator, khác người tạo thay; từ chối có lý do). Approve chỉ duyệt chủ, cập nhật security stamp, không tự Active homestay. Reject đưa cơ sở PendingReview của chủ về Draft.
- Giấy tờ mới gắn HomestayId; sửa/xóa khi cơ sở Draft, hoặc PendingReview khi hồ sơ NeedsRevision; Active/Suspended/Ended luôn bị chặn. ReceiptFileStore lưu riêng tư ngoài wwwroot, kiểm chữ ký ảnh và 5 MB. Giấy tờ cũ gắn hồ sơ và homestay Active không bị chuyển liên kết hay bắt bổ sung PropertyPhoto (B1.H). Quy tắc đủ giấy tờ không chặn đặt phòng/ký/gia hạn của cơ sở cũ.
- `ContractService`: Manager/Administrator tạo cho chủ Approved, homestay chưa Ended; hoa hồng 10–15%, StartDate không tương lai, EndDate > hôm nay, tối đa một Active. Gia hạn = tăng EndDate hợp đồng Active, giữ tỷ lệ. Đổi tỷ lệ = chấm dứt + tạo mới; đơn cũ giữ snapshot. Chấm dứt cần lý do, chặn đơn thuộc HoldsRoom trừ Completed và trả mã đơn. Completed vẫn giữ đêm lịch sử nhưng không chặn chấm dứt (đã chốt sau review B1).
- `ContractEligibility` dùng một biểu thức Active + StartDate <= hôm nay + EndDate >= CheckOut trong đặt phòng, báo giá/gửi/duyệt đổi, lọc tìm kiếm và xem trước voucher. Không có ngày trả thì lọc đến hôm nay.
- `BookingTimeoutJob` gọi hết hạn hợp đồng (EndDate < ngày VN), audit System và báo Manager. SLA hồ sơ = ba ngày làm việc sau SubmittedAt, giữ giờ VN, bỏ thứ Bảy/CN và Holidays; chỉ cảnh báo PendingReview/UnderReview, không NeedsRevision. Chống lặp bằng Notification.Url + CreatedAt sau SubmittedAt; nộp lại được cảnh báo mới.
- Audit trực tiếp trong service/transaction, JSON cũ/mới, AccountId thật; job AccountId NULL và ghi System. Không thêm schema hay interceptor.
- `database/tests/data_consistency.sql` đối chiếu 14 mục liên bảng bằng SELECT, in OK/LỆCH và tổng; có LỆCH thì `sqlcmd -b` thất bại, dừng báo lại. Không tự sửa dữ liệu.

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
| **0** | Khởi động | Chốt đề tài; cài .NET 10 SDK, Visual Studio 2022 / VS Code + C# Dev Kit / Rider, SQL Server (LocalDB trên Windows, Docker trên macOS), `dotnet-ef`; tạo repo GitHub, bảng Kanban; thống nhất quy ước ([mục 12](#12-quy-ước-làm-việc-nhóm)) | Repo + backlog | — |
| **1** | Phân tích | Tóm tắt file PTTKYC: mô tả hệ thống, nghiệp vụ, biểu mẫu khảo sát; xác định chức năng 4 lớp người dùng; vẽ **use-case** tổng quát + phân rã | Use-case (draw.io) | 5.1, 5.2 |
| **2** | Thiết kế | **ERD**, mô tả bảng và ràng buộc; **sitemap**; **wireframe** các màn hình chính (tìm kiếm, chi tiết, đặt phòng, lịch phòng, quản lý đơn, dashboard) | ERD, sitemap, wireframe | 5.2 |
| **3** | Nền tảng | Tạo solution bằng `dotnet new mvc` + `dotnet new xunit`; cấu hình `Program.cs`; entity, `ApplicationDbContext`, `dotnet ef migrations add Init` / `dotnet ef database update`, seed trong `SeedData` (35 homestay mẫu, tài khoản mỗi role); Identity + Customer/Owner/Employee và chức vụ; `_Layout` Bootstrap responsive; cấu hình i18n + cookie ngôn ngữ; partial phân trang dùng chung; CRUD danh mục (Admin) | Chạy được, đăng nhập theo role | 5.3 |
| **4** | Chủ homestay | CRUD homestay, phòng; upload nhiều ảnh; editor mô tả/chính sách; giá 3 mức; trang lịch phòng khóa/mở ngày bằng AJAX | Module Owner | 5.3 |
| **5** | Khách: tìm kiếm | Trang tìm kiếm (filter + paging + sort, lưu bộ lọc vào cookie); chi tiết homestay/phòng; API availability + quote; lịch trống và báo giá AJAX | Module tìm kiếm | 5.3 |
| **6** | Đặt phòng | Đặt phòng 3 bước (session); `BookingService` + `RoomNights`; sinh mã đơn; tra cứu đơn; Owner xác nhận/từ chối; lịch sử đơn; thông báo trong hệ thống | Luồng đặt phòng end-to-end | 5.3 |
| **7** | Nhân viên + Admin | Staff: tạo đơn thay khách, quản lý đơn, giao lại, cảnh báo 30 phút (job); Accounting: bill/hoàn tiền/đối soát; Manager: duyệt nghiệp vụ; job hết hạn, đánh giá, dashboard | Đủ chức năng | 5.3 |
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
| `BookingService` | Completed → Cancelled phải lỗi; hai người cùng đặt phòng cuối thì chỉ một đơn giữ được đêm; chủ xác nhận đơn đã cọc, không tự từ chối các đơn khác |
| `BookingCodeGenerator` | Đúng định dạng; số thứ tự tăng dần trong ngày, quay về 001 khi sang ngày mới |
| `SettlementCalculator` / backend đối soát | Đơn kết thúc có tiền giữ lại/phí đổi vào kỳ theo CheckOut; hoa hồng snapshot, phí đổi chia theo tỷ lệ trên phí; chủ xác nhận tùy chọn |

### 13.2. Test case thủ công (mẫu cho mục 5.4)

| Mã | Mục tiêu | Dữ liệu vào / ràng buộc | Đầu ra dự kiến | Đầu ra thực tế | Kết quả | Ghi chú sửa lỗi |
|---|---|---|---|---|---|---|
| TC01 | Đặt phòng hợp lệ | Phòng trống 20–22/11, 2 khách, SĐT hợp lệ | Tạo AwaitingPayment, giữ hai đêm và hiện mã `FJ…`/QR cọc | | | |
| TC02 | Chặn ngày trả trước ngày nhận | CheckIn 22/11, CheckOut 20/11 | Báo lỗi validation, không gửi form | | | |
| TC03 | Chặn SĐT sai định dạng | SĐT `12345` | Thông báo "Số điện thoại không hợp lệ" | | | |
| TC04 | Chống trùng phòng | Hai khách cùng đặt phòng cuối, cùng khoảng đêm | Một đơn giữ phòng thành công; người còn lại được báo hết phòng | | | |
| TC05 | Phân quyền | Owner A truy cập URL đơn của Owner B | 403 / không tìm thấy | | | |
| TC06 | Tra cứu đơn | Đúng mã, sai SĐT | Không hiển thị thông tin đơn | | | |
| TC07 | Upload ảnh | File `.exe` đổi đuôi `.jpg`, file 10 MB | Từ chối, báo lỗi | | | |
| TC08 | XSS qua editor | Mô tả chứa `<script>alert(1)</script>` | Script bị loại bỏ khi lưu | | | |
| TC09 | Phân trang + lọc | Lọc Tây Hồ, giá ≤ 1 triệu, trang 2 | Đúng kết quả, giữ bộ lọc khi chuyển trang | | | |
| TC10 | Đổi ngôn ngữ | Chọn English, tải lại trang | Giao diện tiếng Anh, cookie được lưu | | | |
| TC11 | Responsive | Màn hình 375px | Không cuộn ngang, menu thu gọn | | | |
| TC12 | Hủy và hoàn cọc | Đơn Deposited, hủy trước 5 ngày, chính sách chung | Chốt hoàn 50% tiền cọc thực chuyển, nhả phòng; kế toán ghi chuyển hoàn riêng | | | |

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
- [ ] `README.md`: yêu cầu môi trường (.NET 10 SDK, SQL Server), cách chạy script SQL hoặc `dotnet ef database update`, chỗ sửa chuỗi kết nối, tài khoản mẫu cho từng role
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
