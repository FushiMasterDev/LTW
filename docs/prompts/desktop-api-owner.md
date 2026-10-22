# Prompt: Web API cho ứng dụng WinForms (đợt 2, chủ homestay)

Repo `/Users/phamngocphu/Downloads/LTW`, nhánh **`feature/desktop-api-owner`** (đã tạo từ `main` = `c70c597`). Đợt 1 (nhân viên) đã merge. Đọc trước: `docs/prompts/desktop-api.md` (mọi ràng buộc của đợt 1 vẫn áp dụng), `docs/api/desktop-api-report.md`, `docs/api-va-backend.md` mục 8, `DesktopApi/*`, `Controllers/Api/Desktop/*`, toàn bộ `Areas/Owner/Controllers/*` và các service chúng gọi, `Controllers/Api/OwnerBookingsApiController.cs`, `Controllers/Api/RoomBlocksApiController.cs`, `Controllers/AccountController.cs` (đăng nhập SĐT), `Services/AccountLookupService.cs`.

## Mục tiêu

Thêm REST API dưới `/api/desktop/owner/...` cho WinForms của **chủ homestay**, phủ **toàn bộ chức năng** khu `/Owner` trên web:

- Tổng quan (`DashboardController`), lịch làm việc (`ScheduleController`).
- Đơn: danh sách/chi tiết, xác nhận, từ chối và mọi thao tác khác `Owner/BookingsController` đang có.
- Lịch phòng theo tháng, khóa/mở khóa phòng (`CalendarController`, `RoomBlockService`).
- Homestay: mọi thao tác của `Owner/HomestaysController` (thông tin, loại phòng, phòng, giá, tiện nghi, ảnh, giấy tờ… đúng những gì web cho phép), gồm upload file qua `multipart/form-data`, dùng lại kiểm tra upload/lưu file hiện có (`ReceiptFileStore`, các validator upload), file riêng tư chỉ trả qua endpoint có kiểm quyền.
- Đánh giá: xem, trả lời (`ReviewsController`).
- Đối soát: danh sách, chi tiết và các thao tác chủ được làm (`SettlementsController`).

API chỉ là lớp vỏ mỏng gọi lại **service có sẵn**, không viết lại nghiệp vụ. Nếu logic đang nằm trong controller MVC, chuyển vào service để web và desktop dùng chung (như `CatalogEditorService` ở đợt 1), giữ nguyên hành vi web.

**Yêu cầu số 1: không ảnh hưởng web.** Mọi trang MVC, endpoint `/api/...` cũ và `/api/desktop/...` của nhân viên giữ nguyên đường dẫn, request/response, xác thực, antiforgery, trang lỗi. Toàn bộ test hiện có phải còn pass.

## Đăng nhập và phân quyền

1. `POST /api/desktop/owner/auth/login` bằng **SĐT + mật khẩu**, dùng `FindForPhoneLoginAsync` và đúng cách web xử lý sai mật khẩu/khóa tài khoản/không Active (đối chiếu `AccountController` như `DesktopAuthController` đã làm). Chỉ tài khoản có vai trò **Owner** và thỏa `AppPolicies.ApprovedOwner` mới nhận token; khách thường, nhân viên → 401 cùng một thông báo. Gắn rate limit `DesktopLogin` có sẵn. `GET /api/desktop/owner/auth/me`.
2. Token dùng chung khóa/issuer/audience/thời hạn của `DesktopApi`. `OnTokenValidated` hiện từ chối mọi người không phải nhân viên đang làm: tổng quát hóa để **nhân viên vẫn bị kiểm y như cũ**, còn chủ được nhận khi Account Active, stamp khớp và vẫn là Owner hợp lệ (nạp lại claim từ factory mỗi request như hiện nay). Token nhân viên gọi API chủ → 403, token chủ gọi API nhân viên → 403.
3. Base controller `DesktopController` đang gắn `Policy = AppPolicies.Employee`. Tách sao cho controller nhân viên giữ nguyên policy (không đổi kết quả 401/403 nào của đợt 1), controller chủ có base riêng với `AppPolicies.ApprovedOwner`, vẫn chỉ Bearer + `[IgnoreAntiforgeryToken]` + `DesktopExceptionFilter`.
4. **Chống IDOR:** mọi endpoint chỉ thao tác trên homestay/phòng/đơn/đánh giá/đối soát của chính chủ đang đăng nhập; id của chủ khác → 404 (không lộ tồn tại). Dùng đúng cơ chế kiểm sở hữu service/controller web đang dùng (`Actor` của chủ, `OwnerHomestaysAsync`…).

## Quy ước (giữ như đợt 1)

DTO riêng cho desktop, enum dạng chữ chỉ trong DTO desktop, không đổi `JsonOptions` toàn cục; ngày `yyyy-MM-dd`, timestamp UTC; phân trang dùng `DesktopPage`; lỗi ProblemDetails qua `DesktopExceptionFilter` (bổ sung thông báo "Không tìm thấy…" mới vào danh sách 404 nếu cần); XML doc + `ProducesResponseType` đầy đủ để Swagger hiện đúng, nhóm chủ có tag riêng.

## Test

Thêm test tích hợp (theo kiểu `DesktopApiTests`): login chủ đúng/sai/khách thường/nhân viên/chủ chưa duyệt/khóa; thu hồi token khi khóa hoặc đổi stamp; token chéo vai trò 403; cookie không Bearer 401; **IDOR cho từng nhóm endpoint** (dùng id của chủ khác); mỗi thao tác ghi có ít nhất một ca thành công và một ca lỗi; upload file hợp lệ/sai loại/quá cỡ; file riêng tư không đọc được của chủ khác. Test Swagger cập nhật số endpoint. Toàn bộ test đợt 1 phải giữ nguyên, không sửa để cho qua.

## Tài liệu

Cập nhật `docs/api-va-backend.md` mục 8 (bảng endpoint chủ, quyền, lỗi, ví dụ curl), README mục 5, và viết báo cáo `docs/api/desktop-api-owner-report.md`: endpoint, file sửa, logic nào đã chuyển từ controller vào service, test, kết quả, điểm chưa chắc.

## Giới hạn môi trường

Sandbox không ghi được `.git`, SqlClient trên macOS trong sandbox lỗi TLS: **không commit, không dùng Docker, không tạo/sửa database**. Chạy `dotnet build HomestayBooking.sln -c Release` và test liên quan (`--filter` theo lớp test desktop, owner và service bạn sửa). Toàn bộ test, thử curl và crawl sẽ chạy trên Mac sau. Không đụng `docs/bao-cao/`. Không thêm dòng Co-Authored-By, không ghi tên công cụ AI trong file.
