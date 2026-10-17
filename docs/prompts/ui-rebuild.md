# Prompt: Xây lại giao diện Fuji Homestay (Razor + Bootstrap 5)

> Dán toàn bộ phần dưới đường kẻ cho Codex (hoặc Claude Code). Chạy **từng giai đoạn một**: mỗi lần chỉ giao một giai đoạn (`Giai đoạn N`), review kết quả rồi mới giao giai đoạn tiếp theo.
> Chỉ bắt đầu khi backend đã xong (thứ tự đã chốt: DB → backend → giao diện). Từ bước `02` cần B8 (`docs/prompts/b8-backend-ui-gaps.md`) đã xong.

---

## Vai trò và mục tiêu

Bạn là front-end engineer cho dự án ASP.NET Core MVC (.NET 10) `HomestayBooking`, thương hiệu **Fuji Homestay**. Nhiệm vụ là **xây lại toàn bộ tầng giao diện từ đầu** (layout, component, view, CSS, JS), để có:

1. **Một thư viện component dùng lại được**: mỗi mẫu UI chỉ viết một lần, view chỉ ghép component lại.
2. **Giao diện thực tế cho đề tài**: chạy được với dữ liệu seed, đủ các luồng của 5 nhóm người dùng (Khách, Nhân viên, Chủ homestay, Kế toán, Quản lý/Quản trị), responsive, có sáng/tối, có hai ngôn ngữ vi/en.
3. **Không phá nghiệp vụ**: backend, database và test hiện có giữ nguyên.

"Từ đầu" nghĩa là viết lại cấu trúc view, CSS và component. **Không** đổi nhận diện thương hiệu: màu, font, bo góc trong `DESIGN.md` và `wwwroot/css/tokens.css` được giữ nguyên.

## Đọc trước khi làm (bắt buộc)

- `DESIGN.md`: luật thiết kế. `tokens.css` là nguồn chuẩn của token.
- `QUY_TRINH_BTL.md` mục bảng truy vết FR-01…FR-27 và mục 7.x: biết service nào đã có, màn nào còn thiếu.
- `fuij_homestay_login_design_v5.md`: luồng đăng nhập/đăng ký (khách và chủ đăng nhập bằng SĐT + mật khẩu, nhân viên bằng mã nhân viên).
- Code hiện có: `Views/`, `Areas/*/Views/`, `Models/ViewModels/`, `Extensions/*StatusExtensions.cs`, `Models/ViewModels/NavigationModels.cs` (`Workspaces`, `SidebarLink`), `wwwroot/js/site.js`, `wwwroot/css/*.css`, `Resources/SharedResource.en.resx`.
- `docs/design/airbnb-reference.md`: **chỉ** tham khảo cách bố trí thanh tìm kiếm, thẻ homestay, trang chi tiết và bộ chọn ngày. Không lấy màu, font hay tên thương hiệu từ file này.

## Skill dùng theo giai đoạn

| Việc | Skill |
|---|---|
| Lên phương án trước mỗi giai đoạn | `superpowers:brainstorming` (ngắn), `ui-ux-pro-max` (tra quy tắc UX, dùng stack `laravel` làm gần nhất với Razor) |
| Trang khách | `design-taste-frontend` + `DESIGN.md` (nếu chạy Codex: có thể dùng `image-to-code`, nhưng ảnh thiết kế phải theo `DESIGN.md`) |
| Dashboard nội bộ | `minimalist-ui` |
| Biểu đồ | `dataviz` (vẽ bằng Chart.js) |
| Chuyển động | `ui-animation`, theo các mức thời lượng đã có trong `motion.css` |
| Chống code bỏ dở | `full-output-enforcement` |
| Kiểm tra | `playwright-cli` (chạy app thật, chụp màn hình), `web-design-guidelines` (rà accessibility/UX), `superpowers:verification-before-completion` |

**Không dùng:** `gpt-taste`, `industrial-brutalist-ui`, `high-end-visual-design`, các skill `vercel-react-*`, `remotion-*`. Chúng đưa vào GSAP/React hoặc phong cách lệch `DESIGN.md`.

## Ràng buộc cứng

**Stack:** Razor Views + Tag Helpers, Bootstrap 5.3, Bootstrap Icons, jQuery + jQuery Validation Unobtrusive, Flatpickr, Chart.js.
- Không thêm Tailwind, React, Vue, npm build hay bundler.
- Không thêm thư viện JS/CSS mới khi chưa ghi lý do vào `docs/ui/decisions.md`.

**Được sửa:**
- `Views/**`, `Areas/*/Views/**`, `wwwroot/css/**`, `wwwroot/js/**`, `wwwroot/images/**`.
- `Resources/*.resx`.
- Thư mục mới `TagHelpers/`, `ViewComponents/`.
- `Models/ViewModels/**`: chỉ thêm thuộc tính hoặc record để hiển thị.
- Controller: chỉ để nối màn với **service đã có** (giai đoạn 5).

**Không được sửa:** `Services/**`, `Data/**`, `Models/Entities/**`, migration, `database/**`, logic phân quyền. Nếu một màn cần dữ liệu mà service chưa cung cấp thì **dừng và ghi vào `docs/ui/backend-gaps.md`**, không tự viết truy vấn nghiệp vụ trong controller hay view.

**Giữ nguyên hợp đồng giữa view và controller:**
- Tên trường form (`asp-for`, `name`), `asp-action`, `asp-controller`, `asp-area`, `asp-route-*`, phương thức HTTP.
- `@Html.AntiForgeryToken()` (hoặc form tag helper), `_ValidationScriptsPartial`.
- Kiểu `@model` của từng view.
- Class `.tone-*` mà `*StatusExtensions.cs` trả về.
- Thuộc tính `data-*` mà `site.js` đang bắt: `data-confirm`, `data-loading-text`, `data-toggle-password`, `data-theme-toggle`, `data-sidebar-collapse`, `data-lightbox-*`.
- Script chống nháy theme trong `_HeadAssets`.

Có thể đổi tên hoặc gộp partial, nhưng phải sửa **mọi** chỗ gọi trong cùng giai đoạn.

**Chữ hiển thị:**
- Luôn qua `L["Khóa tiếng Việt"]` (`IStringLocalizer<SharedResource>`). Thêm bản dịch tiếng Anh vào `SharedResource.en.resx`.
- Không viết cứng chuỗi tiếng Anh trong view.

**Ảnh:**
- Ảnh homestay luôn lấy từ dữ liệu, qua `ImageUrlExtensions`. Không chèn ảnh stock vào thẻ homestay.
- Ảnh trang trí (banner trang chủ, trang đăng nhập) phải thuộc CC0 hoặc public domain, lưu ở `wwwroot/images/`, ghi nguồn vào `docs/ui/credits.md`. Tối đa 4 ảnh.

**Accessibility:**
- Tương phản ≥ 4.5:1 ở cả sáng lẫn tối.
- Focus hiện rõ bằng `--fx-focus`.
- Mọi input có `<label>`, nút chỉ có icon thì có `aria-label`.
- Vùng bấm ≥ 44px trên điện thoại.
- Tôn trọng `prefers-reduced-motion`.

**Responsive:**
- Kiểm tra ở 375px, 768px, 1280px.
- Giao diện chủ homestay ưu tiên điện thoại: khóa ngày phòng tối đa 3 thao tác.
- Trên điện thoại, bảng dữ liệu chuyển sang dạng danh sách thẻ.

## Kiến trúc component (mục tiêu của giai đoạn 1–2)

### Chọn cơ chế theo loại component

| Loại | Dùng khi | Ví dụ |
|---|---|---|
| **Tag Helper** (`TagHelpers/`) | Phần tử nhỏ, không có logic dữ liệu, lặp lại rất nhiều | `<status-badge>`, `<money>`, `<date-range>`, `<rating>`, `<icon>` |
| **Partial có model kiểu rõ** (`Views/Shared/Components/_X.cshtml` + record trong `Models/ViewModels/Ui/`) | Khối giao diện ghép từ dữ liệu view đã có | thẻ homestay, ô số liệu, bảng giá, timeline, empty state, pager |
| **View Component** (`ViewComponents/`) | Khối tự lấy dữ liệu qua service đã có, xuất hiện trên nhiều trang | sidebar có số đếm, chuông thông báo, chuyển khu làm việc |

Quy tắc:
- Không viết partial nhận `dynamic` hoặc `ViewData` tùy tiện. Mỗi component có một record model, mô tả tham số trong XML doc.
- Component không tự gọi `DbContext`. View Component chỉ gọi service có sẵn.

### Danh mục component cần có

**Nền:** `_Layout` (trang khách), `_DashboardLayout` (nội bộ), `_AuthLayout` (đăng nhập/đăng ký/lỗi), `_HeadAssets`.

**Điều hướng:**
- Header công khai, footer.
- Sidebar dùng `SidebarLink`, có số đếm, thu gọn được, thành ngăn kéo khi màn < 992px.
- Topbar dùng `Workspaces.For(User)` để chuyển khu làm việc.
- Breadcrumb, tabs (tổng quát hóa từ `_AccountingTabs`), `_LanguageSwitch`, `_ThemeToggle`, `_LoginPartial`.

**Hiển thị dữ liệu:**
- `status-badge`: nhận enum, lấy tone từ `*StatusExtensions`.
- `money`: VND, `vi-VN`, tabular-nums.
- `date-range`: theo đêm, kèm số đêm.
- `rating`: màu chữ `ink`, không dùng sao vàng.
- Ô số liệu (stat card).
- Bảng dữ liệu: đầu bảng, sắp xếp, cột số căn phải, chuyển sang thẻ trên điện thoại, đi kèm `_RowActions` và `_Pager`.
- Danh sách mô tả (key–value).
- Timeline (kế thừa `_ChangeHistory`).
- Bảng giá và hoàn tiền (kế thừa `_ChangeQuote`, `_PolicyRules`).
- `_EmptyState`, skeleton khi tải.

**Form:**
- `form-field`: label + input + gợi ý + lỗi validation trong một dòng gọi.
- Thanh lọc (filter bar) đồng bộ với query string.
- Bộ chọn khoảng ngày (Flatpickr, theo đêm, chặn ngày quá khứ).
- Chọn số khách.
- Vùng tải file: ảnh homestay, giấy tờ, ảnh bill. Hiện preview, kiểm tra jpg/png/webp và ≤ 5 MB phía client. Server vẫn là nơi kiểm tra chính.
- Hộp xác nhận (`data-confirm`), nút đang xử lý (`data-loading-text`).

**Đặt phòng:**
- `_SearchBar` dạng viên thuốc: Khu vực | Ngày | Khách | nút tìm.
- Thẻ homestay.
- Gallery + lightbox.
- Ô báo giá dính bên phải (sticky).
- Stepper tiến trình đơn (chỉ để đọc).
- Khối thanh toán QR, luôn trên nền trắng.
- Lịch tháng của phòng: trống / đã đặt / bảo trì / khóa. Dùng cho chủ homestay (FR-14) và Quản lý/Quản trị. Tư vấn xem phòng trống bằng bảng của `StaffAvailabilityAsync` (quyết định 17/10).

**Phản hồi:** `_Alerts` (TempData), toast, trang lỗi/`StatusCode`, `AccessDenied`.

### CSS

Tách lại `wwwroot/css/`:
- `tokens.css`: giữ nguyên tên biến, chỉ thêm.
- `base.css`: reset, chữ, ánh xạ Bootstrap.
- `components.css`: mỗi component một khối, prefix `fx-`, đặt tên kiểu BEM nhẹ (`fx-card`, `fx-card__media`, `fx-card--compact`).
- `layout-public.css`, `layout-dashboard.css`.
- `motion.css`.

Quy tắc:
- Không dùng `style=""` trong view, không viết mã hex ngoài `tokens.css`.
- Dùng utility của Bootstrap trước khi viết class mới.
- Xóa hẳn CSS cũ không còn dùng. Không để lại file chết.

### JS

`site.js` giữ mô hình "hành vi qua `data-*`". Tách thành `wwwroot/js/components/*.js` khi một hành vi dài quá khoảng 40 dòng, nạp qua `@section Scripts` của trang cần dùng.

Không dùng inline `onclick`. Mọi request AJAX gửi kèm header `RequestVerificationToken`.

### Trang UI Kit

Tạo `Dev/UiKit` (thêm action vào `DevController`, chỉ chạy ở Development, giống `SimulateBank`). Trang hiển thị **mọi component với mọi biến thể và trạng thái**: rỗng, lỗi, loading, dài quá, sáng/tối. Đây là nơi review và chụp ảnh so sánh.

## Các giai đoạn

Mỗi giai đoạn kết thúc bằng:
1. `dotnet build` và `dotnet test` đều pass.
2. Ảnh chụp bằng `playwright-cli` cho các trang bị ảnh hưởng, ở 375/768/1280px × sáng/tối. Lưu vào `.playwright-cli/` (đã gitignore).
3. Một commit Conventional Commits tiếng Việt (ví dụ `feat(ui): ...`).
4. Báo cáo ngắn: đã làm gì, chỗ nào lệch `DESIGN.md`, việc còn treo.

### Giai đoạn 0 – Kiểm kê (không sửa code)

Tạo `docs/ui/inventory.md`, ghi:
- Mỗi view: route, area, role/policy, `@model`, partial dùng, form + tên trường, hook `data-*`, FR liên quan.
- Những mẫu UI đang lặp lại (ứng viên thành component).
- Màn còn thiếu so với FR-01…FR-27, đối chiếu cột "thiếu" trong `QUY_TRINH_BTL.md`.
- Service nào đã có cho màn đó.

Ghi `docs/ui/backend-gaps.md` cho những màn chưa có service. Backend đã đủ FR-01…FR-27 sau B7, gồm: đặt phòng không đăng nhập (FR-03), đặt hộ theo SĐT (FR-07), truy vấn hồ sơ thẩm định (FR-26). Chỉ còn Zalo/SMS thật là phần rút gọn đã được chấp thuận, giao diện chỉ hiển thị trạng thái `MessageLog`. Nếu file này rỗng thì ghi rõ "không có".

### Giai đoạn 1 – Nền

Làm các phần sau:
- Cấu trúc CSS mới.
- 3 layout.
- Header, footer, sidebar, topbar.
- Tag Helper nền: `status-badge`, `money`, `date-range`, `icon`.
- `form-field`.
- Trang `Dev/UiKit`.

Chuyển `_Layout` và `_DashboardLayout` sang hệ mới, nhưng **mọi view cũ vẫn chạy**.

### Giai đoạn 2 – Thư viện component

Làm đủ danh mục ở trên. Mỗi component có:
- Record model.
- Mục trên UI Kit.
- Test cho Tag Helper có logic (format tiền, tone trạng thái). Đặt trong `HomestayBooking.Tests/TagHelpers/`, theo style test hiện có.

### Giai đoạn 3 – Trang khách

Dùng `design-taste-frontend` cho các trang:
- `Home/Index`, `Homestays/Index`, `Homestays/Details`.
- `Bookings/Create`, `Lookup`, `Details`, `Mine`, `Change`.
- `Account/Login`, `Register`, `EmployeeLogin`, `ChangePassword`, `AccessDenied`.
- `Home/StatusCode`, `Shared/Error`.

Luồng mẫu phải đi trọn được: tìm phòng → xem chi tiết → đặt → thanh toán QR → tra cứu đơn → gửi yêu cầu đổi/hủy.

### Giai đoạn 4 – Màn nội bộ đã có

Dùng `minimalist-ui` cho các màn trong `Areas/Admin`, `Areas/Staff`, `Areas/Owner`, `Areas/Accounting`. Các view chỉ ghép component, không còn HTML/CSS riêng lặp lại. Dashboard mỗi khu có ô số liệu và danh sách việc cần xử lý.

### Giai đoạn 5 – Màn còn thiếu (chỉ nối service đã có)

Theo thứ tự ưu tiên:
1. Chủ homestay: nội dung, phòng, ảnh, giá, chính sách (FR-13); lịch phòng và khóa ngày (FR-14); lịch nhận–trả và bảng đối soát (FR-16); trả lời đánh giá (FR-06).
2. Nhân viên: tìm, giao và đổi trạng thái đơn (FR-09); phòng trống (FR-08); khách hàng (FR-12); trùng phòng và phương án thay thế (FR-10); duyệt nội dung homestay (FR-27).
3. Kế toán: bảng đối soát theo kỳ, xác nhận chuyển tiền, khoản bù (FR-18).
4. Quản lý/Quản trị: đối tác, hồ sơ thẩm định và hợp đồng (FR-19, FR-25, FR-26); nhân viên đặt hộ theo SĐT (FR-07); báo cáo bằng Chart.js (FR-20); tài khoản nhân viên và xóa danh mục an toàn (FR-21).
5. Khách: viết đánh giá sau khi trả phòng (FR-06).

Controller mới mỏng: gắn `[Authorize]` đúng policy trong `AppPolicies`, gọi service, map ra ViewModel, dùng mẫu PRG + TempData cho `_Alerts`. Màn nào service còn thiếu thì bỏ qua và ghi vào `backend-gaps.md`.

### Giai đoạn 6 – Kiểm tra toàn bộ

- Dùng `playwright-cli` đi qua luồng chính của từng vai trò. App chạy `dotnet run --project HomestayBooking.Web`, http://localhost:5011, môi trường Development có seed. Mật khẩu chung của seed là `Fuji@2026`:
  - Khách: SĐT `0987000001`
  - Chủ homestay: SĐT `0912000001`
  - Nhân viên: mã `NV001`
  - Kế toán: mã `KT001`
  - Quản lý: mã `QL001`
  - Quản trị: mã `AD001`
- Chạy `web-design-guidelines` trên `Views/**/*.cshtml` và `Areas/**/*.cshtml`. Sửa hết lỗi mức cao.
- Ghi `docs/ui/qa-report.md`: bảng route × vai trò × kết quả, link ảnh chụp, lỗi còn lại.

## Định nghĩa "xong"

- Không còn view nào tự viết lại một mẫu UI đã có component.
- `grep` không ra `style="` hay mã hex mới trong view.
- Mọi trang đạt ở 3 kích thước × 2 chế độ màu × 2 ngôn ngữ.
- Không bỏ sót route nào trong `inventory.md`. Mỗi route có trạng thái: làm mới / giữ / thiếu backend.
- `dotnet build` không có cảnh báo mới. `dotnet test` pass toàn bộ.
- Không đổi file nào trong `Services/`, `Data/`, `Models/Entities/`, `database/`.

## Khi gặp mơ hồ

Không tự chế nghiệp vụ. Hỏi lại, hoặc chọn phương án ít thay đổi nhất và ghi vào `docs/ui/decisions.md` (mỗi quyết định một dòng, kèm lý do).
