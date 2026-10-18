# Bước 3y – Đổi phong cách theo bản mẫu "Lúa" và thêm chuyển động 3D

> Chạy **sau bước 03** (HEAD `ad0818c`) và **trước bước 04**, trong **phiên Codex mới** (để nạp skill ở `.agents/skills/`).
>
> Ngày 18/10/2026, người dùng chốt đổi nhận diện giao diện sang phong cách của bản mẫu Lovable https://lua-ivory-forest-capture.lovable.app, kèm nhiều chuyển động, ưu tiên 3D. Quyết định này **thay thế** các luật cũ: "không đổi màu, font, bo góc", "chỉ một màu nhấn", "không scroll reveal", "thời lượng 0.06–0.45s" (DESIGN.md và `decisions.md` 02-D, 02x, 03-1). Tên thương hiệu vẫn là **Fuji Homestay**; "Lúa" chỉ là tên bản mẫu.

Đọc trước:
- `docs/prompts/ui-rebuild.md`, `DESIGN.md`, `docs/ui/decisions.md`, `docs/ui/03-trang-khach-report.md`.
- `/Dev/UiKit` (đăng nhập `QL001` / `Fuji@2026` ở `/Account/EmployeeLogin`).
- Mở bản mẫu bằng `playwright-cli`: `/`, `/search`, `/homestay/may-sapa`, `/owner`, `/staff`, `/accounting`, `/admin`. Phần tử trên bản mẫu chỉ hiện khi cuộn tới, nên cuộn hết trang rồi mới chụp full page.

Skill:
- `design-taste-frontend`: house style, `references/motion.md`, `scripts/inspect.mjs`, vòng chụp → chấm → sửa. **Không** chạy `draw.mjs`, vì hướng thiết kế đã chốt theo bản mẫu.
- `ui-animation`: easing, hiệu năng, reduced-motion, anti-pattern.
- `image-to-code`: được dùng với ảnh chụp bản mẫu.
- `full-output-enforcement`, `web-design-guidelines`.

## 1. Bản mẫu: các giá trị đã đo từ CSS/JS của nó

Dùng làm điểm xuất phát, chỉnh lại nếu không đạt tương phản.

| Nhóm | Giá trị trong bản mẫu |
|---|---|
| Nền | `oklch(97.5% .012 85)` (ivory); thẻ `oklch(99% .006 85)`; nền phụ `oklch(94.5% .018 82)`, `oklch(93% .025 80)` |
| Chữ | `oklch(24% .012 60)`; chữ phụ `oklch(50% .02 65)` |
| Xanh rừng | `oklch(36% .06 155)`: logo, khối nổi bật, mục đang chọn. Sidebar `oklch(26% .03 155)`; mục sidebar hover `oklch(32% .035 155)`; viền sidebar `oklch(34% .03 155)` |
| Cam đất | `oklch(62% .13 42)`: nút hành động chính (Tìm, Đặt phòng, Đăng ký), chữ nghiêng nhấn, sao đánh giá, mục sidebar đang chọn |
| Viền | `oklch(89% .02 80)`; ô nhập `oklch(88% .02 80)` |
| Font | Tiêu đề: **Fraunces** (opsz 9–144, 400/500/600), `letter-spacing: -.02em`. Chữ giao diện: **Manrope** 400–700. Đã kiểm: cả hai có bộ ký tự `vietnamese` trên Google Fonts |
| Bo góc | Gốc 1rem. Thẻ và ảnh 1.5rem (`rounded-3xl`). Nút, chip, thanh tìm kiếm, mục sidebar: bo tròn hoàn toàn |
| Bóng | soft: `0 1px 2px …/.05, 0 8px 24px -12px …/.15`. lift: `0 2px 4px …/.06, 0 24px 48px -20px …/.3` |
| Kính mờ | `backdrop-filter: blur(14px)`, nền thẻ 85% độ đục. Dùng cho thanh tìm kiếm trên ảnh, nhãn trên ảnh, menu điện thoại |
| Khung | `max-width: 80rem`, lề 1.25rem (điện thoại) và 2rem (từ `md`). Khoảng cách section `py-24` (6rem) |
| Easing | `cubic-bezier(.2,.7,.2,1)` |
| Vào trang | `main`: fade-up 0.7s, dịch lên từ 18px |
| Hero | Chữ fade-up; thanh tìm fade-up trễ 150ms; ảnh nền `scale(1.05)`; lớp phủ gradient tối dần xuống dưới |
| Hiện khi cuộn | `.reveal`: opacity 0 → 1, dịch lên từ 24px, 0.8s, IntersectionObserver ngưỡng 0.12, chạy một lần, các phần tử trễ dần nhau |
| Thẻ | `.lift`: hover nâng 4px + bóng lift, 0.35s. `.zoom-img`: ảnh phóng 1.06, 0.8s |
| Nút trên ảnh | Kính mờ, nhấn thì `scale(.9)` |
| Header | Trang chủ: header trong suốt, chữ trắng đè lên ảnh. Trang khác: nền đặc, có viền dưới |
| Dashboard | Sidebar xanh rừng đậm, mục đang chọn là viên cam đất. Topbar sáng. Thẻ số liệu bo 1.5rem, số liệu dùng Fraunces cỡ lớn, icon trong vòng tròn sage. Biểu đồ: miền xanh rừng, cột cam đất |

## 2. Token và DESIGN.md (nhóm A)

- **Giữ nguyên tên biến `--fx-*`, chỉ đổi giá trị** để mọi component cũ đổi màu theo. Thêm biến khi cần:
  - `--fx-accent`, `--fx-accent-hover`, `--fx-on-accent` (cam đất);
  - `--fx-font-display`;
  - `--fx-radius-xl` (1.5rem);
  - `--fx-glass-bg`, `--fx-glass-blur`;
  - `--fx-sidebar-*`;
  - các token chuyển động ở mục 4.
  
  Màu viết bằng `oklch()` hoặc hex, nhưng **chỉ ở `tokens.css`**. Biến `--bs-*` của Bootstrap ánh xạ theo.
- **Vai trò màu:**
  - Cam đất cho **một** loại việc: nút hành động chính (`btn-brand`, nút tìm, nút đặt phòng, đăng ký) và điểm nhấn chữ.
  - Xanh rừng cho thương hiệu, khối tối, mục đang chọn, link.
  - `.tone-*` của trạng thái giữ nghĩa cũ; chỉnh giá trị nếu không đạt tương phản trên nền mới.
- **Chế độ tối bắt buộc**, vì bản mẫu không có. Tự thiết kế bộ tối cùng tinh thần: nền xanh than ấm, thẻ sáng hơn nền một bậc, cam đất sáng lên đủ tương phản.
- **Tương phản ≥ 4.5:1** cho mọi cặp chữ/nền ở cả hai chế độ. Riêng chữ trắng trên cam đất của bản mẫu chỉ đạt 3.8:1 (đã tính): làm tối cam đất cho nút, hoặc dùng chữ tối trên cam đất. Viết script nhỏ trong thư mục tạm để tính, đưa bảng kết quả vào báo cáo.
- **Font:** thay Be Vietnam Pro/Lora bằng Manrope (chữ giao diện) và Fraunces (h1–h3 trang khách, tiêu đề trang và số liệu `_StatCard` ở dashboard) trong `_HeadAssets.cshtml`. Chụp kiểm dấu tiếng Việt (ỗ, ữ, ặ, ậ, ỡ) ở mọi cỡ chữ, sáng/tối.
- Viết lại `DESIGN.md` theo nhận diện mới: front matter, Tinh thần, Chữ, Bề mặt, Component, Chuyển động, Không làm. Bỏ mục "một màu nhấn" và "chỉ thời lượng 0.06–0.45s". Thêm dòng vào `decisions.md`: ngày 18/10/2026, bước 03y, thay các quyết định nào và vì sao.

## 3. Component, layout và trang (nhóm B, D)

Sửa ở component, partial, Tag Helper và CSS dùng chung. Không vá riêng từng view. Mẫu mới phải vào thư viện component và `/Dev/UiKit` trước, rồi mới dùng ở trang.

**Component:**
- **Nút:** dạng viên thuốc. Hover nâng 1px kèm bóng; khi nhấn `scale(.97)`.
- **Ô nhập:** bo 0.75–1rem, nhãn nhỏ đậm phía trên, kiểu ô ngày/khách giống `_BookingPanel` của bản mẫu.
- **`_SearchBar` hero:** viên kính mờ nằm **trong** ảnh hero, như bản mẫu. Có một chỉ báo dạng viên trượt sang ô đang focus. Bản compact và bản điện thoại vẫn xếp dọc như hiện tại.
- **`_HomestayCard`:** ảnh 4:3 bo 1.5rem. Nhãn loại phòng/khu vực dạng kính mờ ở góc trái. Tên in đậm; điểm đánh giá bên phải có sao cam đất; dưới là khu vực có icon ghim và giá/đêm. **Không thêm nút yêu thích** (chưa có chức năng).
- **`_Gallery` desktop:** 1 ảnh lớn bên trái, 2 ảnh xếp dọc bên phải, ảnh thứ 4 trở đi vào lightbox. Điện thoại giữ dải ảnh vuốt.
- **Còn lại:** `_BookingPanel`, `_PriceBreakdown`, `_Alert`, `_Tabs`, `.fx-chip`, `_EmptyState`, `_Steps`, `_RatingInput`, `_BookingCard`, `_StatusPage`, `_AuthAside`, `_BookingStepper`, `_PaymentQr` (QR vẫn trên nền trắng cố định), `_StatCard`, `_PageHead`, `_DataTable`, `_FilterBar` theo cùng ngôn ngữ: bo 1.5rem, bóng soft, viền mảnh.

**Layout:**
- `_Layout`, header:
  - Ở trang chủ: trong suốt đè lên hero, sau khi cuộn qua hero thì thành kính mờ. Dùng IntersectionObserver trên một phần tử mốc, không gắn sự kiện `scroll`.
  - Ở trang khác: nền đặc.
  - Điện thoại: menu kính mờ hiện bằng fade-up.
- `_Layout`, footer: khối xanh rừng đậm, chia cột như bản mẫu, giữ các link hiện có.
- `_DashboardLayout`:
  - Sidebar xanh rừng, mục đang chọn là viên cam đất trượt sang mục mới.
  - Topbar sáng.
  - Chỉ đổi khung, không làm lại các view nội bộ (đó là bước 04). Các view đó vẫn phải hiển thị được.
- `_AuthLayout`: hai cột ảnh/form theo cùng phong cách.

**Trang khách đã làm ở bước 03:**
- **`Home/Index`:**
  - **Hero:** cao gần một màn hình, ảnh tràn viền. Gồm: nhãn kính mờ chứa số thật (`Model.HomestayCount` homestay), tiêu đề Fraunces 2 dòng có một cụm chữ nghiêng màu cam đất, một câu phụ, thanh tìm kính mờ.
  - **Khu vực:** thẻ ảnh dọc 3:4, tên khu vực và số homestay đè ở đáy ảnh, có link "Xem tất cả".
  - **Homestay nổi bật:** lưới 4 cột ở 1280px.
  - **3 cam kết:** thẻ có icon tròn.
  - **3 bước đặt phòng:** giữ, theo quyết định 03-1.
  - **Mời làm đối tác:** khối xanh rừng.
  - **Không có phần nhận xét khách**, vì chưa có service lấy đánh giá toàn hệ thống.
- **`Homestays/Index`, `Homestays/Details`:**
  - Trang chi tiết: tiêu đề + điểm + khu vực nằm **trên** gallery, như bản mẫu.
  - Tiện nghi dạng chip có dấu ✓ (Bootstrap Icons).
  - Phòng là thẻ chọn được: viền xanh rừng khi chọn.
  - Đánh giá là thẻ có vòng tròn chữ cái đầu tên.
  - `_BookingPanel` dính bên phải.
- **Còn lại:** `Bookings/*`, `Partner/*`, `Account/*`, `Home/StatusCode`, `Shared/Error` đổi theo component mới, bố cục giữ như bước 03.

**Ảnh khu vực:**
- Thêm `CoverUrl` (có thể null) vào `AreaSummary`. Lấy ảnh bìa của một homestay đang hoạt động trong khu vực đó ngay trong truy vấn có sẵn của `HomeController`.
- Không có ảnh thì dùng thẻ chữ trên nền xanh rừng.
- Không thêm service, không sửa Services/Data/Entities/migration.

## 4. Chuyển động (nhóm C)

Giữ đủ mọi chuyển động của bản mẫu ở mục 1, rồi làm hơn. Ưu tiên chiều sâu 3D bằng **CSS 3D transform (`perspective`, `rotateX/Y`, `translateZ`) và JavaScript thuần**. **Không thêm thư viện** (three.js, GSAP, Lenis, framer…). Nếu thấy cần, ghi đề xuất vào báo cáo, không tự thêm.

**Token** (thêm vào `motion.css` hoặc `tokens.css`, mọi chuyển động chỉ dùng các token này):
- `--fx-ease-out: cubic-bezier(.2,.7,.2,1)`;
- thời lượng `--fx-dur-1` … `--fx-dur-6`, khoảng 0.15 / 0.25 / 0.35 / 0.5 / 0.8 / 1.2s;
- `--fx-stagger: 70ms`;
- `--fx-perspective: 1000px`.

Các thời lượng cũ (0.06–0.45s) giữ cho phản hồi nút và form.

**Trang khách, tối thiểu các chuyển động sau:**

| # | Chỗ | Chuyển động |
|---|---|---|
| 1 | Vào trang | `main` fade-up 18px, 0.7s |
| 2 | Hero, ảnh | Ken Burns chậm (scale 1.05 → 1.12, khoảng 20s, đảo chiều), cộng parallax khi cuộn: ảnh trôi chậm hơn chữ, dùng `translate3d`, cập nhật trong `requestAnimationFrame` |
| 3 | Hero, chiều sâu | Desktop: ảnh, chữ, thanh tìm là 3 lớp `translateZ` khác nhau, cả khối nghiêng tối đa 3° theo con trỏ |
| 4 | Hero, chữ | Lần lượt hiện nhãn → dòng 1 → dòng 2 → câu phụ → thanh tìm, cách nhau `--fx-stagger` (thanh tìm trễ khoảng 150ms như bản mẫu) |
| 5 | Mọi section | Hiện khi cuộn tới, có chiều sâu: tiêu đề → chữ → ảnh/thẻ lần lượt dịch lên 24px kèm `rotateX(8deg)` về 0, 0.8s, chạy một lần |
| 6 | Thẻ khu vực | Nghiêng 3D theo con trỏ (tối đa 8°, perspective 900px). Tên khu vực nổi `translateZ(40px)` trên ảnh. Một vệt sáng chạy theo con trỏ (lớp phủ độ mờ thấp từ token). Ảnh phóng 1.06. Rời chuột thì trở về trong 0.5s |
| 7 | Thẻ homestay | `.lift` (nâng 4px + bóng lift, 0.35s), ảnh phóng 1.06 (0.8s), nghiêng nhẹ tối đa 4° trên desktop |
| 8 | 3 bước đặt phòng | Hiệu ứng riêng của trang: 3 thẻ ban đầu xếp chồng 3D (lệch `translateZ`/`rotateX`), khi cuộn tới thì tách ra thành hàng. Dùng `animation-timeline: view()` trong `@supports`; trình duyệt không hỗ trợ thì dùng hiện-khi-cuộn thường |
| 9 | 3 cam kết | Thẻ lật vào từ `rotateY(-12deg)` khi hiện; hover thì icon xoay 3D một vòng |
| 10 | Khối đối tác | Ảnh hoặc khối nghiêng `rotateY(10deg)` về 0 theo tiến độ cuộn |
| 11 | Header | Trong suốt → kính mờ khi qua hero, 0.35s |
| 12 | Thanh tìm | Viên chỉ báo trượt sang ô đang focus |
| 13 | Gallery / lightbox | Ảnh lớn phóng ra từ đúng vị trí thumbnail (kỹ thuật FLIP), đóng thì thu về |
| 14 | Chi tiết homestay | Thẻ phòng khi chọn có dấu ✓ bật ra (scale), viền đổi màu |
| 15 | `_BookingStepper`, `_Steps` | Đường tiến trình vẽ dần (`scaleX`) khi hiện |
| 16 | Modal xác nhận, toast | Vào bằng `scale(.96)` + `rotateX(6deg)` → 0, giữ nhịp tổng khoảng 370ms của modal |
| 17 | Link, nút, chip | Gạch chân link dài ra khi hover; nút nâng/nhấn như mục 3 |

**Dashboard: tiết chế để đọc dữ liệu.**
- Thẻ số liệu hiện lần lượt một lần.
- Số đếm lên trong 0.8s. Giá trị cuối đã có sẵn trong HTML, không JS thì vẫn đúng; không đặt `aria-live`.
- Viên mục đang chọn của sidebar trượt sang mục mới.
- Biểu đồ Chart.js vẽ dần (cấu hình `animation` 0.8s).
- **Không** nghiêng 3D, parallax hay Ken Burns ở dashboard.

**Luật bắt buộc:**
- **Chỉ animate `transform`, `opacity`** (thêm `filter`/`backdrop-filter` cho kính mờ khi thật cần). Không animate kích thước hay bố cục. Không dùng `transition: all`.
- **Không JS vẫn thấy hết nội dung:**
  - Trạng thái ẩn chỉ áp dụng khi `<html>` có class bật chuyển động. Class này do script theme có sẵn trong `<head>` gắn.
  - Không có `style="` trong markup. Độ trễ lệch nhau dùng `:nth-child`, hoặc JS gọi `style.setProperty('--fx-i', i)`.
- **`prefers-reduced-motion: reduce`:**
  - tắt nghiêng, parallax, Ken Burns, chồng thẻ, FLIP;
  - phần tử hiện-khi-cuộn hiện ngay, không di chuyển;
  - chỉ giữ đổi màu/độ mờ ≤ 0.2s.
  
  Mỗi khối chuyển động mới có khối reduce tương ứng trong `motion.css`.
- **Thiết bị cảm ứng hoặc không có hover** (`(hover: none)`, `(pointer: coarse)`): không nghiêng theo con trỏ. Parallax và chồng thẻ vẫn chạy nhưng nhẹ hơn.
- **Hiệu năng:**
  - Một IntersectionObserver dùng chung, `unobserve` sau khi chạy.
  - Sự kiện `pointermove` gộp theo `requestAnimationFrame`.
  - `will-change` chỉ bật khi đang tương tác.
  - Ken Burns dừng khi hero ra khỏi màn hình hoặc tab bị ẩn.
  - Ảnh hero `preload`, có kích thước sẵn: **CLS = 0** (đo bằng `PerformanceObserver` qua `playwright-cli`).
  - Cuộn trang chủ ở 1280px không có frame nào > 50ms (ghi trace).
- **File:** JS đặt ở `wwwroot/js/motion/` (ví dụ `reveal.js`, `tilt.js`, `parallax.js`, `flip-lightbox.js`), mỗi file một việc. Chỉ nạp ở layout/trang cần.

## 5. Nội dung và ảnh

- **Thương hiệu:** tên, logo và `translate="no"` của Fuji Homestay giữ nguyên. Không dùng chữ "Lúa".
- **Không bịa số liệu:**
  - không có "1.200+ homestay", "4,9★ từ 18.000+ chuyến đi", nhận xét giả, "phản hồi dưới 15 phút", VNPay;
  - không có "Phí dịch vụ 5%" hay "VAT 8%";
  - số và giá chỉ lấy từ dữ liệu thật.
- **Ảnh:**
  - **Không lấy ảnh của bản mẫu** (không rõ giấy phép).
  - Ảnh homestay lấy từ dữ liệu.
  - Ảnh trang trí: CC0 hoặc public domain, tối đa 4 ảnh, lưu ở `wwwroot/images/`, ghi nguồn vào `docs/ui/credits.md`.
  - Fuji bán phòng ở Hà Nội, nên hero là cảnh Hà Nội: giữ ảnh Hoàn Kiếm hoặc tìm ảnh rộng hơn, hợp lệ.
- **Câu chữ:**
  - Tiêu đề hero tối đa 2 dòng ở mọi độ rộng, ví dụ "Ngủ giữa phố cổ, thức dậy cùng *Hà Nội*."
  - Chữ mới đi qua `L[...]` và có bản dịch trong `SharedResource.en.resx`.

## 6. Không được đổi

- `@model`, tên trường form, `asp-*`, route, antiforgery, validation, mọi hook `data-*` và chuỗi mà test tích hợp đọc. Xem mục "Ràng buộc test" của `02-component.md` và các test trong `HomestayBooking.Tests/Integration`.
- Không sửa Services/Data/Entities/migration. Controller chỉ được thêm phần nhỏ đã nêu ở mục 3 (`CoverUrl`).
- Không `style="`, không mã màu ngoài `tokens.css`. Màn nào cũng chạy được cả sáng và tối.
- Không thêm thư viện CSS/JS.

## 7. Commit và vòng làm

Bốn commit, theo thứ tự:
1. `style(ui): token, font và DESIGN.md theo phong cách mới`
2. `style(ui): component, layout và UI Kit theo phong cách mới`
3. `feat(ui): chuyển động 3D cho trang khách và dashboard`
4. `style(ui): trang khách theo phong cách mới`

Mỗi nhóm làm theo vòng: sửa → `dotnet build -c Release -p:UseSharedCompilation=false` → `dotnet test HomestayBooking.sln -c Release --no-build` → chụp ảnh 375/768/1280 × sáng/tối → so với bản mẫu → sửa → commit.

Ảnh chụp để trong `.playwright-cli/` hoặc thư mục tạm, không commit.

**Chạy app:**
- Chạy app riêng: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project <đường dẫn tuyệt đối>/HomestayBooking.Web --no-launch-profile --urls http://localhost:5012`.
- Cổng 5011 có thể là app người dùng đang chạy: **không tắt**.
- Dừng app của mình bằng đúng PID của nó. **Không dùng `pkill -f` với mẫu ngắn**: ngày 18/10, `pkill -f "cat"` đã tắt nhầm Docker Desktop và SQL Server.
- SQL Server chạy trong container Docker `sql-homestay`.

## 8. Kiểm tra trước khi báo xong

- Build không cảnh báo mới. Test pass: báo số trước/sau (nền: 1595 pass + 1 skip).
- `style="` = 0. Mã màu ngoài `tokens.css` = 0.
- `inspect.mjs` trên `/`, `/Homestays`, `/Homestays/Details/{id}`, `/Bookings/Create?roomTypeId=…`, `/Bookings/Lookup`, `/Partner` ở 375/768/1280:
  - không còn FAIL; ngoại lệ phải ghi vào `decisions.md`;
  - trang chủ phải qua mức chuyển động của inspector (≥ 8 phần tử chuyển động).
- `playwright-cli`:
  - chụp lại bằng `set-reduced-motion reduce` (nội dung hiện đủ, không có gì di chuyển);
  - chụp lại khi tắt JavaScript (nội dung hiện đủ);
  - ở 375px không có thanh cuộn ngang;
  - quay một video ngắn (`video-start`) cảnh cuộn trang chủ và rê chuột lên thẻ.
- Bảng tương phản các cặp màu mới ở cả hai chế độ.
- `python3 -I tools/ui-crawl.py http://localhost:5012` với mọi vai trò: không có trang 500. Đã biết: `/Bookings/Create` thiếu `roomTypeId` trả 404, 3 thẻ mẫu ở UI Kit trả 404.
- Đi lại luồng của bước 03 bằng tài khoản khách `0987000001` và khi chưa đăng nhập: trang chủ → tìm → chi tiết → đặt → chi tiết đơn có QR → tra cứu.

## 9. Báo cáo rồi dừng

Viết `docs/ui/03y-phong-cach-report.md`:
- bảng token cũ → mới, kèm tương phản;
- component đã đổi;
- danh sách chuyển động: chỗ, thời lượng, cách tắt khi reduce;
- kết quả `inspect.mjs`;
- CLS và frame dài;
- ảnh đã dùng;
- chỗ còn khác bản mẫu và lý do;
- đề xuất thư viện (nếu có).

**Không bắt đầu bước 04.**
