# Prompt: API loại phòng theo homestay cho ô chọn đặt hộ (desktop)

Repo `/Users/phamngocphu/Downloads/LTW`, nhánh **`feature/desktop-api`** (đã có commit `b698b24` với 42 endpoint desktop). Đọc trước: `docs/prompts/desktop-api.md` (mọi ràng buộc ở đó vẫn áp dụng), `docs/api/desktop-api-report.md` mục việc còn mở số 1, `Services/StaffBookingService.cs`, `Controllers/Api/Desktop/DesktopBookingsController.cs`, `DesktopApi/DesktopDtos.cs`.

## Việc cần làm

WinForms đặt hộ cần chọn homestay rồi chọn loại phòng, nhưng `GET /api/desktop/booking-homestays` chỉ trả id/tên. Thêm đúng **một** endpoint đọc:

`GET /api/desktop/booking-homestays/{homestayId:int}/room-types` → danh sách (không phân trang) loại phòng của homestay đó: `id`, `name`, `categoryId`, `categoryName`, `maxGuests`, số phòng đang dùng được. Thêm trường khác chỉ khi WinForms cần để dựng form đặt hộ.

- Quyền: giống `/booking-homestays` (`DesktopApiRegistration.StaffPolicy`; Kế toán 403).
- Logic đọc đặt trong `StaffBookingService` (method mới, kiểm actor bằng `EmployeeAsync` như `HomestaysAsync`), controller chỉ map DTO. **Không đổi** `HomestaysAsync`, `StaffHomestayOption` hay màn web nào đang dùng chúng.
- Chỉ trả loại phòng mà `CreateForCustomerAsync` chấp nhận đặt; đọc điều kiện trong service đặt phòng/`AvailabilityService` và lọc giống hệt (homestay/loại phòng/phòng ngừng hoạt động…), không tự đặt luật mới.
- Homestay không tồn tại → 404 ProblemDetails qua `DesktopExceptionFilter` có sẵn (thêm thông báo vào danh sách `Missing` nếu dùng `BookingException`). Homestay có nhưng không loại nào đặt được → 200 với mảng rỗng.
- XML doc + `ProducesResponseType` như các action bên cạnh để Swagger hiện đúng.

## Test

Thêm vào `HomestayBooking.Tests/Integration/DesktopApiTests.cs`: Tư vấn lấy được danh sách đúng của một homestay seed; loại phòng/phòng không đặt được bị loại; homestay không tồn tại → 404; Kế toán → 403; không token → 401. Nếu test Swagger đếm số endpoint desktop thì cập nhật 42 → 43.

## Tài liệu

Cập nhật bảng endpoint desktop trong `docs/api-va-backend.md`, bỏ ghi chú "hiện chỉ id/tên homestay", và đánh dấu việc mở số 1 trong `docs/api/desktop-api-report.md` là đã xử lý.

## Kiểm tra và giới hạn môi trường

- Sandbox không ghi được `.git` và SqlClient trên macOS trong sandbox lỗi TLS: **không commit, không dùng Docker, không tạo/sửa database**. Chạy `dotnet build HomestayBooking.sln -c Release` và các test liên quan (`--filter "FullyQualifiedName~DesktopApi|FullyQualifiedName~StaffBooking"`). Toàn bộ test và thử curl sẽ chạy trên Mac sau.
- Không đụng `docs/bao-cao/`. Không thêm dòng Co-Authored-By.
- Kết thúc bằng tóm tắt ngắn: file đã sửa, kết quả build/test, điểm nào chưa chắc.
