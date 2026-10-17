# Bước 1 – Nền giao diện

Đọc `docs/prompts/ui-rebuild.md` (luật chung, mục "Kiến trúc component"), `DESIGN.md`, `docs/ui/inventory.md`, `docs/ui/decisions.md`.

Skill: `superpowers:brainstorming` (ngắn, chốt cấu trúc trước khi code), `minimalist-ui` cho dashboard, `design-taste-frontend` cho header/footer công khai, `full-output-enforcement`.

## Việc cần làm

1. **CSS.** Tách `wwwroot/css/` thành `tokens.css` (giữ nguyên biến, chỉ thêm), `base.css`, `components.css`, `layout-public.css`, `layout-dashboard.css`, `motion.css`.
   - Chuyển nội dung từ `site.css`, `public.css`, `dashboard.css`, `accounting.css` sang file mới. Class nào inventory ghi là chết thì xóa.
   - Class mới dùng prefix `fx-` và kiểu BEM nhẹ. Class cũ mà view cũ còn dùng thì giữ, đánh dấu `/* legacy: xóa ở bước 04 */`.
   - Cập nhật `_HeadAssets.cshtml` để nạp file mới. Giữ nguyên script chống nháy theme.
2. **Layout.**
   - `_Layout` (khách): header (logo, điều hướng, `_SearchBar` thu gọn ở trang không phải trang chủ, `_LanguageSwitch`, `_ThemeToggle`, `_LoginPartial`), footer.
   - `_DashboardLayout` (nội bộ): sidebar + topbar.
   - `_AuthLayout` (mới): đăng nhập, đăng ký, lỗi. Bố cục 2 cột (ảnh | form), trên điện thoại còn 1 cột.
3. **Điều hướng.**
   - View Component `Sidebar`: dựng danh sách `SidebarLink` theo area và quyền. Số đếm lấy từ service đã có, nếu không có thì để `null`.
   - View Component `WorkspaceSwitcher`: dùng `Workspaces.For(User)`.
   - Sidebar thu gọn được (`data-sidebar-collapse`), thành ngăn kéo khi < 992px, đánh dấu mục đang mở bằng `aria-current="page"`.
4. **Tag Helper nền** trong `TagHelpers/`, đăng ký trong các `_ViewImports.cshtml`:
   - `<status-badge value="@Model.Status" />`: lấy tone và nhãn từ `*StatusExtensions` hiện có. Không chép logic.
   - `<money value="@x" />`: định dạng VND theo `vi-VN`, class tabular-nums.
   - `<date-range from="" to="" />`: `dd/MM/yyyy – dd/MM/yyyy · N đêm`.
   - `<icon name="calendar" />`: in `<i class="bi bi-calendar" aria-hidden="true">`.
5. **`form-field`** (Tag Helper hoặc partial `_FormField`): một lời gọi in ra label + input/select/textarea + gợi ý + `asp-validation-for`. Hỗ trợ `asp-for`, kiểu input, placeholder, gợi ý, bắt buộc.
6. **UI Kit.** Thêm action `UiKit` vào `DevController` (cùng điều kiện Development như `SimulateBank`). View `Views/Dev/UiKit.cshtml` hiển thị mọi thứ đã làm ở bước này, gom theo nhóm, có mục lục.
7. **Test** trong `HomestayBooking.Tests/TagHelpers/`: `money` (0, số lẻ nghìn, số âm), `status-badge` (mỗi enum ra đúng tone), `date-range` (1 đêm, nhiều đêm).

## Không làm

Không viết lại nội dung từng trang. View cũ chỉ cần chạy được trong layout mới.

## Kết quả bàn giao

- Commit: `feat(ui): nền giao diện – CSS, layout, điều hướng, tag helper và UI Kit`.
- Ảnh `playwright-cli`: `/`, `/Homestays`, `/Account/Login`, `/Admin/Dashboard`, `/Dev/UiKit` ở 375/768/1280 × sáng/tối.

## Kiểm tra trước khi báo xong

- `dotnet build` không có cảnh báo mới, `dotnet test` pass.
- Mở hết route trong `inventory.md` với tài khoản phù hợp: không trang nào lỗi 500 hoặc vỡ layout.
- `grep -rn 'style="' Views Areas` không tăng so với trước.

## Báo cáo rồi dừng

Báo cáo cấu trúc file CSS mới, danh sách class legacy còn lại, quyết định đã ghi vào `decisions.md`. **Không bắt đầu bước 2.**
