# Quyết định giao diện

Mỗi quyết định ghi một dòng: ngày, bước, nội dung, lý do. Dùng cho các chỗ `ui-rebuild.md` để ngỏ, các chỗ lệch `DESIGN.md`, cách làm tạm cho gap trong `backend-gaps.md`, hoặc khi thêm thư viện JS/CSS.

| Ngày | Bước | Quyết định | Lý do |
|---|---|---|---|
| 17/10/2026 | 01 | Tách CSS: `site.css` → `base.css` (nền, utility ghi đè, reduced-motion) + `components.css`; `public.css` → `layout-public.css`; `dashboard.css` + `accounting.css` → `layout-dashboard.css` (phần kế toán đặt cuối file) | Giữ đúng thứ tự cascade cũ (accounting.css từng nạp sau dashboard.css, chỉ ở dashboard); tránh vỡ giao diện khi chưa viết lại view |
| 17/10/2026 | 01 | Chỉ xóa 5 class chết ở `public.css`; giữ `form-select-sm`, `text-body-secondary` | Hai class này là ghi đè lên utility của Bootstrap, view mới có thể dùng; không phải CSS chết của dự án |
| 17/10/2026 | 01 | Class không có tiền tố `fx-` trong `components.css`/`layout-*.css` đều là class cũ, còn 57 mã hex nằm trong chúng; bước 02–04 thay bằng component mới rồi xóa | Không đánh dấu `/* legacy */` từng class vì hầu hết sẽ bị thay; ghi một lần ở đầu file |
| 17/10/2026 | 01 | Đổi tên `_SearchBar` cũ (ô tìm theo tên ở 3 trang danh mục Admin) thành `_CatalogSearch`; để dành `_SearchBar` cho thanh tìm phòng dạng viên thuốc | Tránh trùng tên với component của bước 02 (đã ghi ở inventory mục 1.6) |
| 17/10/2026 | 01 | Chưa đặt `_SearchBar` compact vào header trang khách | Component thanh tìm phòng làm ở bước 02; gắn vào header ở bước 03 |
| 17/10/2026 | 01 | Sidebar là View Component `Sidebar`, số đếm qua `BookingChangeRequestService.PendingCountAsync` và `AccountingService.PendingAdditionalDeposits`; bỏ `@inject ApplicationDbContext` khỏi `_DashboardLayout` | Layout không tự truy vấn; giữ nguyên markup và quyền hiện link (test `SidebarShowsOnlyPermittedChangeActions`) |
| 17/10/2026 | 01 | `WorkspaceSwitcher` chỉ dùng ở header trang khách | Sidebar dashboard đã liệt kê mọi khu làm việc theo nhóm |
| 17/10/2026 | 01 | Đăng nhập, đăng ký, đổi mật khẩu, AccessDenied, StatusCode, Error dùng `_AuthLayout` | Khung gọn theo `ui-rebuild.md`; các view vốn đã tự dựng thẻ 2 cột |
| 17/10/2026 | 01 | `<money>` luôn định dạng VND theo vi-VN (`1.500.000đ`) kể cả khi giao diện tiếng Anh | Theo `ui-rebuild.md`; đơn vị là đồng, dấu nhóm kiểu Việt Nam tránh đọc nhầm |
| 17/10/2026 | 01 | Chữ mô tả trên `Dev/UiKit` không đưa vào resource; trang dùng `_DashboardLayout` | Trang công cụ cho nhóm phát triển như `SimulateBank`; component bên trong vẫn tự dịch |
