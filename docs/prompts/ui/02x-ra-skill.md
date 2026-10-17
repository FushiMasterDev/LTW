# Bước 2x – Rà lại thư viện component theo skill thiết kế

> Chạy **sau khi bước 02 đã commit đủ nhóm A–D**, trong **phiên Codex mới** (để Codex nạp skill trong `.agents/skills/`). Bước 01–02 đã làm khi Codex chưa có các skill dưới đây; bước này chỉ rà và chỉnh phần nhìn, không làm thêm component. Ngoại lệ duy nhất: các lỗi ở mục "Sửa từ review bước 02".

Đọc `docs/prompts/ui-rebuild.md`, `DESIGN.md`, `docs/prompts/ui/02-component.md`, `docs/ui/decisions.md`. Mở `/Dev/UiKit`.

## Phạm vi

- **Nhóm A–C** (`0cde961`, `6f6db35`, `109bdcf`) làm khi chưa có skill: rà đủ theo bảng dưới.
- **Nhóm D** (`3b2e606`) đã đọc `design-taste-frontend`, `ui-animation`, `full-output-enforcement` và soi bằng ảnh chụp, nhưng chưa chạy `inspect.mjs` và chưa áp `minimalist-ui`. Với D chỉ làm hai việc: áp `minimalist-ui` cho `_RoomCalendar`, và chạy `inspect.mjs` cho các trang có component D.
- Đọc `docs/ui/02-component-report.md` trước để không làm lại phần đã ghi.

## Sửa từ review bước 02 (làm trước, commit riêng)

Commit `fix(ui): sửa lỗi review bước 02`, chạy `dotnet test` trước khi sang phần rà skill.

| # | Chỗ | Lỗi | Cách sửa |
|---|---|---|---|
| 1 | `wwwroot/js/site.js` (modal xác nhận) | `data-confirm` đặt trên **nút** submit không được xử lý, vì handler chỉ đọc `form[data-confirm]`. Hậu quả: `Views/Bookings/Change.cshtml:40` gửi yêu cầu đổi lịch mà không hỏi | Bắt `submit` của mọi form; thông báo lấy `submitter.dataset.confirm`, không có thì lấy `form.dataset.confirm`, cả hai rỗng thì bỏ qua. Tiêu đề và variant đọc theo cùng thứ tự. Giữ nguyên `requestSubmit(submitter)`, Esc/Hủy không gửi. Mục UI Kit thêm một mẫu `data-confirm` trên nút |
| 2 | `TagHelpers/RatingTagHelper.cs` | `aria-label` đặt trên `<span>` không có role nên trình đọc màn hình bỏ qua | Thêm `role="img"` khi có điểm. Bổ sung assert vào test rating hiện có |
| 3 | `Views/Shared/_Timeline.cshtml` | `<div class="fx-timeline">` nằm trước `@using`/`@model`. Badge viết cứng `tone-sky` | Đưa directive lên đầu file. Thay badge bằng `<status-badge value="r.Status" />`. Giữ nguyên `article data-change-request/data-status` và các chuỗi trong bảng "Ràng buộc test" |
| 4 | `Views/Shared/_PaymentQr.cshtml` | Ngân hàng chỉ hiện `MB`; trước bước 02 là "MB Bank (Ngân hàng Quân đội)" | Hiện lại tên đầy đủ (qua `L[...]`, có bản dịch EN). Không sửa `BankAccountOptions` hay appsettings |
| 5 | `Views/Bookings/Details.cshtml` | Đầu khối thanh toán (`pay-card`) còn sót dòng trống sau khi countdown chuyển vào `_PaymentQr` | Xóa dòng trống, gọn lại thẻ div chỉ còn tiêu đề |
| 6 | `Views/Shared/_Gallery.cshtml` | Trạng thái rỗng mất câu "Thông tin phòng, giá và chính sách bên dưới vẫn đầy đủ. Cần xem thêm, hãy gọi 1900 0000." | Truyền lại câu này làm `Description` của `EmptyStateModel` (khóa resx cũ còn dùng được) |
| 7 | `Views/Shared/_DataTable.cshtml` | Link sắp xếp dùng `Request.Path`, thiếu `PathBase` (khác `_FilterBar`, `_RoomCalendar`) | Dùng `Context.Request.PathBase + Context.Request.Path` |

Các lỗi nhìn dưới đây gộp vào bảng rà skill ở mục "Việc cần làm":
- `_RoomCalendar`: mọi đêm trống đều tô xanh `tone-emerald`, cả tháng xanh rực. Đêm trống để trung tính (nền surface, chữ muted, vẫn có icon/chữ "Trống"); chỉ trạng thái bận mới dùng tone.
- `_SearchBar` (cả D): chưa ra dạng viên thuốc. Các ô có viền riêng, nhãn lệch hàng vì dòng gợi ý dưới ô ngày. Cần một viên liền khối, các đoạn ngăn bằng vạch dọc, ô bên trong không viền, nhãn thẳng hàng, gợi ý đêm không đẩy lệch bố cục. Trên điện thoại vẫn xếp dọc.
- `_DataTable`: mã đơn (`FJ2611-0012`) xuống dòng ở 1280px. Cho ô mã/số tiền `white-space: nowrap` qua class của cột (thêm thuộc tính `NoWrap` vào `TableColumn` nếu cần).

## Skill và giới hạn khi dùng

`DESIGN.md` + `tokens.css` là nhận diện đã chốt. Skill chỉ dùng để **soi và sửa chất lượng**, không được đổi màu, font, bo góc hay thêm phong cách mới.

| Skill | Áp cho | Dùng phần | **Không** dùng phần |
|---|---|---|---|
| `minimalist-ui` | Component dashboard: `_StatCard`, `_DataTable`, `_DescriptionList`, `_Timeline`, `_FilterBar`, `_Tabs`, `_PageHead`, `_Breadcrumb`, `_EmptyState`, skeleton, `NotificationBell`, `_RoomCalendar`, sidebar/topbar | Mục 2 (cấm), mục 5 (thông số component), mục 6 (icon), mục 7 (chuyển động nhẹ) | Bảng màu và font riêng của skill (mục 3–4): giữ Be Vietnam Pro/Lora và token `--fx-*` |
| `design-taste-frontend` | Component trang khách: `_SearchBar`, `_HomestayCard`, `_Gallery`, `_BookingPanel`, `_BookingStepper`, `_PaymentQr`, `<rating>`, `_PriceBreakdown`, header/footer, `_AuthLayout` | House style chống "AI slop", `scripts/inspect.mjs` (đo trong trình duyệt), vòng chụp → chấm → sửa ở 3 độ rộng | `scripts/draw.mjs` và mọi hướng/bảng màu/kiểu chữ/hero do skill "chia bài"; không tìm ảnh qua `assets.mjs` (ảnh homestay lấy từ dữ liệu, ảnh trang trí làm ở bước 03) |
| `ui-animation` | Toàn bộ `motion.css`, hộp xác nhận, toast, lightbox, date picker, ngăn kéo sidebar | Quy tắc chọn transition, easing, reduced-motion, hiệu năng, anti-patterns | Thời lượng mới: chỉ dùng các mức đã có trong `motion.css` (0.06–0.45s) và `--fx-ease` |
| `full-output-enforcement` | Mọi file sửa | Viết đủ, không để `...`/TODO | — |

`dataviz` không dùng ở bước này. Không dùng `gpt-taste`, `industrial-brutalist-ui`, `high-end-visual-design`.

## Việc cần làm

1. Đọc `SKILL.md` của 4 skill trên (đường dẫn `.agents/skills/<tên>/SKILL.md`).
2. Với **từng component** trong bảng, so với skill tương ứng và `DESIGN.md`, ghi lỗi tìm được vào bảng tạm (component → lỗi → quy tắc vi phạm).
3. Sửa trong `components.css`, `layout-*.css`, `motion.css`, partial/Tag Helper của component và mục UI Kit. Sửa ở component, không vá từng view.
4. Chạy `design-taste-frontend/scripts/inspect.mjs` trên `/Dev/UiKit`, `/`, `/Homestays` (cổng app Release như `decisions.md` 02-A, không ghi vào v5). Sửa lỗi mức cao.

## Không được đổi

- Mọi ràng buộc ở mục "Ràng buộc test" của `02-component.md` (hook `data-*`, chuỗi, `class="status-code"`, token antiforgery).
- Tên tham số query, `asp-for`, `@model`, record model của component (chỉ thêm thuộc tính nếu thật cần).
- Token: chỉ thêm biến mới vào `tokens.css` nếu thiếu, kiểm tương phản ≥ 4.5:1 cả sáng/tối.

## Kết quả bàn giao

- Hai commit: `fix(ui): sửa lỗi review bước 02`, rồi `style(ui): rà thư viện component theo minimalist-ui, design-taste-frontend, ui-animation`.
- Ghi vào `docs/ui/decisions.md` các chỗ bỏ qua khuyến nghị của skill vì trái `DESIGN.md`.
- Ảnh `playwright-cli` trước/sau của `/Dev/UiKit` ở 375/1280 × sáng/tối.

## Kiểm tra trước khi báo xong

- `dotnet build` không cảnh báo mới, `dotnet test HomestayBooking.sln` pass, báo số test.
- Không có `style="`, không thêm mã hex ngoài `tokens.css`, báo số mã hex còn lại.

## Báo cáo rồi dừng

Bảng: component → lỗi theo skill → đã sửa / bỏ qua (lý do). **Không bắt đầu bước 3.**
