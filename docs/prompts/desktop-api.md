# Prompt: Web API cho ứng dụng WinForms (đợt 1, chỉ nhân viên)

Repo: `/Users/phamngocphu/Downloads/LTW`, nhánh **`feature/desktop-api`** (đã tạo từ `main`). Đọc trước: `README.md`, `docs/api-va-backend.md`, `Program.cs`, `Controllers/Api/*`, `Areas/Staff/Controllers/StaffController.cs`, `Areas/Accounting/Controllers/AccountingController.cs`, `Areas/Admin/Controllers/AdminController.cs`.

## Mục tiêu

Thêm Web API REST cho một ứng dụng desktop WinForms **dành cho nhân viên Fuji** (tư vấn, kế toán, quản lý, quản trị), đăng nhập bằng **mã nhân viên + mật khẩu**, xác thực bằng **JWT Bearer**. API chỉ là lớp vỏ mỏng gọi lại **service có sẵn**; không viết lại nghiệp vụ.

**Yêu cầu số 1: không ảnh hưởng web.** Mọi trang MVC, 10 endpoint `/api/...` đang có, đăng nhập cookie, antiforgery, trang lỗi HTML, JSON của các action MVC phải giữ nguyên hành vi. Toàn bộ test hiện có phải còn pass (trừ test đã trượt sẵn trước khi bạn sửa, xem mục Kiểm tra).

## Ràng buộc kỹ thuật (bắt buộc)

1. **Tiền tố riêng `/api/desktop/...`** cho mọi endpoint mới. Không đổi đường dẫn, dạng request/response của 10 endpoint cũ.
2. **Cookie Identity vẫn là scheme mặc định.** JWT chỉ là scheme thêm (`AddJwtBearer`). Thêm gói `Microsoft.AspNetCore.Authentication.JwtBearer` đúng bản 10.0.x khớp các gói ASP.NET Core đang dùng.
3. Mọi controller desktop kế thừa một base controller chung có `[ApiController]`, `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`, `[IgnoreAntiforgeryToken]`. **Chỉ nhận Bearer, không nhận cookie** (đó là lý do bỏ antiforgery an toàn). Có test: gửi cookie đăng nhập hợp lệ mà không có Bearer tới endpoint desktop → 401.
4. **Policy giữ nguyên ý nghĩa như web:**
   - Đơn, khách, phòng trống: vị trí Consultant/Manager/Administrator (giống bộ lọc trong `StaffController`; Accountant bị 403).
   - Duyệt bill: `AppPolicies.Accounting`.
   - Báo cáo, ghi danh mục: `AppPolicies.Management`.
   - Đọc danh mục (để đổ dropdown): mọi nhân viên (`AppPolicies.Employee`).
   - Service vẫn tự kiểm quyền lại như hiện nay; lấy `Actor`/EmployeeId giống `StaffController.CurrentActorAsync` và `AccountingController.CurrentEmployeeIdAsync`.
5. **Claim trong token** lấy từ `IUserClaimsPrincipalFactory<ApplicationUser>` (`AppClaimsPrincipalFactory`) để policy cũ chạy được với Bearer. Kiểm tra kỹ việc map claim type (role, NameIdentifier, EmployeePosition) giữa token và policy; có test chứng minh.
6. **Khóa tài khoản phải có hiệu lực ngay:** token chứa security stamp; ở `OnTokenValidated` nạp user, từ chối nếu `Status != Active` hoặc stamp đã đổi (giống `AppSignInManager.ValidateSecurityStampAsync` của web). Thời hạn token mặc định 60 phút, cấu hình được.
7. **Cấu hình JWT** ở section `DesktopApi` (`SigningKey`, `Issuer`, `Audience`, `TokenMinutes`) với validate. Không ghi khóa thật vào repo.
   - Development và test: nếu không cấu hình khóa thì sinh khóa ngẫu nhiên lúc khởi động.
   - Production thiếu khóa: **app web vẫn chạy bình thường**, API desktop tắt (login trả 503 ProblemDetails) và log cảnh báo. Không được làm app dừng khởi động.
   - Thêm `DesktopApi__SigningKey` vào mẫu cấu hình production trong `deploy/mac/` (file mẫu/hướng dẫn, không phải file bí mật thật) và runbook `docs/deploy/self-host-mac.md`.
8. **Lỗi trả `ProblemDetails` JSON** qua một exception filter **chỉ gắn trên base controller desktop**: `BookingException` → 400 (dịch qua `IStringLocalizer` như API cũ), không tìm thấy → 404, `DbUpdateConcurrencyException` → 409, trùng giá trị duy nhất → 409. 401/403 của endpoint desktop phải là JSON, không bị `UseStatusCodePagesWithReExecute` biến thành trang HTML; trang lỗi HTML của web giữ nguyên (có test).
9. **Enum trả dạng chữ chỉ trong DTO desktop** (attribute trên DTO hoặc formatter riêng cho controller desktop). **Không đổi `JsonOptions` toàn cục** (ô báo giá `booking-quote.js` đang dùng JSON của MVC).
10. **Rate limit** bằng `AddRateLimiter` có sẵn, **chỉ** gắn policy cho `POST /api/desktop/auth/login` (ví dụ 10 lần/phút/IP, có cấu hình). Không đặt global limiter. Vượt → 429 ProblemDetails.
11. **DTO riêng**, không trả entity. Danh sách có phân trang `{ items, page, totalPages, totalCount }`, dùng lại `PaginatedList`. `DateOnly` dạng `yyyy-MM-dd`; thời điểm trả UTC (ghi rõ trong Swagger).
12. Không thêm CORS (WinForms không phải trình duyệt).
13. **Swagger** (vẫn chỉ Development): thêm security scheme Bearer (nút Authorize) áp cho nhóm desktop; sửa câu mô tả `"API công khai..."` cho đúng. XML comment cho mọi endpoint và mã phản hồi như `OwnerBookingsApiController`.

## Endpoint đợt 1

Tên DTO, field cụ thể do bạn chọn theo service. Mỗi endpoint gọi đúng service ghi bên cạnh.

**Đăng nhập** (`AllowAnonymous` riêng cho login)
- `POST /api/desktop/auth/login` `{ employeeCode, password }` → `{ accessToken, expiresAt, employee: { id, code, fullName, position } }`. Dùng `AccountLookupService.FindForEmployeeLoginAsync` + `SignInManager.CheckPasswordSignInAsync(..., lockoutOnFailure: true)` (đã kiểm `Status` qua `AppSignInManager.CanSignInAsync`). Câu báo lỗi và việc ghi nhật ký đăng nhập giống `AccountController` phần đăng nhập nhân viên. Không phân biệt "sai mã" với "sai mật khẩu".
- `GET /api/desktop/auth/me`.

**Danh mục**: khu vực, tiện nghi, loại phòng (`RoomCategory`), ngày lễ
- `GET /api/desktop/{areas|amenities|room-categories|holidays}` (mọi nhân viên), `GET .../{id}`, `POST`, `PUT .../{id}`, `DELETE .../{id}` (Management).
- Logic thêm/sửa và kiểm trùng hiện nằm trong `Areas/Admin/Controllers/{Areas,Amenities,RoomCategories,Holidays}Controller.cs`. **Chuyển nguyên logic đó vào một service dùng chung** để cả controller MVC và API gọi; câu báo lỗi, kiểm trùng, bắt lỗi unique, audit (nếu có) giữ y hệt. Xóa dùng `CatalogDeletionService`. Màn danh mục web phải hoạt động y như cũ (`AdminWorkspaceUiTests` pass).

**Đơn đặt phòng** (Consultant/Manager/Administrator)
- `GET /api/desktop/bookings` (lọc như `StaffBookingQuery`, phân trang) → `StaffBookingService.SearchAsync`
- `GET /api/desktop/bookings/{id}` → `DetailsAsync`
- `POST /api/desktop/bookings` → `CreateForCustomerAsync` (nhận `submissionId` để gửi lặp không tạo đơn đôi)
- `POST /api/desktop/bookings/{id}/assign` `{ employeeId }` → `AssignAsync`; `GET /api/desktop/assignees` → `AssigneesAsync`
- `POST /api/desktop/bookings/{id}/status` `{ status, newRoomId?, note }` → `ChangeStatusAsync`
- `GET /api/desktop/booking-homestays` (lựa chọn homestay/loại phòng cho form đặt hộ) → `StaffBookingService.HomestaysAsync`
- `GET /api/desktop/availability?checkIn=&checkOut=&areaId=&guests=` → `AvailabilityService.StaffAvailabilityAsync`

**Khách hàng** (Consultant/Manager/Administrator)
- `GET /api/desktop/customers?q=&page=`, `GET .../{id}`, `PUT .../{id}` → `CustomerService`

**Kế toán duyệt bill** (`AppPolicies.Accounting`)
- `GET /api/desktop/receipts?status=&page=` → `AccountingService.ReceiptsAsync`
- `GET /api/desktop/receipts/{paymentId}/file` → trả file ảnh bill theo đúng cách màn kế toán web đang phục vụ (dùng `ReceiptFileStore`, giữ kiểm quyền)
- `POST /api/desktop/receipts/{paymentId}/approve` → `ReceiptReviewService.ApproveAsync`
- `POST /api/desktop/receipts/{paymentId}/reject` `{ reason }` → `RejectAsync`

**Báo cáo** (Management)
- `GET /api/desktop/reports/{management|bookings|financial|employees|homestays}` (tham số theo `ReportQuery`) → `ReportService` tương ứng.

Nếu một service thiếu dữ liệu cần cho WinForms, **không sửa nghiệp vụ**: ghi vào báo cáo mục "Cần người dùng quyết định".

## Kiểm thử

Thêm test tích hợp trong `HomestayBooking.Tests/Integration/` dùng `TestWebApplicationFactory`:
- Login: đúng → token; sai mật khẩu/sai mã → 401 cùng một câu; tài khoản khóa/không Active → bị chặn; khách/chủ (không phải nhân viên) → 401; vượt rate limit → 429.
- Bearer: thiếu/hết hạn/sai chữ ký → 401 JSON; cookie không có Bearer → 401; POST với Bearer không cần antiforgery; khóa tài khoản hoặc đổi security stamp sau khi cấp token → 401.
- Phân quyền theo bảng ở mục Ràng buộc 4 (ví dụ Consultant → 403 ở receipts/reports/ghi danh mục; Accountant → 403 ở bookings).
- Mỗi endpoint: một ca thành công và một ca lỗi (404/400/409).
- **Web không đổi:** đăng nhập cookie và các trang Admin/Staff/Accounting vẫn 200; trang lỗi 404/403 của web vẫn là HTML; ô báo giá `Bookings/Quote` trả JSON như cũ; 10 endpoint cũ giữ nguyên test hiện có.

## Kiểm tra trước khi xong

1. `dotnet build HomestayBooking.sln -c Release` không cảnh báo mới; `dotnet test HomestayBooking.sln -c Release --no-build`. **Chạy test một lần trước khi sửa** để ghi mốc (lần duyệt 21/10: `SeedDataSqliteTests` trượt do phụ thuộc ngày chạy, không thuộc phạm vi này, không sửa). Sau khi sửa: không có test nào trượt thêm so với mốc. Test SQL Server chạy tuần tự, không chạy hai lượt cùng lúc.
2. Tạo database riêng `HomestayBooking_DesktopApi` bằng `database/deploy.sql` như `README.md` mục 2 (nếu đã tồn tại thì dừng, báo, không xóa). Chạy app **Development ở cổng 5016** trỏ vào database đó qua biến môi trường `ConnectionStrings__Default` (lấy máy chủ/mật khẩu từ user-secrets của `HomestayBooking.Web`, không ghi mật khẩu vào file nào trong repo hay báo cáo). Rồi:
   - `python3 -I tools/ui-crawl.py http://localhost:5016`: không có trang lỗi 500, số trang tương đương lần trước (840).
   - Gọi thử bằng curl: login KT001/QL001/NV001 → gọi mỗi nhóm endpoint; lưu lệnh mẫu (không kèm token thật) vào tài liệu.
   - Dừng app bằng đúng PID của mình.
3. Cập nhật `docs/api-va-backend.md` (thêm phần "API cho ứng dụng desktop": đăng nhập, cách gửi Bearer, bảng endpoint, quyền, mã lỗi, ví dụ curl) và bảng Web API trong `README.md`.

## Cấm

- Không đụng app đang chạy cổng 5011, app Production cổng 8080, database `HomestayBooking`, `HomestayBooking_v5`, `HomestayBooking_PreLaunch*`, LaunchAgent, Tailscale, container `sql-homestay` (không dừng/xóa/tạo lại).
- **Không dùng `pkill -f`/`killall` với mẫu ngắn.** Chỉ dừng đúng PID mình tạo.
- Không `git add -A`/`git add .`: thư mục `docs/bao-cao/` là file của người dùng chưa commit, không được thêm hay sửa. Chỉ add đúng file mình sửa.
- Commit nhỏ theo bước (`feat(api): ...`, `refactor(catalog): ...`, `test(api): ...`, `docs(api): ...`), tác giả là cấu hình git hiện tại. **Không thêm dòng `Co-Authored-By`, không ghi tên công cụ AI nào trong commit message.** Không push, không merge, không đổi nhánh.
- Không sửa nghiệp vụ, view, CSS, JS của web (ngoại lệ duy nhất: chuyển logic danh mục ra service như trên).

## Báo cáo

Viết `docs/api/desktop-api-report.md`: danh sách endpoint đã làm, các thay đổi đụng tới phần web dùng chung (`Program.cs`, 4 controller danh mục) và cách đã kiểm là không đổi hành vi, kết quả test trước/sau (số pass/fail/skip), kết quả ui-crawl, curl, mục **"Cần người dùng quyết định"**, các commit. Rồi dừng.
