# B9 – Lấp gap backend phát sinh ở bước 05

> Chạy trong **phiên Codex mới**, **sau khi bước 05d đã commit và được review**. Không chạy song song với bất kỳ bước UI nào, vì bước này sửa `Services/`.
>
> Ngày 19/10/2026, người dùng chốt làm backend cho các gap ghi lại trong bước 05: G-16, G-17, G-18, và mọi gap mới mà 05b–05d ghi vào `docs/ui/backend-gaps.md` (G-19 trở đi). Sau phần backend, nối kết quả vào đúng chỗ UI đang làm tạm.

Đọc:
- `QUY_TRINH_BTL.md` (bảng FR, mục 7.x, mục 7.10 của B8);
- `docs/ui/backend-gaps.md` (G-16 trở đi, kèm cột "Cách làm tạm" và "Trạng thái");
- `docs/prompts/b8-backend-ui-gaps.md` (cách B8 làm);
- `docs/ui/decisions.md`, các báo cáo `docs/ui/05*-report.md`;
- code liên quan trong `HomestayBooking.Web/Services/`.

Style viết như B8: primary constructor, record DTO, kiểm quyền qua `Actor` + `HomestayAccessService`/`PartnerServiceSupport`, `TimeProvider`, test theo các `*TestHost` trong `HomestayBooking.Tests/Services/`.

## Giới hạn

- **Phần 1 (backend):** chỉ sửa `Services/`, DTO trong `Models/ViewModels/` nếu DTO nằm đó, test, tài liệu.
- **Phần 2 (nối UI):** chỉ sửa controller/view/resource đúng những chỗ mà cột "Cách làm tạm" của từng gap đang làm tạm. Không làm lại màn, không đổi bố cục ngoài phần cần nối, giữ phong cách theo `DESIGN.md`.
- **Không thêm migration, không đổi schema.** Gap nào bắt buộc đổi schema thì **không làm**, ghi lý do vào báo cáo mục "Cần người dùng quyết định".
- Không sửa dữ liệu nghiệp vụ trong database bằng SQL trực tiếp.

## Việc cần làm

| # | Gap | Việc |
|---|---|---|
| 1 | **G-16** – danh mục cho trình sửa của chủ | `HomestayContentService.EditorAsync` (hoặc hàm đọc riêng, ví dụ `EditorCatalogAsync(actor)`) trả thêm danh mục **đầy đủ, đang dùng được** của tiện nghi (Id, tên, icon nếu có, cờ đang chọn) và loại phòng (Id, tên). Danh mục này **không** lọc theo homestay đang Active như `AvailabilityService.SearchAsync`. Kiểm quyền như `EditorAsync`. Đọc các hàm ghi hiện có (cập nhật tiện nghi, tạo/sửa loại phòng) để DTO khớp đúng dữ liệu chúng nhận. Test: chủ khác bị chặn; tiện nghi đang chọn được đánh dấu đúng; danh mục không bị lọc theo homestay Active. |
| 2 | **G-17** – số đơn chờ xác nhận của chủ | Thêm hàm đọc kiểm quyền theo chủ, ví dụ `OwnerBookingSummaryAsync(Actor actor)`, trả: số đơn `Deposited` đang chờ chủ xác nhận (kèm số đơn đã quá hạn xác nhận), số lượt nhận phòng hôm nay. Chủ chỉ đếm đơn thuộc homestay của mình. Đặt ở service hợp lý nhất (`OwnerScheduleService`, `BookingService` hoặc service mới cho phía chủ). Nếu tiện, thêm luôn hàm danh sách đơn của chủ để `Owner/BookingsController` thôi tự truy vấn DbContext; không bắt buộc. Test: đếm đúng, chủ khác không thấy đơn của nhau, tài khoản không phải chủ bị chặn. |
| 3 | **G-18** – thông báo của chủ không có link | Sửa nguồn tạo thông báo: `BookingService` (khoảng dòng 430–434, nhánh đặt cọc/xác nhận/hủy) và mọi chỗ `NotificationService.Add` khác trong `Services/` đang thiếu URL. Thông báo cho chủ dùng `/Owner/Bookings/Details/{bookingId}`, cho khách dùng `/Bookings/Details/{booking.Code}`, cho nhân viên dùng đúng màn của 05b nếu có. Nếu `Add` chỉ nhận một URL cho nhiều người nhận thuộc vai trò khác nhau, tách thành các lần gọi theo vai trò. **Không backfill** thông báo cũ (bảng không lưu mã đơn riêng, không đoán URL từ nội dung); ghi điều này vào báo cáo. Test: mỗi loại thông báo có URL đúng theo người nhận. |
| 4 | **G-19 trở đi** | Với mỗi gap 05b–05d ghi thêm và còn mở: làm nếu không cần đổi schema và nằm trong phạm vi service; không thì ghi lý do. |

## Phần 2 – Nối UI

- **G-16:** tab Tiện nghi của trình sửa chủ (`/Owner/Homestays/Edit/{id}`) cho chọn/bỏ tiện nghi và lưu qua hàm ghi có sẵn. Form loại phòng cho chọn danh mục; tạo loại phòng mới nếu service ghi đã hỗ trợ. Bỏ trạng thái "chưa hỗ trợ".
- **G-17:** `Owner/Dashboard` hiện số đơn chờ xác nhận (và số quá hạn nếu có) bằng `_StatCard`, link tới `/Owner/Bookings?status=Deposited`. Mục "Việc cần xử lý" hiện số tương ứng.
- **G-18:** chuông và trang `/Notifications` của chủ mở đúng màn với thông báo mới. Thông báo cũ không có URL vẫn hiển thị bình thường, không lỗi.
- **G-19 trở đi:** nối theo cột "Cách làm tạm" của từng gap.

Cập nhật `docs/ui/backend-gaps.md`: mục nào xong ghi "Xong (B9)" kèm tên method. Thêm mục 7.11 "Backend bổ sung cho giao diện (B9)" vào `QUY_TRINH_BTL.md`. Ghi quyết định vào `docs/ui/decisions.md`.

## Kiểm tra trước khi báo xong

- `dotnet build -c Release -p:UseSharedCompilation=false` không cảnh báo mới. `dotnet test HomestayBooking.sln -c Release --no-build` pass toàn bộ, báo số test trước/sau.
- Mỗi method mới có ít nhất 1 test thành công và 1 test bị chặn quyền (nếu có kiểm quyền).
- `git diff --stat` không có file trong `Migrations/`.
- Chạy app riêng cổng 5012 (không tắt app cổng 5011; dừng app của mình bằng đúng PID; không `pkill -f`/`killall` mẫu ngắn; SQL Server ở container `sql-homestay`).
  - Đăng nhập chủ `0912000001` (mật khẩu `Fuji@2026`) trên 375px: sửa tiện nghi một homestay, xem số đơn chờ trên dashboard.
  - Tạo một thông báo mới qua luồng thật (ví dụ khách `0987000001` đặt và đặt cọc qua `SimulateBank`), mở thông báo từ chuông của chủ thì đến đúng đơn.
- `python3 -I tools/ui-crawl.py http://localhost:5012`: không có trang 500.
- **Giữ đơn demo `FJ261019-001`.** Dữ liệu thử mới ghi vào báo cáo.

## Commit

1. `feat(backend): danh mục trình sửa, tổng hợp đơn của chủ và link thông báo (B9)` (backend + test)
2. `feat(ui): nối tiện nghi, số đơn chờ và link thông báo của chủ (B9)` (phần nối UI)

Thêm commit riêng cho G-19+ nếu có.

## Báo cáo rồi dừng

Viết `docs/ui/b9-backend-report.md`:
- method mới, test mới, số test trước/sau;
- chỗ UI đã nối;
- gap chưa làm và lý do;
- mục "Cần người dùng quyết định".
