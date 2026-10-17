# Bước 5d – Màn mới cho quản lý / quản trị viên

Đọc `docs/prompts/ui-rebuild.md`, `DESIGN.md`, `docs/prompts/ui/05a-chu-homestay.md` (**mục "Luật controller", áp dụng nguyên văn**), `docs/ui/inventory.md`, `docs/ui/backend-gaps.md`, `docs/ui/decisions.md`. Xem `/Dev/UiKit`.

Skill: `minimalist-ui`, `dataviz` (đọc trước khi vẽ biểu đồ), `full-output-enforcement`.

## Các màn (area `Admin`, policy `Management`; thao tác chỉ Quản trị viên được làm thì theo đúng quyền service yêu cầu)

1. **Đối tác và hợp đồng (FR-19):** controller `Admin/ContractsController`, route `/Admin/Contracts/{id}` khớp với URL trong thông báo (B8). Đọc bằng `ContractService.ListAsync/DetailsAsync` (B8), ghi bằng `CreateAsync/ExtendAsync/TerminateAsync` (+ `ContractEligibility`). Theo quyết định 16/10: chưa có hợp đồng tương lai, gia hạn bằng cách lùi `EndDate`.
   - Danh sách chủ/homestay đối tác: trạng thái hợp đồng, tỷ lệ hoa hồng, ngày hết hạn, cảnh báo sắp hết hạn.
   - Chi tiết hợp đồng; form tạo/gia hạn; chấm dứt cần xác nhận kèm lý do.
2. **Duyệt nội dung homestay (FR-27, chỉ Quản lý/Quản trị):** `HomestayReviewService.PendingAsync` (+ DTO nội dung đầy đủ B8) / `ApproveAsync` / `RejectAsync` / `ResumeAsync`. Hàng chờ duyệt ghi lý do vào hàng (đổi địa chỉ, giấy tờ, homestay mới). Trang duyệt so sánh nội dung (ảnh, mô tả, phòng/giá, giấy tờ), duyệt/từ chối kèm lý do.
3. **Hồ sơ đối tác (FR-25/26) phía quản lý:** nếu bước 5b đã làm màn thẩm định thì chỉ thêm lối vào từ sidebar Admin, không làm lại.
4. **Báo cáo (FR-20):** `ReportService.ManagementAsync/BookingsAsync/FinancialAsync/EmployeesAsync/HomestaysAsync`.
   - Một trang có `_Tabs`: Tổng quan / Đơn / Tài chính / Nhân viên / Homestay. Bộ lọc khoảng ngày dùng chung.
   - Hàng `_StatCard` + biểu đồ Chart.js (đường cho xu hướng theo thời gian, cột cho so sánh nhân viên/homestay) + bảng số liệu chi tiết bên dưới.
   - Màu biểu đồ lấy từ biến CSS `--fx-primary` và `tone-*`, đọc qua `getComputedStyle`, vẽ lại khi đổi sáng/tối.
   - Tỷ lệ chốt FR-20 = đơn nhân viên tạo được xác nhận / đơn nhân viên tạo. Hiện đúng như service trả về, không tự tính lại.
   - Biểu đồ có `aria-label` và bảng số liệu thay thế cho trình đọc màn hình.
5. **Tài khoản nhân viên (FR-21):** `AccountAdminService.ListAsync/CreateEmployeeAsync/ChangeEmploymentAsync/SetLockedAsync/ResetEmployeePasswordAsync`.
   - Danh sách: lọc theo chức vụ/trạng thái.
   - Form tạo: mã nhân viên, họ tên, SĐT, email, chức vụ, phòng ban (`Department`, B8).
   - Đổi chức vụ/nghỉ việc; khóa/mở khóa và đặt lại mật khẩu đều có xác nhận. Mật khẩu tạm hiện **một lần** qua `TempData`, kèm nút sao chép.
6. **Danh mục:** rà các màn `Amenities`, `Areas`, `Holidays`, `RoomCategories` đã chuyển ở bước 4. Bảo đảm thao tác xóa đi qua `CatalogDeletionService` và báo rõ khi mục đang được dùng.
7. **Dashboard Admin:** số đơn/doanh thu/hoa hồng kỳ này (từ `ReportService.ManagementAsync`), hợp đồng sắp hết hạn (`ContractService.ListAsync`), nội dung chờ duyệt, hồ sơ chờ thẩm định, lối tắt tới báo cáo.

## Kết quả bàn giao

- Commit theo màn: `feat(admin): ...`.
- Ảnh `playwright-cli` ở 1280 × sáng/tối. Riêng trang báo cáo thêm 768.

## Kiểm tra trước khi báo xong

- `dotnet build`, `dotnet test` pass.
- Đăng nhập `QL001`: xem 5 tab báo cáo với khoảng ngày có dữ liệu seed (gồm dữ liệu 2028), tạo/gia hạn hợp đồng, duyệt 1 nội dung homestay đang chờ.
- Đăng nhập `AD001`: tạo nhân viên, khóa rồi mở khóa, đặt lại mật khẩu, xóa thử một danh mục đang được dùng (phải bị chặn kèm thông báo).
- Đổi sáng/tối trên trang báo cáo: biểu đồ đổi màu đúng.

## Báo cáo rồi dừng

Báo cáo màn đã làm, service đã nối, gap đã ghi.
