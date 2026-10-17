# B8 – Lấp gap backend cho giao diện

> Dán phần dưới cho Codex **trước bước UI `02-component`**. Bước `01-nen` không phụ thuộc B8.

---

Đọc `QUY_TRINH_BTL.md` (bảng FR, mục 7.x), `docs/ui/backend-gaps.md`, `fuji_homestay_database_v1.md`, và code trong `HomestayBooking.Web/Services/` liên quan từng mục dưới đây.

Bước này **chỉ sửa backend**: `Services/`, `Models/ViewModels/` (nếu DTO nằm đó), test, tài liệu. **Không sửa view, CSS, JS.** Không thêm migration hay đổi schema. Mọi mục đều đọc từ bảng đã có. Viết theo đúng style hiện có: primary constructor, record DTO, kiểm quyền qua `Actor` + `HomestayAccessService`/`PartnerServiceSupport`, `TimeProvider`, test theo các `*TestHost` trong `HomestayBooking.Tests/Services/`.

## Quyết định đã chốt (17/10)

- **FR-27 chỉ Quản lý/Quản trị viên duyệt nội dung homestay.** Giữ nguyên kiểm quyền hiện tại của `HomestayReviewService`. Sửa cột tác nhân FR-27 trong `QUY_TRINH_BTL.md` thành "Quản lý, Hệ thống".
- **Màn hợp đồng nằm ở khu Admin** (policy `Management`). URL trong thông báo hợp đồng đổi thành `/Admin/Contracts/{id}`.
- **FR-08 của Tư vấn dùng bảng phòng trống** (`AvailabilityService.StaffAvailabilityAsync`). Lịch tháng `RoomBlockService.CalendarAsync` giữ nguyên quyền (Chủ + Quản lý/Quản trị). Không làm G-05.

## Việc cần làm (theo thứ tự ưu tiên)

| # | Gap | Việc |
|---|---|---|
| 1 | **G-08 (lỗi)** | `OverbookingService` dòng ~73, 108, 123: link thông báo đổi từ `/Bookings/Details/{id}` (Id số) sang `/Bookings/Details/{booking.Code}` (và `replacement.Code`). Viết test khẳng định `Notification.Url` chứa mã đơn. Grep toàn `Services/` các link `/Bookings/Details/` khác, sửa nếu cũng dùng Id. |
| 2 | **G-01** | Thêm vào `NotificationService`: `ListAsync(string userId, int page, int pageSize)` trả `PaginatedList` mới nhất trước; `UnreadCountAsync(string userId)`; `LatestAsync(string userId, int take)`; `MarkReadAsync(int id, string userId)` (chỉ chủ của thông báo, sai chủ thì không đổi gì hoặc ném lỗi theo style hiện có); `MarkAllReadAsync(string userId)`. Giữ nguyên `Add`. Test: phân trang, đếm chưa đọc, không đánh dấu được thông báo của người khác. |
| 3 | **G-02** | `HomestayContentService`: `OwnerHomestaysAsync(Actor actor)` trả danh sách homestay của chủ (Id, tên, khu vực, trạng thái, ảnh bìa, số phòng); `EditorAsync(int homestayId, Actor actor)` trả DTO cho form sửa: thông tin + mô tả, loại phòng + phòng, giá hiện hành và giá tương lai theo `EffectiveFrom` (3 mức), ảnh (thứ tự, bìa), tiện nghi đang chọn, chính sách hủy đang áp dụng, giấy tờ. Kiểm quyền bằng `HomestayAccessService.AuthorizeAsync(..., readOnly: true)`. Quản lý/Quản trị đọc được để dùng ở màn duyệt. Test: chủ khác bị chặn, giá tương lai tách khỏi giá hiện hành. |
| 4 | **G-13b** | `HomestayReviewService.PendingAsync` (hoặc thêm `PendingDetailsAsync(id, actor)`) trả nội dung đủ cho trang duyệt: dùng lại DTO của `EditorAsync`, kèm lý do vào hàng chờ (đổi địa chỉ/giấy tờ theo quyết định 16/10). Quyền giữ nguyên Quản lý/Quản trị. |
| 5 | **G-11** | `ContractService.ListAsync(ContractQuery query, Actor actor)` (lọc trạng thái, chủ/homestay, sắp hết hạn trong N ngày; trả `PaginatedList`) và `DetailsAsync(int id, Actor actor)` (homestay, chủ, hoa hồng, ngày bắt đầu/kết thúc, trạng thái, lịch sử gia hạn/chấm dứt nếu có). Quyền: `Management`. Đổi URL thông báo sang `/Admin/Contracts/{id}`. Test: lọc sắp hết hạn, URL mới. |
| 6 | **G-14** | `BookingChangeRequestService.PendingCountAsync()` (đếm yêu cầu `Pending`) để View Component sidebar dùng thay cho `_DashboardLayout` tự đếm. |
| 7 | **G-04** | `StaffBookingService.AssigneesAsync(Actor actor)` trả nhân viên đang làm việc có chức vụ nhận đơn được (đúng điều kiện `AssignAsync` đang kiểm). Tư vấn gọi được. |
| 8 | **G-03** | Overload `OwnerScheduleService.ReadAsync(Actor actor)` tự suy `ownerId` từ tài khoản chủ. Giữ hàm cũ cho job. |
| 9 | **G-06** | (a) Trả kèm danh mục lọc cho trang tìm kiếm: loại phòng, tiện nghi đang dùng, theo cách `AvailabilityService.SearchAsync` hoặc một hàm `SearchFiltersAsync()`. (b) Thêm `Rating`, `ReviewCount` vào `HomestayCard` từ `ReviewRatings.Summaries`, nạp một lần cho cả trang (không N+1). Sửa mọi chỗ tạo `HomestayCard`. |
| 10 | **G-09** | Cờ `HasReview` (hoặc `ReviewService.ForBookingAsync(bookingId)`) để chi tiết đơn biết đã đánh giá chưa. Nối vào `BookingDetailsViewModel` ở controller hiện có, chỉ thêm thuộc tính, không đổi view. |
| 11 | **G-12** | Thêm `Department` (tùy chọn) vào `EmployeeAccountInput` và `AccountSummary`, lưu vào `Employees.Department`. Sửa mọi chỗ gọi/test. |
| 12 | **G-07** (nếu kịp) | `AvailabilityService.BlockedDatesAsync(int roomTypeId, DateOnly from, DateOnly to)` trả các đêm không còn phòng, dùng công khai cho Flatpickr. Tối đa 92 đêm mỗi lần gọi. |

G-10 không cần sửa backend: prompt UI `05c` đã đổi sang `GeneratePeriodAsync`.

## Tài liệu

- `docs/ui/backend-gaps.md`: thêm cột "Trạng thái". Mục nào xong ghi "Xong (B8)" kèm tên method.
- `QUY_TRINH_BTL.md`: thêm mục 7.10 "Backend bổ sung cho giao diện (B8)", liệt kê method mới, quyết định đã chốt ở trên, sửa tác nhân FR-27, bổ sung cột Service/Test của các FR bị ảnh hưởng.

## Kiểm tra trước khi báo xong

- `dotnet build` không có cảnh báo mới. `dotnet test HomestayBooking.sln` pass toàn bộ. Báo số test trước và sau.
- Mỗi method mới có ít nhất 1 test thành công + 1 test bị chặn quyền (nếu có kiểm quyền).
- `git diff --stat` không có file trong `Views/`, `Areas/*/Views/`, `wwwroot/`, `Migrations/`.

## Commit

- `fix(booking): link thông báo trùng phòng dùng mã đơn`
- `feat(ui-backend): đọc thông báo, dữ liệu sửa homestay, danh sách hợp đồng và bộ lọc tìm kiếm (B8)`

Báo cáo: method mới, test mới, số test trước/sau, gap nào chưa làm kèm lý do. Rồi dừng.
