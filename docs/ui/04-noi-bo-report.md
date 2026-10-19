# Báo cáo bước 04 — Chuyển các màn nội bộ đang có

Ngày 19/10/2026, nhánh `feature/foundation`, bắt đầu từ `bf98dee` (prompt cập nhật sau `b12d011`). Không đổi nhánh, không push, không bắt đầu bước 05.

## Màn đã chuyển

24 view trong phạm vi, giữ `@model`, form binding, route, antiforgery, validation và quyền sidebar:

| Khu | View |
|---|---|
| Admin | Dashboard; Amenities Index/Form; Areas Index/Form; Holidays Index/Form; RoomCategories Index/Form; Vouchers Index/Create |
| Staff | Dashboard; ChangeRequests/Index; ChangeApprovals/Index |
| Owner | Dashboard; Bookings/Index; Bookings/Details |
| Accounting | Dashboard; Transactions Index/Process; Receipts/Index; Refunds/Index; AdditionalDeposits/Index |
| Dev | SimulateBank |

Danh sách dùng PageHead, FilterBar, DataTable, EmptyState và Pager; ô bảng giữ đầy đủ nội dung trên thẻ ở độ rộng dưới 992px. Danh sách duyệt đổi/hủy, bill và cọc bổ sung giữ hình thức công việc có form, dùng Timeline/DescriptionList/ReceiptReview để đọc và xử lý cùng chỗ. `data-approval` vẫn nằm trên `section`; `data-additional-review`, trường `Note` và điều kiện yêu cầu legacy giữ nguyên.

Form danh mục và giả lập dùng `form-field`, Breadcrumb, thẻ tối đa 40rem, nút cam và `data-loading-text`; cuối form dính dưới trên điện thoại. Xóa danh mục vẫn qua controller/CatalogDeletionService hiện có, thông báo qua Alerts và hộp xác nhận danger. Từ chối yêu cầu/bill và thu hồi voucher dùng hộp xác nhận tương ứng.

Dashboard chỉ ghép số liệu đang được controller truyền. Không thêm biểu đồ, phần trăm tăng trưởng, truy vấn, controller hoặc service. Owner dùng bố cục thông tin → thao tác → lịch sử trên điện thoại; nút xác nhận/từ chối ít nhất 44px. Link việc của Owner kiểm cùng claim ApprovedOwner đang có. Accounting giữ Tabs và điều kiện hiện cọc bổ sung cho Kế toán.

## Component thêm hoặc sửa

- Thêm `_TaskList` với `TaskListModel`/`TaskListItem`: danh sách việc và liên kết từ dữ liệu có sẵn; có mẫu thường/rỗng trong UI Kit.
- Thêm `_ReceiptReview` với `ReceiptReviewModel`: ảnh bill, DescriptionList, cảnh báo chờ lâu và form duyệt/từ chối. UI Kit có mẫu thiếu tiền/chờ lâu, không tải ảnh bill thật hoặc dựng form ghi dữ liệu.
- `_StatCard`: nhãn cạnh icon sage, số Fraunces phía dưới; giữ hook count-up và giá trị cuối trong HTML.
- `_DataTable`: bọc nội dung từng ô để nhãn và nhiều thành phần không rơi sang cột khác; thẻ đến 991.98px để bảng tài chính/Voucher không bị cắt ở 768px; chỉ giữ đầu bảng có sắp xếp, số vẫn căn phải và tabular-nums.
- `_FilterBar`: dùng lại cho danh mục và danh sách nội bộ; ẩn legend lặp khi chỉ có một lựa chọn mang cùng nhãn. Query string, tên trường và giá trị enum giữ nguyên.
- `_DescriptionList`, `_Timeline`, `_PriceBreakdown`, `<money>`, `<status-badge>` thay các mẫu lặp. Money dùng cả ở số tiền cần nộp, gợi ý khớp và mô tả giao dịch; chuỗi xác nhận trong thuộc tính giữ định dạng văn bản.
- Dùng chung lớp form, bố cục chi tiết Owner và trạng thái rỗng bo góc; có mẫu trong UI Kit. Chữ mới qua `L[...]`, bổ sung resource EN. Không thêm ảnh, thư viện, token hoặc chuyển động; giữ `dashboard.js`/`motion.css` và reduced-motion.

## CSS và partial đã xóa

| File | Trước bước 04 | Sau | Chênh lệch |
|---|---:|---:|---:|
| layout-dashboard.css | 486 | 279 | −207 |
| components.css | 1040 | 1029 | −11 |
| Tổng hai file | 1526 | 1308 | −218 |

Xóa CSS tile, nhóm trạng thái cũ, thẻ/facts/thumbnail bill cũ, gợi ý khớp, độ rộng ref cũ, accent đầu thẻ, icon-chip và filter-bar cũ. Bỏ page-head/section-title/card-soft sau chuyển các chỗ còn dùng; count-pill và table-modern chuyển sang selector `fx-` của component. Giữ class shell đang dùng và override Bootstrap, validation, Flatpickr do thư viện sinh.

Xóa `_CatalogSearch.cshtml` adapter, thay mọi lời gọi bằng `_FilterBar`, gồm mẫu ở UI Kit. UI Kit và view Change dùng chung khách/Staff chỉ đổi class cũ sang class mới; không sửa quy trình form. Xóa thêm 7 dòng ghi đè page-head chết trong layout-public.css và tham chiếu chết ở base.css.

## Dữ liệu QA trong Development

Database đang cấu hình: `HomestayBooking_v5`, SQL Server container `sql-homestay`. Không dừng/xóa container, không sửa schema hoặc dữ liệu seed bằng SQL trực tiếp.

| Dữ liệu | Kết quả |
|---|---|
| RoomCategory ID `1002` | Tạo “Kiểm tra UI04 20261019”, sửa thành “Kiểm tra UI04 đã sửa 20261019”, xóa thành công qua hộp xác nhận; không còn mục danh mục này, audit theo cơ chế sẵn có |
| Booking ID `3013`, mã `FJ261019-001` | Tạo bằng khách seed Nguyễn Văn Minh / `0987000001`; Ba Đình Green Villa, RoomType `14`, phòng `101`; 15–17/12/2026, 2 khách; tổng 1.800.000đ, cọc 540.000đ |
| BankTransaction ID `1003` | Provider Simulator, mã `SIM-bdbe4caf3bb24f55b7ad074d4ab0bbc0`, nội dung “UI04 doi chieu 20261019”, 540.000đ; QL001 tạo bằng SimulateBank, ban đầu Unmatched; KT001 khớp tay qua Transactions/Process với đơn QA |
| Owner xác nhận | Chủ `0912000001` xác nhận đơn QA trên 375px; nút đo 309 × 44px; trạng thái cuối Confirmed, lịch sử có khớp tiền và xác nhận |

Ghi chú đơn: “Kiểm thử giao diện bước 04 – đối chiếu giao dịch và xác nhận chủ”; ghi chú khớp: “Kiểm thử giao diện bước 04”. Có Payment, BookingNights/RoomNights, lịch sử, audit/thông báo theo service sẵn có. Không tạo khách mới, không dùng đơn cũ để xác nhận thử, không chuyển tiền thật. Đơn QA đã xác nhận vẫn giữ lịch hai đêm; không tự xóa dữ liệu nghiệp vụ.

## So với DESIGN.md

Giữ nguyên Manrope/Fraunces, xanh rừng/cam đất, token sáng/tối, bo góc và chuyển động dashboard. Đã cập nhật phần Dashboard của DESIGN.md, bỏ câu chặn bước 04. Các lựa chọn mới ghi trong decisions.md.

Admin chưa có link xử lý hồ sơ chờ vì màn đó thuộc bước 05. Owner Dashboard chưa có số đơn cần xác nhận vì model hiện chỉ chứa homestay/hồ sơ; hiển thị đường dẫn tới danh sách đơn, không thêm truy vấn để lấy số. Đây là giới hạn dữ liệu hiện tại, không thay đổi nghiệp vụ.

## Cần người dùng quyết định

Giữ đơn QA `FJ261019-001` để demo hay hủy bằng luồng nghiệp vụ sau khi review. Hiện giữ nguyên trạng thái Confirmed và ghi rõ lịch đang chiếm; không tự hủy/xóa để tránh tạo thêm nghiệp vụ hoàn tiền. Không có quyết định nghiệp vụ mới khác.

## Kiểm tra và bằng chứng

- Các commit theo khu: `2f3e5a6` Admin, `dbc2549` Staff, `ea4b8a2` Owner, `c3d889c` Accounting; sau cùng là commit dọn CSS/partial kèm báo cáo này. Mỗi khu có build, test và vòng chụp → sửa; test liên quan lần lượt 59/45/32/72 đạt, ngoài các lượt toàn bộ suite. Mọi commit có trailer Co-Authored-By theo yêu cầu.
- Bộ ảnh cuối `final-*.png`: **204 cấu hình** màn/trạng thái, ở **375/768/1280 × sáng/tối**. AD001 mở toàn bộ Admin/Dev; QL001 UI Kit/duyệt yêu cầu; NV001 Dashboard/tra đơn (rỗng và có kết quả); KT001 toàn bộ Accounting; Owner Dashboard/danh sách/chi tiết trước và sau xác nhận. Có thêm bộ Owner ở bảy trạng thái có dữ liệu và ảnh luồng ghi QA.
- Các log `final-{admin,staff,owner,accounting}.log` và `final-dashboards.log`: **0 tràn trang, 0 bảng bị cắt, 0 điều khiển hiển thị thiếu nhãn, 0 pageerror JavaScript**. Đã xem ảnh và sửa: nút bị co chữ, ô bảng nhiều thành phần lệch cột, bảng nhiều cột tại 768px, thẻ rỗng vuông và khoảng cách phần lịch sử. Dashboard được cuộn qua từng thẻ trước chụp để reveal hiện đủ số liệu.
- `accessibility.log`: **20 cấu hình**, gồm 4 dashboard × 2 theme × reduced-motion/không JS và 4 mẫu tiếng Anh. Không tràn, không StatCard bị ẩn; reduced/no-JS không có animation đang chạy trong nội dung. Đo **595 mẫu chữ/nền đặc**, nhỏ nhất **5.17:1**, không mẫu dưới 4.5:1. Đây là đo dashboard trên Chromium; không suy rộng thành đo mọi pixel ảnh bill hoặc thiết bị vật lý. No-JS tối dùng harness đặt theme để kiểm CSS.
- `python3 -I tools/ui-crawl.py http://localhost:5012`: **840 lượt / 7 vai trò, 0 lỗi 500**. Bốn 404 đã biết: `/Bookings/Create` thiếu roomTypeId và ba mã đơn mẫu UI Kit `FJ2611-0012/0013/0014`. Đã phát hiện/sửa lời gọi CatalogSearch còn sót ở UI Kit rồi crawl lại bản cuối.
- Rà **31 file `Areas/**/*.cshtml`** theo Web Interface Guidelines tải mới; không còn lỗi mức cao. Các sửa xác nhận danger, nhãn điều khiển, kích thước ảnh bill, vùng bấm và responsive được kiểm lại. Bằng chứng `web-design-review.md`, `web-guidelines-final.md` trong thư mục artifact.
- `static-audit.json` và `rg`: **0 inline `style="`, 0 mã màu ngoài tokens.css, 0 tham chiếu class/partial đã xóa, 0 thay đổi @model**, 11 khóa resource EN mới, 0 khóa thiếu/trùng. `git diff --check` sạch. Không đổi Controller, Services, Data, Entities, migration, database script, thư viện hoặc ảnh.

Mọi ảnh/log/script QA nằm ở `/private/tmp/fuji-04/` hoặc `.playwright-cli/`, không commit. Kiểm tra điện thoại dùng Chromium emulation, chưa kiểm thiết bị vật lý.

Build cuối: `dotnet build -c Release -p:UseSharedCompilation=false` — **0 lỗi, 0 cảnh báo**. Test cuối: `dotnet test HomestayBooking.sln -c Release --no-build` — **1597 pass, 1 skip, 0 fail**, tổng 1598; skip là `StaffAvailabilityAndDatedSearchStayWithinTargets` theo cấu hình sẵn có. Log `build-final.log`, `test-final.log` trong thư mục artifact.

App riêng cổng 5012 đã được dừng bằng đúng PID lưu khi khởi chạy và PID con của nó; hai phiên trình duyệt QA đã đóng. Không tác động app cổng 5011 hoặc vòng đời container `sql-homestay`. Dừng ở bước 04.
