# Chặn chiếm hồ sơ qua SĐT và dò mã đơn

Nhánh thực hiện: `fix/account-takeover`.

## Luật và phạm vi sửa

- Đăng ký khách và chủ tự đăng ký phải xác minh mã một đơn thuộc Customer của PersonProfile trùng SĐT, chưa có Account, nếu Customer có ít nhất một đơn ở bất kỳ trạng thái nào. Mã được trim và so sánh không phân biệt hoa thường; không chấp nhận đơn của khách khác dù trùng SĐT liên hệ.
- Thiếu/sai mã: trả đúng thông báo yêu cầu xác minh trước mọi thay đổi hồ sơ, tạo Account, Owner hay hồ sơ đối tác. Logic dùng chung trong transaction đăng ký hiện có.
- Bộ đếm dùng chung theo E.164 giữa hai luồng: 5 lần sai trong cửa sổ trượt 15 phút khóa 15 phút kể từ lần thứ 5. Lần thứ 5 trả thông báo khóa; mã đúng cũng bị chặn trong thời gian khóa. Kiểm tra thời gian bằng TimeProvider; IMemoryCache được bảo vệ khi cập nhật đồng thời. Có comment về mất dữ liệu khi restart và chuyển DB/Redis khi chạy nhiều instance.
- Ghép tài khoản sau xác minh ghi AccountActivityLog, event `AccountCreated`, mô tả có “xác minh bằng mã đơn”; không ghi mã đơn hay mật khẩu vào log.
- SĐT mới/hồ sơ chưa có đơn, chủ đã đăng nhập, các nhánh từ chối hiện có và `CreateAssistedAsync` giữ luật cũ.
- Bốn endpoint dùng chung policy fixed window theo IP, mặc định 10 lần/60 giây: POST `/Bookings/Lookup`, GET `/api/bookings/{code}`, POST `/Account/Register`, POST `/Partner/Index` (form cũng truy cập tại `/Partner`). Không có global limiter; GET các form và đăng nhập web không gắn policy mới. Cấu hình `RateLimits:PermitLimit`, `RateLimits:WindowSeconds`, tách khỏi DesktopApiOptions.
- OnRejected trả 429 + Retry-After: API giữ ProblemDetails JSON, MVC để status pages re-execute HTML với thông báo dễ hiểu. Policy/cấu hình desktop login và các nghiệp vụ desktop giữ nguyên.
- Hai form có BookingCode tùy chọn qua `form-field`, view model, controller/input và bản dịch tiếng Anh. Câu giới thiệu đăng ký khách được sửa để phản ánh bước xác minh.

## File sửa/thêm

Đường dẫn dưới đây tương đối với thư mục dự án tương ứng.

| Nhóm | File |
|---|---|
| Web: logic, DI, cấu hình | `Services/BookingCodeVerificationService.cs` (mới), `Services/PublicRateLimits.cs` (mới), `Services/CustomerAccountService.cs`, `Services/OwnerRegistrationService.cs`, `Program.cs`, `appsettings.json` |
| Web: endpoint và phản hồi | `Controllers/AccountController.cs`, `Controllers/PartnerController.cs`, `Controllers/BookingsController.cs`, `Controllers/Api/BookingsApiController.cs`, `DesktopApi/DesktopApiRegistration.cs` |
| Web: form và dịch | `Models/ViewModels/BookingViewModels.cs`, `Models/ViewModels/PartnerViewModels.cs`, `Views/Account/Register.cshtml`, `Views/Partner/Index.cshtml`, `Views/Home/StatusCode.cshtml`, `Resources/SharedResource.en.resx` |
| Test mới | `Services/AccountTakeoverPreventionTests.cs`, `Integration/PublicRateLimitTests.cs` |
| Test/fixture cập nhật | `Identity/CustomerAccountServiceTests.cs`, `Identity/IdentityTestHost.cs`, `Services/PartnerTestHost.cs`, `Services/BackofficeTestHost.cs`, `Services/GuestCustomerServiceTests.cs`, `Services/AnonymousBookingTests.cs`, `Services/BookingContactVisibilityTests.cs` |
| Tài liệu gốc repo | `README.md`, `docs/api-va-backend.md`, báo cáo này |

## Test thay đổi và bổ sung

- Đổi tên `CustomerAccountServiceTests.Register_PhoneOfCallCenterCustomer_AttachesAccountToExistingProfile` thành `Register_PhoneOfCallCenterCustomerWithoutBookings_AttachesAccountToExistingProfile`: fixture cũ chỉ có Customer, không có đơn, nên **giữ kỳ vọng đăng ký thành công**, làm rõ điều kiện trong tên test. Các test chủ cũ dùng hồ sơ có Account vẫn giữ kỳ vọng từ chối, hoặc yêu cầu đúng actor đang đăng nhập; không hạ điều kiện để làm test qua.
- Các fixture được truyền service xác minh và bộ đếm singleton mới. AnonymousBookingTests, BookingContactVisibilityTests, GuestCustomerServiceTests chỉ đổi cách dựng dependency; giữ toàn bộ assertion nghiệp vụ.
- AccountTakeoverPreventionTests chạy các ca cho cả hai service trên SQLite trong bộ nhớ: hồ sơ không có Customer/không có đơn vẫn nhận Account; thiếu/trắng/sai/mã của khách khác bị từ chối và snapshot DB từ context khác không đổi; mã đúng khác hoa thường/có khoảng trắng nhận đúng hồ sơ ở mọi BookingStatus; log đúng AccountCreated, không lộ mã; sai 5 lần, lần 6 đúng vẫn khóa; sát mốc và đúng mốc hết khóa sau 15 phút; lỗi cũ rơi khỏi cửa sổ; dùng chung bộ đếm giữa hai luồng và các dạng SĐT, cách ly SĐT khác; hỗ trợ đăng ký chủ vẫn nhận khách vãng lai có đơn. SĐT mới đã được phủ bởi CustomerAccountServiceTests và OwnerRegistrationServiceTests.
- PublicRateLimitTests đi qua WebApplicationFactory: ba POST MVC quá giới hạn cấu hình trả 429 HTML, GET form vẫn dùng được; API trả 429 ProblemDetails + Retry-After; cùng ngân sách giữa bốn route; IP khác độc lập, IPv4 và IPv4-mapped IPv6 dùng cùng khóa; không global limit; hai form có trường mã đơn và nhãn rõ ràng. DesktopApiTests giữ test 429 ProblemDetails + Retry-After của desktop login.
- Kiểm chứng hồi quy: tạm bỏ xác minh ở service dùng chung, chạy 8 ca thiếu/sai/mã của khách khác → cả 8 thất bại vì đăng ký lại thành công; đã khôi phục source sau phép thử.

## Kiểm tra dữ liệu nhạy cảm của hồ sơ chưa có đơn

Rà soát code, không mở rộng luật xác minh:

- Chưa tìm thấy endpoint/view dành cho khách để đọc `CustomerProfile.CitizenId`, `StudentId` hoặc ghi chú nội bộ Customer.Note. Các trường này chỉ đi qua CustomerService cho nhân viên, giao diện Staff và DTO desktop; service kiểm nhân viên, CCCD/thẻ SV che một phần cho tư vấn. Form đặt phòng của khách chỉ điền sẵn họ tên và SĐT.
- **Dữ liệu tài chính vẫn có thể hiện:** BookingsController.Mine và form đặt phòng đọc voucher theo CustomerId; trang Mine hiển thị mã, số dư và hạn voucher. Schema cho phép `Voucher.SourceBookingId` null, nên nếu hồ sơ không có đơn nhưng vẫn có voucher, đăng ký theo luật giữ nguyên có thể xem/dùng voucher đó. Đây là kết luận từ code/schema, chưa xác minh bằng trình duyệt; không thêm điều kiện voucher vào luật.
- Email cũ được giữ khi đăng ký khách bỏ trống email và được gắn vào Account mới. Chưa thấy trang khách hiển thị CCCD/thẻ SV/địa chỉ hồ sơ cũ.
- Trường hợp ngoài khách thuần cần lưu ý: nếu PersonProfile chưa có Account/chưa có đơn nhưng đã có Owner, đăng ký khách vẫn theo luật cũ. Từ code, sau khi nhận Account, người đó có thể tự đăng ký đối tác trên cùng hồ sơ khi không còn hồ sơ đối tác mở, nhận role Owner rồi truy cập homestay/giấy tờ của Owner đó. Đây là suy luận từ CustomerAccountService, PreparePersonAsync và OwnerRegistrationQueryService, chưa thử trên dữ liệu thật; các nhánh từ chối hiện có không bị đổi trong bản sửa này.

## Xác minh và giới hạn

Không dùng Docker, không kết nối/tạo/sửa SQL Server hay chạy migration; test service dùng SQLite trong bộ nhớ, test tích hợp dùng EF InMemory riêng. Không commit, không sửa `docs/bao-cao/`.

Lần chạy tích hợp mặc định bị treo tại WebApplication.CreateBuilder trước khi cấu hình ứng dụng. Tắt theo dõi thay đổi cấu hình bằng `DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false` trong tiến trình test giúp chạy tiếp; biến này chỉ dùng cho tiến trình test, không thêm vào cấu hình ứng dụng.

Kết quả cuối trên source đã khôi phục sau phép thử hồi quy:

- `dotnet build HomestayBooking.sln -c Release`: exit 0, **0 warning, 0 error**.
- Lệnh dưới: exit 0, **205 passed, 0 failed, 0 skipped** (1 phút 12 giây), bao gồm toàn bộ lớp DesktopApi khớp filter.
- `git diff --check`: exit 0. Trước đó, nhóm service đạt 156/156 và nhóm PublicRateLimit + DesktopApi đạt 49/49 khi chạy riêng.

```bash
DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false \
  dotnet test HomestayBooking.Tests/HomestayBooking.Tests.csproj -c Release --no-build \
  --filter 'FullyQualifiedName~CustomerAccountServiceTests|FullyQualifiedName~OwnerRegistrationServiceTests|FullyQualifiedName~AccountTakeoverPreventionTests|FullyQualifiedName~PublicRateLimitTests|FullyQualifiedName~GuestCustomerServiceTests|FullyQualifiedName~AnonymousBookingTests|FullyQualifiedName~BookingContactVisibilityTests|FullyQualifiedName~DesktopApi' \
  --logger 'console;verbosity=minimal'
```

Chưa chạy toàn bộ suite, trình duyệt/curl hoặc SQL Server thật theo phạm vi yêu cầu; cần xác nhận trên Mac sau. Mã đơn vẫn tăng dần như cũ; xác minh này dựa vào việc giữ kín mã đơn và các giới hạn thử, chưa phải xác minh sở hữu SĐT bằng SMS/OTP.

Nguồn đối chiếu middleware: [ASP.NET Core rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0).
