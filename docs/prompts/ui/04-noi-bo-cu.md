# Bước 4 – Chuyển các màn nội bộ đang có

Đọc `docs/prompts/ui-rebuild.md`, `DESIGN.md` (mục dashboard), `docs/ui/inventory.md`, `docs/ui/decisions.md`. Xem `/Dev/UiKit`.

Skill: `minimalist-ui`, `dataviz` (nếu dashboard có biểu đồ), `full-output-enforcement`.

## Phạm vi: chỉ các view đang có

- **Admin:** `Dashboard`, `Amenities` (Index/Form), `Areas` (Index/Form), `Holidays` (Index/Form), `RoomCategories` (Index/Form), `Vouchers` (Index/Create).
- **Staff:** `Dashboard`, `ChangeRequests/Index`, `ChangeApprovals/Index`.
- **Owner:** `Dashboard`, `Bookings/Index`, `Bookings/Details`.
- **Accounting:** `Dashboard`, `Transactions` (Index/Process), `Receipts/Index`, `Refunds/Index`, `AdditionalDeposits/Index`.
- **Dev:** `SimulateBank`.

## Mẫu chung

- **Trang danh sách:** `_PageHead` (tiêu đề + nút chính) → `_FilterBar` → `_DataTable` hoặc danh sách thẻ trên điện thoại → `_Pager`. Có `_EmptyState`.
- **Trang form:** `_PageHead` + `_Breadcrumb` → một cột tối đa ~40rem, `form-field` → nút Lưu/Hủy cố định ở cuối. Có `data-loading-text`.
- **Xóa danh mục:** gọi qua `CatalogDeletionService` nếu controller đã dùng. Thông báo khi không xóa được (đang được dùng) hiện qua `_Alerts`. Nút xóa dùng hộp xác nhận `danger`.
- **Dashboard mỗi khu:** hàng `_StatCard` + danh sách "Việc cần xử lý" (ví dụ đơn chờ xác nhận, yêu cầu đổi/hủy chờ duyệt, giao dịch chưa khớp…). Lấy dữ liệu controller đang truyền. Muốn thêm số liệu thì chỉ gọi service đã có.
- **Owner:** ưu tiên điện thoại. Nút xác nhận/từ chối đơn to, nằm trong tầm ngón cái. Chi tiết đơn đọc được trên 375px.
- **Accounting:** gom bằng `_Tabs` (thay `_AccountingTabs`). Số tiền dùng `<money>`, trạng thái khớp ngân hàng dùng `<status-badge>`.

Sau khi chuyển xong:
- Xóa `accounting.css`, các file CSS cũ và mọi class `/* legacy */` không còn ai dùng.
- Xóa partial cũ đã được component thay thế. Sửa mọi chỗ gọi.

## Kết quả bàn giao

- Commit theo area: `feat(ui): chuyển màn Admin sang thư viện component`, tương tự cho Staff, Owner, Accounting.
- Ảnh `playwright-cli` mỗi màn ở 1280 × sáng/tối. Riêng Owner thêm 375.

## Kiểm tra trước khi báo xong

- `dotnet build`, `dotnet test` pass.
- Đăng nhập lần lượt `AD001`, `QL001`, `NV001`, `KT001`, `0912000001` (mật khẩu `Fuji@2026`): mỗi tài khoản mở hết màn của mình. Tạo, sửa, xóa một mục danh mục; xử lý một giao dịch ở `SimulateBank` → `Transactions/Process`.
- `grep` không còn class legacy, không có `style="`, không có mã hex ngoài `tokens.css`.
- Chạy `web-design-guidelines` trên `Areas/**/*.cshtml`.

## Báo cáo rồi dừng

Báo cáo màn đã chuyển, CSS/partial đã xóa, số dòng CSS trước/sau. **Không bắt đầu bước 5.**
