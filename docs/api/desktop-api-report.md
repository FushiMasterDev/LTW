# Báo cáo API desktop nhân viên — 21/10/2026

Nhánh làm việc: `feature/desktop-api`, mã nguồn gốc `e5f2aed`. Không đổi nhánh, push hoặc merge. Không sửa/thêm file trong `docs/bao-cao/`.

## Phạm vi đã triển khai

43 endpoint REST riêng `/api/desktop`, DTO riêng, JSON camelCase, enum dạng chữ trong DTO desktop, ngày `yyyy-MM-dd`, timestamp UTC có hậu tố Z. Danh sách có `{items,page,totalPages,totalCount}`, 10 mục/trang qua `PaginatedList`; riêng lựa chọn loại phòng theo homestay trả mảng không phân trang, mảng con trong tài liệu chi tiết/báo cáo tổng hợp không phân trang độc lập.

| Nhóm | Endpoint (method + đường dẫn sau `/api/desktop`) | Số |
|---|---|---:|
| Auth | POST `/auth/login`, GET `/auth/me` | 2 |
| Khu vực | GET/POST `/areas`; GET/PUT/DELETE `/areas/{id}` | 5 |
| Tiện nghi | GET/POST `/amenities`; GET/PUT/DELETE `/amenities/{id}` | 5 |
| Loại phòng | GET/POST `/room-categories`; GET/PUT/DELETE `/room-categories/{id}` | 5 |
| Ngày lễ | GET/POST `/holidays`; GET/PUT/DELETE `/holidays/{yyyy-MM-dd}` | 5 |
| Đơn | GET/POST `/bookings`, GET `/bookings/{id}`, POST `/bookings/{id}/assign`, POST `/bookings/{id}/status` | 5 |
| Lựa chọn / tồn phòng | GET `/assignees`, `/booking-homestays`, `/booking-homestays/{homestayId:int}/room-types`, `/availability` | 4 |
| Khách | GET `/customers`, GET/PUT `/customers/{id}` | 3 |
| Bill | GET `/receipts`, GET `/receipts/{paymentId}/file`, POST `/receipts/{paymentId}/approve`, POST `/receipts/{paymentId}/reject` | 4 |
| Báo cáo | GET `/reports/{management,bookings,financial,employees,homestays}` | 5 |

Đơn/khách/phòng trống dùng đúng StaffBookingService, CustomerService, AvailabilityService. Bill dùng AccountingService/ReceiptReviewService/ReceiptFileStore. Báo cáo gọi từng phương thức ReportService. Bổ sung method đọc RoomTypesAsync trong StaffBookingService ngày 22/10/2026 như mục xử lý bên dưới; giữ nguyên nghiệp vụ các method cũ. Đặt hộ nhận submissionId GUID bắt buộc khác rỗng, chuyển nguyên cho cơ chế chống gửi lặp hiện có.

## Xác thực, quyền và lỗi

- Thêm `Microsoft.AspNetCore.Authentication.JwtBearer` **10.0.12**, khớp Identity/EF ASP.NET Core trong repo. Cookie Identity vẫn là scheme mặc định.
- Base desktop có ApiController, Bearer-only Authorize, IgnoreAntiforgeryToken và exception filter riêng. Cookie hợp lệ không có Bearer vẫn 401.
- Login dùng AccountLookupService.FindForEmployeeLoginAsync + CheckPasswordSignInAsync(lockoutOnFailure: true). Câu lỗi chung và cách đếm IsNotAllowed giống AccountController. Không tạo cookie.
- Claim lấy từ IUserClaimsPrincipalFactory<ApplicationUser>, giữ nguyên role/NameIdentifier/person_id/employee_position/security stamp; không map claim type. Mỗi request kiểm Active, stamp và lockout, dựng lại claim để phản ánh việc nghỉ/đổi chức vụ ngay.
- Đơn/khách/tồn phòng chỉ Consultant/Manager/Administrator. Đọc danh mục Employee; ghi danh mục và mọi báo cáo Management; bill Accounting. Service tiếp tục tự kiểm quyền. Administrator chỉ đọc bill, service vẫn chặn duyệt/từ chối như web.
- DesktopApi có validate Issuer/Audience, TokenMinutes 1–1440 (mặc định 60), khóa UTF-8 tối thiểu 32 byte. Dev/Testing thiếu khóa dùng khóa ngẫu nhiên trong bộ nhớ. Production cấu hình thiếu/sai chỉ tắt desktop, login 503 và log cảnh báo, web vẫn chạy.
- Rate limit fixed window theo IP, mặc định 10 lần/60 giây; chỉ POST desktop login, không có global limiter. 429 có ProblemDetails và Retry-After.
- BookingException được dịch qua IStringLocalizer → 400; các khóa lỗi không tìm thấy → 404; concurrency và unique → 409. Lỗi bất ngờ trong action → 500 ProblemDetails chung, log nội bộ.
- Không đổi JsonOptions toàn cục, không thêm CORS. Swagger Development có Bearer Authorize riêng cho endpoint desktop được bảo vệ; XML comments và mã phản hồi cho mọi endpoint.

## Phần dùng chung với web

| File | Thay đổi | Kiểm chứng |
|---|---|---|
| `Program.cs` | Đăng ký service editor + desktop, thêm JWT không đổi defaults; status pages riêng theo prefix desktop; endpoint rate limiter; mô tả/security Swagger | Test scheme mặc định, cookie workspace, antiforgery, HTML 403/404, quote MVC, Swagger cũ/mới; toàn bộ test trước/sau |
| `AreasController.cs` | Chuyển trim, kiểm trùng tỉnh+tên, thêm/sửa, bắt unique sang CatalogEditorService | Giữ model errors, TempData, redirect, truy vấn Index; AdminWorkspaceUiTests và crawl |
| `AmenitiesController.cs` | Chuyển trim tên/icon, kiểm trùng, thêm/sửa, bắt unique | Giữ field lỗi Name và câu báo, Index/redirect; AdminWorkspaceUiTests và crawl |
| `RoomCategoriesController.cs` | Chuyển trim, kiểm trùng, thêm/sửa, bắt unique | Giữ field lỗi Name và câu báo, Index/redirect; AdminWorkspaceUiTests và crawl |
| `HolidaysController.cs` | Chuyển kiểm trùng ngày, thêm/sửa tên; ngày khóa chính không đổi | Giữ IsNew, ModelState.Remove(Date), thông báo và redirect year; AdminWorkspaceUiTests và crawl |

Logic mới dùng chung nằm trong `Services/CatalogEditorService.cs`. Không thêm audit khi tạo/sửa vì controller cũ không có; xóa vẫn qua CatalogDeletionService, kiểm đang sử dụng và audit giữ nguyên. View/CSS/JS, Identity/cookie events, controller và hợp đồng 10 API cũ không sửa.

## Kiểm thử

| Lượt chốt | Tổng | Pass | Fail | Skip |
|---|---:|---:|---:|---:|
| Mã gốc `e5f2aed`, .NET SDK 10.0.400 Linux, locale vi-VN | 1781 | 1780 | 0 | 1 |
| Nhánh hiện tại, cùng SDK/locale/provider | 1817 | 1816 | 0 | 1 |

Đối chiếu TRX theo tên từng test: **không thiếu test cũ, không có ca hồi quy; đúng 36 test mới và cả 36 pass**. Lượt lấy mốc 4,1552 phút; lượt sau 4,3440 phút; cả hai lệnh thoát 0. Kết quả gốc ở `/private/tmp/desktop-api-baseline-linux-results/desktop-suite.trx`, kết quả sau ở `/private/tmp/desktop-api-post-linux-results/desktop-suite.trx`; bản đối chiếu ở `/tmp/desktop-api-test-comparison.json`. `git diff --check` không báo lỗi.

Skip duy nhất là `AvailabilityPerformanceTests.StaffAvailabilityAndDatedSearchStayWithinTargets`, vốn yêu cầu `RUN_PERF_TESTS=1`; không thay đổi cấu hình hoặc test để bỏ qua thêm ca nào. `SeedDataSqliteTests` **pass** ở lượt mã gốc hiện tại; test này không sửa dù prompt ghi nhận từng trượt theo ngày. `ReportServiceSqlServerTests` **pass** trên SQL Server thật, dùng ConstraintTest riêng và rollback.

Lượt đầu đã chạy `dotnet test HomestayBooking.sln -c Release` trước khi sửa. Tiến trình không trả kết quả sau hơn 10 phút, được hủy bằng Ctrl+C qua session của chính nó. Chạy lại mã nguồn gốc `e5f2aed` trong thư mục tạm canonical `/private/tmp/desktop-api-baseline-canonical`, không dùng mã đã sửa. Các lượt macOS giới hạn hai luồng nhưng vẫn rất chậm do môi trường crypto; số liệu chốt lấy từ container SDK chính thức 10.0.400, `DOTNET_PROCESSOR_COUNT=4`, MaxParallelThreads=4, `LANG=LC_ALL=vi_VN.UTF-8`, rồi chạy bản sửa cùng cấu hình. Các lượt SQL Server trước/sau chạy tuần tự, không chạy hai lượt cùng lúc; lượt lọc desktop chỉ dùng InMemory/SQLite. User-secrets nguồn mount read-only; bản thay loopback cho Linux chỉ tồn tại trong container tạm và được xóa khi kết thúc.

Lệnh build chốt trên host: `dotnet build HomestayBooking.sln -c Release`, với biến môi trường local feed/cache tạm để tránh lỗi TLS và quyền ghi cache; kết quả **0 warning, 0 error**. Lệnh test chốt ở từng thư mục: `dotnet test HomestayBooking.sln -c Release --no-build --settings /qa.runsettings --results-directory /results`, thêm logger TRX/console. Các tham số bổ sung chỉ điều khiển số luồng và vị trí kết quả.

Lượt chẩn đoán Linux để locale mặc định có một test kế toán trượt vì `20,000` khác `20.000`; chạy riêng test đó với locale Việt Nam pass, rồi chạy lại toàn bộ mã gốc với locale này được kết quả trong bảng. Không sửa formatter hoặc nghiệp vụ để xử lý khác biệt môi trường. Các lượt desktop trung gian đã phát hiện MIME ProblemDetails bị Produces ghi đè và việc test tạo đơn cần SQLite; đã sửa trong phạm vi desktop/test. Dữ liệu token thử sai audience cũng được sửa để không giữ audience đúng từ token gốc.

Máy hiện có lỗi TLS của .NET/macOS (`AppleCrypto: bad certificate format`, `CSSMERR_DL_MDS_ERROR`). Restore gói JWT chính thức bằng curl tới NuGet, dùng local feed/cache trong `/tmp` vì cache NuGet ở home không được phép ghi. Không đổi chứng chỉ, cấu hình hệ thống hoặc sửa test có sẵn để né lỗi.

Các test mới: `DesktopApiTests`, `DesktopApiConfigurationTests`, helper `DesktopSqliteHost`. Bao phủ mọi endpoint với thành công và lỗi; login sai/không Active/lockout/khách/chủ; rate limit; token thiếu/hết hạn/sai chữ ký/issuer/audience; cookie-only; không cần antiforgery cho Bearer; thu hồi stamp/trạng thái/nghỉ việc; quyền chức vụ; CRUD/unique/not found; replay submissionId; file riêng tư; bill; báo cáo; web cookie/antiforgery/HTML errors/quote; Swagger 42 mới + 10 cũ; Production thiếu/sai cấu hình web vẫn chạy. Ca tạo đơn dùng SQLite có transaction và ExecuteUpdate thật, không mock service.

### Review bổ sung trên macOS — 22/10/2026

Đã đọc lại toàn bộ controller desktop, CatalogEditorService và diff của bốn controller danh mục Admin. Lượt `dotnet test HomestayBooking.sln --configuration Release --no-restore` trên Mac: **1816 pass, 0 fail, 1 skip, tổng 1817**, thời gian 5 phút 26 giây. Skip là ca hiệu năng cần RUN_PERF_TESTS=1.

Năm ca kiểm tra bổ sung qua HTTP/form MVC thật trong `/private/tmp/LTW-catalog-review-331187_k` đều pass. Bao phủ bốn danh mục: tạo/sửa/xóa, trim tên, tạo trùng, tên rỗng khi sửa, id không tồn tại và id giả gửi trong form; giữ nguyên khóa ngày lễ; cho phép khu vực cùng tên ở hai tỉnh và sửa khu vực không đổi tên. Các ca bổ sung này là công cụ review tạm, không nằm trong bộ test được commit.

Chưa phát hiện lỗi hồi quy cụ thể khi review. Giới hạn: ca unique race trong DesktopApiTests ném exception giả từ action filter; chưa kiểm thử tranh chấp ghi thực sự đi qua CatalogEditorService trên SQL Server. Bộ test trong repo cũng chưa có các ca tạo/sửa thành công qua đủ bốn form Admin như lượt review tạm.

### Bổ sung loại phòng đặt hộ — 22/10/2026

- Trước sửa: `dotnet test HomestayBooking.sln -c Release --no-restore --filter "FullyQualifiedName~DesktopApi|FullyQualifiedName~StaffBooking" --logger "console;verbosity=minimal"` → **62 pass, 0 fail, 0 skip**.
- Sau sửa: `dotnet build HomestayBooking.sln -c Release` → **0 warning, 0 error**. Restore có dòng chẩn đoán macOS `CSSM_ModuleLoad(): CSSMERR_DL_MDS_ERROR`, lệnh vẫn thoát 0.
- `dotnet test HomestayBooking.sln -c Release --no-build --no-restore --filter "FullyQualifiedName~DesktopApi|FullyQualifiedName~StaffBooking" --logger "console;verbosity=minimal"` → **66 pass, 0 fail, 0 skip**. Swagger kiểm đúng **43 desktop + 10 API cũ**.
- Thêm ba test RoomTypes trong DesktopApiTests: đối chiếu đủ trường với một homestay seed; SQLite thực thi truy vấn lọc, đếm đúng phòng Active và trả đủ 11 loại không phân trang, loại loại phòng/phòng ngừng kinh doanh hoặc không có phòng, trả mảng rỗng cho homestay ngừng hoạt động/hợp đồng không hợp lệ/không còn phòng Active; 404 ProblemDetails và 401. Thêm một ca Kế toán 403 vào theory policy. Test service hiện có kiểm thêm việc từ chối actor không hợp lệ ở method mới; helper BackofficeTestHost truyền AvailabilityService vào constructor mới.
- `git diff --check` không báo lỗi. Không commit, không dùng Docker, không tạo/sửa database SQL Server, không đụng `docs/bao-cao/`. Test chỉ dùng InMemory/SQLite trong bộ nhớ của fixture. Không chạy lại toàn bộ test/curl trên SQL Server trong phiên sandbox này; dành cho lượt kiểm tra tiếp theo trên Mac theo prompt.

## Database, app, crawl và curl

- Kiểm DB_ID trước khi tạo; `HomestayBooking_DesktopApi` **chưa tồn tại**. Tạo DB mới, thực thi nguyên `database/deploy.sql` (mở rộng `:r schema.sql`, thay TargetDb, chạy từng batch GO theo thứ tự và cùng session, giữ các SET option). Không xóa/recreate DB.
- Máy không có sqlcmd; SqlClient macOS gặp lỗi TLS trước khi tạo DB. Dùng helper .NET tạm chạy trong image chính thức `mcr.microsoft.com/dotnet/aspnet:10.0.11`, kết nối cùng SQL Server với máy chủ/tài khoản/mật khẩu lấy từ user-secrets Web. Chỉ đổi loopback thành host.docker.internal để tới cùng server từ Linux. Không lưu chuỗi kết nối/mật khẩu vào repo hay báo cáo.
- Chạy app **Development tại 5016**, `Database__AutoMigrate=false`, `ConnectionStrings__Default` chỉ tới DB mới. App ở container tạm **fuji-desktop-api-qa**, mount mã read-only, App_Data tạm riêng. Không sửa hay dừng container **sql-homestay**, LaunchAgent, Tailscale, app 8080/5011 hoặc các DB bị cấm.
- Health check 5016: **200**. Chạy đúng `python3 -I tools/ui-crawl.py http://localhost:5016`: **840 trang, 0 lỗi 500**, 4 lỗi 404. `/Bookings/Create` không có roomTypeId nên controller cũ trả NotFound; ba link `/Bookings/Details/FJ2611-0012`, `...0013`, `...0014` là dữ liệu minh họa trong `Views/Dev/UiKit.cshtml`. Không sửa view hoặc nghiệp vụ ngoài phạm vi.
- Curl thực tế: đăng nhập KT001/QL001/NV001 đều **200**. Me + bốn danh mục, đơn/chi tiết đơn, assignees, booking-homestays, availability, khách/chi tiết khách, receipts/file bill, cả năm báo cáo đều **200** với đúng vai trò. KT001→bookings và NV001→receipts **403**; anonymous me **401**. Không lưu token thật. Lệnh mẫu có chỗ điền token ở [tài liệu API](../api-va-backend.md#8-api-cho-ứng-dụng-desktop).
- Đã dừng đúng **PID 1 của app trong container riêng fuji-desktop-api-qa** bằng `docker exec fuji-desktop-api-qa /bin/kill -TERM 1`; container ứng dụng tạm tự kết thúc. DB kiểm thử được giữ nguyên. Không dùng pkill/killall.

## Cần người dùng quyết định

1. **Đã xử lý — 22/10/2026:** thêm `GET /api/desktop/booking-homestays/{homestayId:int}/room-types` gọi `StaffBookingService.RoomTypesAsync`, service kiểm actor bằng EmployeeAsync. Trả mảng không phân trang gồm `id`, `name`, `categoryId`, `categoryName`, `maxGuests`, `activeRoomCount`. Dùng lại `AvailabilityService.SellableRooms()` để lọc theo trạng thái homestay/loại phòng/phòng, sức chứa và hợp đồng hiện hành. Số phòng là số đang kinh doanh, tồn và giá theo ngày vẫn qua `/availability` và kiểm lại lúc tạo đơn. Homestay không tồn tại trả 404 ProblemDetails qua DesktopExceptionFilter, không có loại phù hợp trả 200 với mảng rỗng. StaffPolicy giữ Kế toán 403, thiếu Bearer 401. Không đổi HomestaysAsync, StaffHomestayOption hay màn web.

## Phạm vi commit

Theo yêu cầu người dùng, gom API desktop, CatalogEditorService, bốn controller Admin được refactor, đăng ký/middleware trong Program.cs, ba file test/helper và tài liệu API/deploy liên quan vào một commit. Không stage `docs/bao-cao/`, không đưa công cụ review tạm vào commit, không push hoặc merge.
