# Rà thư viện component bước 02x

Ngày: 17/10/2026. Phạm vi theo `docs/prompts/ui/02x-ra-skill.md`; dừng trước bước 03.

## Sửa review bước 02

| Chỗ sửa | Lỗi | Cách sửa |
|---|---|---|
| `wwwroot/js/site.js`, `Views/Dev/UiKit.cshtml` | Chỉ bắt form có data-confirm, bỏ qua nút đổi lịch | Bắt mọi form; message/title/variant ưu tiên submitter rồi form; bỏ qua khi không có message; giữ requestSubmit(submitter). Thêm mẫu nút vào UI Kit |
| `TagHelpers/RatingTagHelper.cs`, `Tests/Ui/DataComponentTests.cs` | Nhãn của span không có role bị trình đọc bỏ qua | Khi có điểm thêm role=img; assert trong test rating hiện có |
| `Views/Shared/_Timeline.cshtml` | Directive nằm sau markup; badge viết cứng | Đưa using/model lên đầu; dùng status-badge; giữ hook và chuỗi tích hợp |
| `Views/Shared/_PaymentQr.cshtml` | Chỉ hiện MB | Hiện MB Bank (Ngân hàng Quân đội), dùng khóa dịch Ngân hàng Quân đội có sẵn |
| `Views/Bookings/Details.cshtml` | Wrapper/dòng trống sau khi chuyển countdown | Gộp thành tiêu đề h2 có margin dưới |
| `Views/Shared/_Gallery.cshtml` | Thiếu hướng dẫn khi rỗng | Truyền lại Description đầy đủ với hotline 1900 0000; dùng khóa dịch cũ |
| `Views/Shared/_DataTable.cshtml` | Sort thiếu tiền tố ứng dụng | Dùng Request.PathBase + Request.Path |

Kiểm chứng trước commit fix: `dotnet test HomestayBooking.sln --configuration Release --no-restore -p:UseSharedCompilation=false`: **1589 pass, 1 skip cũ, 0 fail** (1590 tổng); TRX `/private/tmp/LTW-ui02x/review.trx`. Lượt đầu trong sandbox lỗi named pipe; lượt chạy lại ngoài sandbox exit 0. `node --check site.js`, `git diff --check` đạt.

Playwright trên Release cổng 5012 / DeployTest / AutoMigrate=false: nút submit mở modal; Esc/Hủy không điều hướng; Đồng ý tới `?confirmed=button` đúng name/value; không có pageerror. Không thêm bản dịch, không sửa bank options/appsettings.

## Bảng rà skill

Bảng này ghi nhận lỗi/đối chiếu trước khi sửa và kết quả cuối. `minimalist-ui` dùng mục 2/5/6/7; `design-taste-frontend` dùng house style/inspector/vòng ảnh; không lấy palette, font hoặc bố cục marketing của skill.

| Component | Lỗi theo skill / quy tắc đối chiếu | Đã sửa / bỏ qua (lý do) |
|---|---|---|
| StatCard | Bóng khi đứng yên, trái bề mặt phẳng | Bỏ bóng; giữ viền token và thứ tự icon/số/nhãn theo DESIGN.md |
| DataTable | Mã xuống dòng; `overflow-wrap:anywhere` áp cả mã và tiền | Thêm TableColumn.NoWrap, class cho th/td; UI Kit bật ở mã/tiền. Mẫu bảng cũ trong UI Kit chuyển sang component, mobile thành thẻ |
| DescriptionList | Đối chiếu cấu trúc phẳng, chữ dài, nhãn muted | Giữ; hai cột và đường phân cách có ý nghĩa dữ liệu, dùng token |
| Timeline | Badge viết cứng và directive sai vị trí (review) | Sửa ở commit fix; giữ đường dọc, hook article và chuỗi test |
| FilterBar | Đối chiếu nhãn, khoảng cách, mobile drawer | Giữ; form GET rõ nhãn, actions 44px; viền form không phải trang trí |
| Tabs | Đối chiếu quiet navigation, keyboard, selected state | Giữ nav/aria-current và gạch dưới trạng thái; không chuyển thành widget tab giả |
| PageHead | Đối chiếu phân cấp yên tĩnh, bỏ module màu | Giữ nền trang, icon muted, tiêu đề sans; không ép heading lớn theo skill marketing |
| Breadcrumb | Đối chiếu mật độ topbar, chữ dài | Giữ ellipsis và ẩn bớt mục trên mobile; viền topbar giữ theo DESIGN.md |
| EmptyState | Đối chiếu nội dung, hướng dẫn, icon đồng nhất | Giữ; khôi phục mô tả Gallery ở commit fix, không thêm ảnh giả |
| Skeleton | Đối chiếu animation chỉ opacity, giảm chuyển động | Giữ pulse opacity 0.45s, reduced-motion tắt animation; không thêm stagger/reveal |
| NotificationBell | Đối chiếu 44px, số lượng, menu dài | Giữ nút 44px, 99+, scroll vùng menu, muted metadata, thông báo encode |
| Sidebar | Animate width/margin-left, palette cứng của count | Bỏ tween layout; count dùng token. Drawer vẫn transform, màu trạng thái có nghĩa |
| Topbar | Đối chiếu vùng bấm/focus, bề mặt | Giữ; blur rất nhẹ của thanh sticky được minimalist-ui cho phép, không thêm glass card |
| SearchBar (ngoại lệ D được prompt nêu) | Ô có viền riêng, nhãn ngày cao hơn, nhiều khung lồng | Một pill, vạch ngăn dọc, input không viền, hint không đẩy nhãn; tablet nút icon có nhãn, mobile xếp dọc |
| DateRangePicker | Đối chiếu focus, keyboard, lỗi/hint và motion vendor | Giữ native fallback và Flatpickr theo token; trong pill hint được định vị riêng; reduced-motion tắt animation lịch |
| GuestPicker | Đối chiếu vùng bấm và biên | Giữ input thật/min/max; trong pill nút không viền, vẫn 44px |
| UploadZone | Đối chiếu dashed border, focus, preview | Giữ viền nét đứt vì drop zone thật; không thêm hiệu ứng nâng/scroll reveal |
| Modal xác nhận | Nút submit bị bỏ qua; nhịp mở/đóng, Esc | Sửa review, test browser nút/form; dùng --fx-ease, giữ 150+220ms và reduced-motion |
| Alerts | Đối chiếu tone có ý nghĩa, không module màu | Giữ tone phản hồi và animation nhẹ; easing đồng bộ --fx-ease |
| Toast | Đối chiếu phản hồi đúng chỗ, aria-live | Giữ toast cho AJAX/background; copy vẫn phản hồi tại nút; không thêm stagger |
| PriceBreakdown | Đối chiếu số tiền, bảng phân cách, tabular-nums | Giữ money nowrap/căn phải và đường phân cách dữ liệu; không đổi quy tắc phí |
| Header | Glass/blur không cần thiết | Bỏ blur, dùng bg token phẳng; giữ gạch dưới điều hướng và thương hiệu |
| Footer | Đối chiếu chữ, tương phản, hierarchy | Giữ token và thông tin liên hệ; giữ đường phân khu theo DESIGN.md thay vì xóa mọi viền |
| AuthLayout | Đối chiếu khung gọn, controls, bóng card | Bỏ bóng primitive card-soft dùng chung; giữ bố cục/ảnh và font hiện tại, không thiết kế lại trang đăng nhập |
| HomestayCard (D) | Chỉ inspect theo prompt | Giữ hình 3:2, dữ liệu thật, rating ink và fallback; hover dùng shadow-raised có sẵn |
| Gallery (D) | Chỉ inspect; empty copy thuộc review | Giữ lưới/dải vuốt và lightbox; timer reveal cũ được hủy khi đổi nhanh/đóng |
| BookingPanel (D) | Chỉ inspect theo prompt | Giữ sticky component; không gắn vào trang thật trước bước 03 |
| BookingStepper (D) | Chỉ inspect theo prompt | Giữ read-only/aria-current và số thứ tự nghiệp vụ; không bỏ số theo rule trang trí |
| PaymentQr (D) | Chỉ inspect; bank label thuộc review | Giữ nền paper trắng, timer, copy; không đổi BankAccountOptions |
| RoomCalendar (D) | Mọi đêm Free tô emerald, trái màu khan hiếm của minimalist-ui | Free dùng surface/muted; chỉ đêm bận dùng tone. Giữ icon/chữ và trạng thái chọn/focus |
| UI Kit | Mẫu nút trên nền primary sáng ở dark chỉ 2.1:1; tiêu đề bảng dài; bảng demo cũ khác component | Nền tối cố định on-paper cho mẫu nút trắng; rút gọn heading, chuyển hướng dẫn xuống đoạn văn; dùng chung DataTable |
| Motion toàn cục | Nhiều easing, bóng thứ ba; global reduced-motion xóa cả màu/opacity | Dùng --fx-ease và shadow-raised; press tức thì; bỏ duration override toàn cục, chỉ loại chuyển động không gian |

## Vòng ảnh và inspector

Ảnh Playwright toàn UI Kit: `.playwright-cli/ui02x-{before|after}-{375|768|1280}-{light|dark}.png`; đủ trước/sau ở 375/1280 sáng/tối theo prompt, thêm 768 để kiểm tablet. Các ảnh từng section cùng prefix, gồm booking-search/data-table/room-calendar. Ảnh không commit vì thư mục đã gitignore.

Vòng 1: thấy pill có nhãn thẳng nhưng ở 768px nút chữ làm co input Guests; đổi nút tìm tablet thành icon 44px có aria-label. Vòng 2: thấy mẫu bảng cũ ở mục Trang danh sách vẫn bẻ mã và không dùng DataTable; chuyển sang partial chung. Không đổi màn nghiệp vụ bước 03.

Đo trước/sau: nhãn date lệch ~12.8px ở 1280 → chênh 0px ở 768/1280; 375 xếp dọc. Free light bg rgb(255,255,255)/text rgb(89,97,92), dark bg rgb(26,31,28)/text rgb(162,171,165); các cặp này dùng token cũ. Cả sáu cấu hình không tràn ngang, không pageerror.

Chạy `inspect.mjs` trên ba trang thật qua proxy QA; widths 1280/768/375. Báo cáo JSON/ảnh tại `/private/tmp/LTW-ui02x/inspect-*`. Lượt thô sau sửa: Kit 4 FAIL (hairline, outlined-box, few-pictures, focal-point), Home 5 FAIL (shadow, hairline, outlined-box, headline-lines, eyebrow), Homestays 2 FAIL (hairline, focal-point). Đã bỏ shadow hero-search cũ. Lượt kế tiếp Home có thể thêm still-page do số ảnh lazy chuyển động khác nhau theo lúc tải.

Các cờ loại theo phạm vi có lý do, ghi ở decisions.md:
- hairline/outlined-box: viền cấu trúc và form do DESIGN.md quy định; không xóa hệ viền để đạt style marketing.
- few-pictures/focal-point ở Kit: công cụ component, không có hero; focal-point ở Homestays: danh sách tra cứu không cần hero lớn.
- headline-lines Home mobile và eyebrow nhận đoạn hướng dẫn trước số 2: markup trang cũ, ghi nhận dành bước 03; không sửa Home trong 02x.
- still-page: không thêm animation hàng loạt để đủ 8–16 chuyển động; trái ui-animation và phạm vi đã chốt.

WARN được giữ: dashed là upload thật; SFMono chỉ ở code mẫu của Kit; nhiều cỡ chữ vì Kit trưng biến thể; không video vì không thêm asset; gallery/tabs cuộn có chủ đích trên mobile; đoạn demo Kit/Home dài được ghi nhận (Home dành bước 03). Không thêm ảnh/video giả hoặc ẩn phần tử khỏi inspector.

### Critique

ROUND 1

Seen in one second: thư viện dữ liệu yên tĩnh, thanh tìm còn co input ở tablet.

Idea: nhãn/giá/ngày rõ, màu chỉ gắn với trạng thái hoặc hành động.

Live thing: tìm phòng theo ngày/khách; lịch chọn đêm; modal xác nhận.

Layout words: Kit=danh mục / dữ liệu=bảng-danh sách / form=chia đoạn / lịch=lưới.

Scores: design 6.5 · usability 7 · creativity 5 · content 8 → 6.5 (cap raw FAIL: 6.0 theo rubric marketing).

Top three: SearchBar tablet co Guests (ảnh booking-search-768) → nút icon; bảng demo cũ bẻ mã (Kit desktop-page-02) → dùng DataTable; contrast mẫu dark-band 2.1:1 (inspector trước sửa) → on-paper.

ROUND 2

Seen in one second: cùng hệ component, dữ liệu và trạng thái đọc được ở cả hai theme.

Idea/live thing/layout giữ nguyên, không biến Kit thành landing page.

Scores: design 7 · usability 8 · creativity 5 · content 8 → 7.0 trước cap; giữ cap 6.0 nếu chấm nguyên rubric marketing vì những raw FAIL đã nêu. Đây là audit component, không nhận là site đạt giải.

Còn lại: Home mobile có heading ba dòng và CTA dưới fold, chờ bước 03; giữ viền theo DESIGN.md; Kit không bổ sung hero/video. Dừng ở hai vòng sửa.

## Motion review

| Trước | Sau | Vì sao |
|---|---|---|
| Sidebar transition width/margin-left | Đổi kích thước tức thì | Không animate layout, thao tác lặp phản hồi nhanh |
| Press mất 100ms | :active transition-duration 0s | Phản hồi ngay, nhả vẫn nhẹ |
| Easing enter/press/drawer riêng | --fx-ease | Nhất quán DESIGN.md |
| Hover bóng tự định nghĩa thứ ba | shadow-raised | Chỉ hai mức bóng |
| Lightbox timer cũ vẫn tồn tại khi đổi nhanh | clearTimeout và cleanup hidden | Timer trước không làm hiện ảnh sau quá sớm |
| Reduced-motion ép mọi duration về gần 0 | Chỉ tắt travel/scale/animation tương ứng | Giữ phản hồi màu/opacity |

Không animate width/height/top/left hoặc transition:all trong diff. Không thêm spring/JS animation hoặc will-change. Không thử trên thiết bị vật lý; Playwright mô phỏng viewport/keyboard/reduced-motion. Verdict: Approve cho thay đổi motion trong phạm vi component; các nguyên tắc bố cục marketing không dùng làm cớ thêm animation.

## Kiểm chứng bàn giao

- Build Release sau thay đổi cuối: `dotnet build --configuration Release --no-restore -p:UseSharedCompilation=false`, exit 0, **0 Warning(s), 0 Error(s)**. Log `/private/tmp/LTW-ui02x/handoff-build.log`.
- `dotnet test HomestayBooking.sln --configuration Release --no-build --no-restore`, exit 0: **1589 pass, 1 skip cũ, 0 fail; tổng 1590**. Log/TRX `handoff-test.log`, `handoff.trx` cùng thư mục. Số test trước/sau không đổi vì chỉ thêm assert vào test rating sẵn có.
- `node --check site.js`, `git diff --check`: đạt. Lint skill: 0 FAIL, 1 WARN padStart của đồng hồ (đúng mục đích).
- `style="` trong Views/Areas: **0**; hex CSS ứng dụng ngoài tokens, loại vendor: **20 → 18** (18 còn lại đều ở layout-public.css cũ, chờ bước 03). Không thêm hex, không đổi tokens.css.
- Inspector bàn giao sau các `--allow` đã giải thích: Kit **0 FAIL/7 WARN**; Home **0 FAIL/4 WARN**; Homestays **0 FAIL/2 WARN**. Đây là kết quả có ngoại lệ, không phải tuyên bố raw audit sạch. Không có cờ overflow/contrast còn lại.
- Playwright: 375/768/1280 × light/dark đều không overflow/pageerror; cả hai mẫu bảng đều DataTable (6 dòng tổng trong mỗi cấu hình). Range đo mã `FJ2611-0012`: **1 dòng**, computed white-space=nowrap.
- Modal: buttonPriority/formFallback/nativeFallback đều true; Esc/Hủy không gửi; accept giữ confirmed=button. EN: tên ngân hàng và hotline đầy đủ, không overflow; no-JS native date hiển thị và không overflow ở 768.
- Bàn phím: date picker ArrowDown/Esc; Guests Enter; lightbox mở/ArrowRight/Esc; sidebar và bell Enter/Esc; đổi tháng 10→11; reduced-motion translate/scale=none. QR copy phản hồi “Đã chép”, clipboard trùng data-copy. Không kiểm thiết bị vật lý.
- Ảnh section sau sửa ẩn topbar **chỉ trong thao tác screenshot** để sticky header không đè vào ảnh dài khi Playwright cuộn; ảnh toàn trang đưa về đầu trước chụp. Không thay hành vi UI.
- App QA Release 5012 / DeployTest / AutoMigrate=false đã dừng; proxy 5013 đã dừng. Snapshot từ `ui-before-start-1792254580673586000.json` đến `ui-after-stop-02x-handoff.json`: **45/45 bảng khớp COUNT_BIG và CHECKSUM_AGG**, không bảng khác biệt. Chỉ database thử được cấu hình cho QA, không chạy trên v5.
- Không sửa Services/Data/Entities/database, không thêm thư viện/component mới. File README/prompt/skill và ảnh người dùng có sẵn ngoài phạm vi được giữ nguyên ngoài commit. Không bắt đầu bước 03.
