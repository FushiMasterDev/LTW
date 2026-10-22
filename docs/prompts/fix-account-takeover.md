# Prompt: chặn chiếm hồ sơ qua SĐT và dò mã đơn

Repo `/Users/phamngocphu/Downloads/LTW`, nhánh **`fix/account-takeover`** (đã tạo từ `main` = `9b75c26`). Đọc trước: `README.md`, `docs/api-va-backend.md`, `Services/CustomerAccountService.cs`, `Services/OwnerRegistrationService.cs` (`RegisterSelfAsync`, `PreparePersonAsync`), `Controllers/AccountController.cs` (Register), `Controllers/PartnerController.cs` hoặc nơi xử lý `PartnerRegisterViewModel`, `Controllers/BookingsController.cs` (`Lookup`, `CanAccess`), `Controllers/Api/BookingsApiController.cs` (`Lookup`), `DesktopApi/DesktopApiRegistration.cs` (rate limiter đang có), `Program.cs`.

## Lỗi 1: chiếm hồ sơ khách vãng lai khi đăng ký

Hiện ai cũng đăng ký tài khoản khách (`CustomerAccountService.RegisterAsync`) hoặc tự đăng ký chủ homestay (`OwnerRegistrationService` → `PreparePersonAsync`) bằng SĐT của một hồ sơ chưa có tài khoản (khách đặt không đăng nhập, nhân viên đặt hộ). Tài khoản mới được gắn vào hồ sơ đó nên xem/hủy được đơn của người khác. Chưa có SMS/OTP.

**Đã chốt: xác minh bằng mã đơn.**

- Hồ sơ cần xác minh = `PersonProfile` có SĐT trùng, **chưa có Account**, và `Customer` của hồ sơ có **ít nhất một đơn** (mọi trạng thái).
- Với hồ sơ cần xác minh, người đăng ký phải nhập **mã một đơn thuộc hồ sơ đó** (so sánh sau khi trim, không phân biệt hoa thường). Đúng → gắn tài khoản như hiện nay. Thiếu/sai → từ chối, không ghi gì vào DB, báo: "Số điện thoại này đã có đơn đặt phòng. Vui lòng nhập đúng mã một đơn đã đặt để xác minh, hoặc liên hệ Fuji."
- SĐT chưa có hồ sơ, hoặc hồ sơ chưa có đơn nào → giữ nguyên hành vi hiện tại. Kiểm tra xem khách đăng nhập có xem được giấy tờ (CCCD/thẻ SV) hoặc dữ liệu nhạy cảm nào khác của hồ sơ không có đơn không; nếu có thì ghi vào báo cáo, **không** tự mở rộng luật.
- Áp dụng cho **cả hai** đường: đăng ký khách và chủ homestay tự đăng ký (trường hợp hồ sơ là khách vãng lai, chưa Account, chưa Owner). Luồng nhân viên hỗ trợ đăng ký chủ (`CreateAssistedAsync`) và các trường hợp `PreparePersonAsync` đang từ chối giữ nguyên.
- Đặt logic kiểm tra ở **một chỗ dùng chung** (ví dụ method trong `CustomerAccountService` hoặc service nhỏ mới) và gọi từ cả hai service, trong cùng transaction hiện có.
- Form: thêm ô tùy chọn "Mã đơn đã đặt (nếu từng đặt phòng bằng số này)" vào form đăng ký khách và form đăng ký đối tác (view model + view + resource dịch nếu có). Giữ phong cách form hiện có (tag helper `form-field`…).
- **Chống thử mã:** đếm lần xác minh sai theo SĐT (E.164): sai 5 lần trong 15 phút → khóa xác minh SĐT đó 15 phút, báo "Thử quá nhiều lần, vui lòng thử lại sau 15 phút.". Dùng `IMemoryCache` (app chạy một instance) và ghi comment `shortcut: đếm trong bộ nhớ, mất khi khởi động lại; chuyển sang DB/Redis nếu chạy nhiều instance`. Thời gian lấy từ `TimeProvider` để test được.
- Ghi `AccountActivityLog` khi gắn tài khoản vào hồ sơ có sẵn sau xác minh (dùng event type có sẵn phù hợp nhất, ghi rõ "xác minh bằng mã đơn" trong mô tả).

## Lỗi 2: dò mã đơn qua tra cứu

`/Bookings/Lookup` (POST) và `GET /api/bookings/{code}?phone=` không giới hạn số lần; mã đơn tăng dần nên đoán được. **Đã chốt: chỉ giới hạn số lần, không đổi định dạng mã đơn.**

- Thêm rate limit theo IP (fixed window, mặc định 10 lần/phút, cấu hình được) cho hai endpoint trên **và** POST đăng ký khách, POST đăng ký đối tác. Dùng `AddRateLimiter` + `EnableRateLimiting` như policy `DesktopLogin`; vẫn **không** đặt global limiter.
- `OnRejected` hiện đang ghi ProblemDetails JSON cho mọi request bị chặn. Sửa để: đường dẫn `/api/...` trả 429 ProblemDetails như cũ; trang MVC trả 429 rồi để `UseStatusCodePagesWithReExecute` hiện trang HTML (kiểm tra `/Home/StatusCode/429` hiển thị thông báo dễ hiểu, thêm nội dung nếu thiếu). Hành vi 429 của `POST /api/desktop/auth/login` giữ nguyên.
- Nếu cần, gom cấu hình rate limit web vào một section riêng (ví dụ `RateLimits`), đừng nhét vào `DesktopApiOptions`.

## Ràng buộc

- Không đổi hành vi đăng nhập, các endpoint `/api/desktop`, hay luồng nào khác ngoài phạm vi trên.
- Test hiện có về đăng ký với hồ sơ có sẵn (`CustomerAccountServiceTests`, test đăng ký chủ…) sẽ phải đổi theo luật mới: sửa có chủ đích, ghi trong báo cáo test nào đổi và vì sao.
- Không commit (sandbox không ghi được `.git`), không dùng Docker, không tạo/sửa database (SqlClient trong sandbox lỗi TLS). Không đụng `docs/bao-cao/`. Không thêm dòng Co-Authored-By, không ghi tên công cụ AI trong file.

## Test cần thêm

- Khách: đăng ký SĐT mới OK; SĐT có hồ sơ không đơn OK; có đơn + thiếu mã / sai mã / mã của khách khác → từ chối và DB không đổi; mã đúng (khác hoa thường, có khoảng trắng) → gắn đúng hồ sơ; sai 5 lần → khóa, lần 6 dù đúng mã vẫn bị chặn; sau 15 phút (TimeProvider giả) thử lại được.
- Chủ tự đăng ký: các ca tương ứng, gồm hồ sơ khách vãng lai có đơn.
- Rate limit: tra cứu MVC quá giới hạn → 429 HTML; API tra cứu → 429 ProblemDetails; desktop login vẫn 429 ProblemDetails.

## Kiểm tra và báo cáo

Chạy `dotnet build HomestayBooking.sln -c Release` và các test liên quan (`--filter` theo tên lớp test bạn sửa/thêm, cộng `DesktopApi`). Toàn bộ test và thử trên trình duyệt/curl sẽ chạy trên Mac sau.

Ghi báo cáo ngắn vào `docs/security/account-takeover-fix.md`: luật mới, file đã sửa, test đã đổi/thêm, kết quả build/test, điểm chưa chắc. Cập nhật `docs/api-va-backend.md` (rate limit tra cứu) và README nếu có mô tả luồng đăng ký.
