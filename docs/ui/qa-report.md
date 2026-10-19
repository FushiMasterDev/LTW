# Báo cáo QA giao diện — bước 06

Ngày 19/10/2026, nhánh `feature/foundation`, kiểm trên HEAD `9e076f1` (sau B9) rồi sửa ở `77e0526`. Claude tự làm bước này theo yêu cầu người dùng. App kiểm thử chạy riêng cổng 5012 (Release, Development, SQL Server container `sql-homestay`), không đụng cổng 5011. Ảnh chụp không commit, nằm ở `/private/tmp/claude-501/-Users-phamngocphu-Downloads-LTW/d5b5bcfe-95ef-4718-b0eb-3298b33edca2/scratchpad/qa/` (thư mục tạm của phiên).

## Tóm tắt

- **768 cấu hình** (128 trang theo vai trò × 375/768/1280 × sáng/tối): 758 đạt ở lượt đầu, 10 lỗi thuộc 3 lỗi (đã sửa). Chạy lại toàn bộ 375px sau khi sửa: **256/256 đạt**.
- Tiếng Anh ở 1280 cho 11 trang khách: không tràn ngang, không còn chữ giao diện tiếng Việt (chữ có dấu còn lại là tên homestay/khu vực/loại phòng từ dữ liệu).
- Kiểm tự động trên mọi cấu hình: mã HTTP, lỗi JavaScript (`pageerror`), tràn ngang, ảnh thiếu `alt`, nút/link thiếu tên truy cập, ô nhập thiếu nhãn, nút cao < 40px ở 375px.
- Tìm và sửa **5 lỗi** (2 mức cao, 3 mức trung bình). Không còn lỗi mức cao/trung bình đã biết.
- `dotnet build -c Release -p:UseSharedCompilation=false`: 0 cảnh báo, 0 lỗi. `dotnet test HomestayBooking.sln -c Release --no-build`: **1719 pass + 1 skip, 0 fail** (skip là test hiệu năng chỉ bật khi `RUN_PERF_TESTS=1`).
- `python3 -I tools/ui-crawl.py http://localhost:5012`: 840 trang, 7 vai trò, không có 500; đúng 4 lỗi 404 đã biết (`/Bookings/Create` thiếu `roomTypeId`, 3 mã đơn mẫu trên UI Kit).

**Đề xuất: có thể merge `feature/foundation` vào `main`** sau khi người dùng xem lại phần "Còn lại" và mở thử trên một điện thoại thật.

## Lỗi đã sửa (`77e0526`)

| # | Mức | Lỗi | Tái hiện | Sửa |
|---|---|---|---|---|
| 1 | Cao | Nút `btn-brand` ("Đăng nhập", "Tìm phòng trống", "Đặt và giữ phòng"…), nút ngôn ngữ đang chọn và link "Bỏ qua đến nội dung chính" **không có vòng focus** khi dùng bàn phím | Trang chủ 1280, Tab qua header: "VI" và "Đăng nhập" chỉ đổi màu nhẹ | `components.css`: `.btn:focus-visible` dùng chung `outline: 3px solid var(--fx-focus)`. Nguyên nhân: Bootstrap đặt `outline: 0` và vẽ bằng `--bs-btn-focus-shadow-rgb`, nút dùng màu token không có biến này |
| 2 | Cao | Trang Thông báo: nút "Xem chi tiết" **chưa có bản dịch** (tiếng Anh vẫn hiện tiếng Việt) | `/Notifications` ở EN, thông báo có URL | Thêm khóa vào `SharedResource.en.resx`. Kiểm khóa trước đây bỏ sót dạng `L[x ? "a" : "b"]`; đã quét lại mọi chuỗi trong `L[...]`, không còn khóa thiếu |
| 3 | Trung bình | `/Account/ChangePassword` (và AccessDenied khi đã đăng nhập) **tràn ngang 33px** ở 375px, nút giao diện bị ép méo | Đăng nhập khách, mở Đổi mật khẩu ở 375px | `layout-public.css`: nút trong header không co; dưới 420px khi có menu tài khoản ẩn chữ thương hiệu về mặt hiển thị (logo và tên cho trình đọc màn hình vẫn còn) |
| 4 | Trung bình | Nút `.btn-sm` cao 36px trên điện thoại (bộ lọc Bill chờ duyệt, nút Duyệt/Từ chối, "Quay lại" ở Xử lý giao dịch), dưới mức 44px | `/Accounting/Receipts` ở 375px | `components.css`: dưới 576px `.btn-sm` cao tối thiểu 44px; desktop giữ 36px |
| 5 | Trung bình | Lịch sử đối soát hiện chuỗi kỹ thuật "Số tiền: 120000; ngày chuyển UTC: 2026-10-19T14:10:49.0000000Z" | `/Accounting/Settlements/Details/52` | `SettlementService.RecordPaidAsync` ghi "Số tiền: 120.000đ; ngày chuyển: 21:10 19/10/2026 (giờ Việt Nam)"; mở rộng `B9FinanceTests`. Dòng audit cũ của bảng #52 giữ nguyên (không sửa dữ liệu đã ghi) |

## Kiểm tra tự động

| Kiểm | Lệnh / cách | Kết quả |
|---|---|---|
| Inline style | quét `style="` trong `Views/`, `Areas/` | 0 |
| Mã màu ngoài `tokens.css` | quét `#rgb/#rrggbb` trong `wwwroot/css` (trừ `tokens.css`) và view | 0 |
| Khóa thiếu bản dịch EN | mọi chuỗi trong `L[...]`/`_localizer[...]` (kể cả biểu thức điều kiện) so với `SharedResource.en.resx` | 1 khóa thiếu ("Xem chi tiết") → đã thêm; còn 0 |
| Chữ tiếng Việt viết cứng lọt ra trang | 798 chuỗi tiếng Việt viết trong view (ngoài UI Kit) × 630 trang của 7 vai trò ở tiếng Anh | Chỉ còn khớp với **dữ liệu**: mô tả homestay, nhận xét, nội dung thông báo và ghi chú lịch sử do service lưu bằng tiếng Việt (giữ nguyên văn theo quyết định 02-C) |
| Web Interface Guidelines (rút gọn) | `transition: all`, `outline: none` không thay thế, `user-scalable`, `onclick`, `<img>` thiếu kích thước, `autofocus` | 0 vi phạm, trừ `autofocus` ở form đăng nhập/đăng ký/danh mục (ô chính duy nhất; mức thấp, giữ). Các bước 03–05d đã chạy bản đầy đủ của skill trên view của từng bước |

## Bàn phím

| Mục | Kết quả |
|---|---|
| Tab qua header trang chủ | Thứ tự: bỏ qua → logo → menu → VI/EN → giao diện → Đăng ký → Đăng nhập → thanh tìm. Sau sửa #1 mọi mục có vòng focus `--fx-focus` |
| Bộ chọn ngày (Flatpickr) | Focus ô ngày mở lịch; ↓ vào ngày, ←/→ chọn, Enter chốt nhận rồi trả; ghi đúng `CheckIn`/`CheckOut` ẩn; Esc đóng lịch |
| Modal xác nhận (`data-confirm`) | Enter mở, focus nằm trong modal, Esc đóng và **không** gửi form |
| Ngăn kéo sidebar 375px | Enter mở, Esc đóng |
| Lịch phòng của chủ | Ô đêm là `<button>` thật (ô ngoài tháng/đã qua bị `disabled`); khóa ngày vẫn làm được bằng form chọn phòng → ngày → xác nhận |

## Luồng theo vai trò

- **Chạy lại ở bước 06** (375px, khách chưa đăng nhập): tìm (9 kết quả) → chi tiết → đặt → đơn **FJ261019-006** có QR → tra cứu bằng mã + SĐT ra đúng đơn. Không lỗi JavaScript.
- **Mọi vai trò:** mở toàn bộ trang đọc (ma trận trên) bằng `0987000001`, `0912000001`, `NV001`, `KT001`, `QL001`, `AD001`, cùng chủ thử B9 `0977091919`.
- **Luồng ghi đã chạy và có bằng chứng ở báo cáo từng bước** (không lặp lại để tránh sinh thêm dữ liệu tài chính):
  - khách đặt có voucher, viết đánh giá, đổi/hủy: `03-trang-khach-report.md`, `03y-phong-cach-report.md`;
  - chủ xác nhận đơn, sửa homestay, khóa/mở 2 đêm trong 3 thao tác, lịch nhận–trả, đối soát, trả lời đánh giá: `04-noi-bo-report.md`, `05a-chu-homestay-report.md`, `b9-backend-report.md`;
  - nhân viên nhận/giao đơn, đặt hộ, phòng trống, trùng phòng, bị chặn ở duyệt nội dung: `05b-nhan-vien-report.md`, `b9-backend-report.md`;
  - kế toán SimulateBank → khớp giao dịch, duyệt bill, đối soát (gửi, xác nhận thay chủ, ghi chuyển tiền, mở lại): `04-noi-bo-report.md`, `05c-ke-toan-report.md`, `b9-backend-report.md`;
  - quản lý báo cáo, tạo/gia hạn hợp đồng, duyệt nội dung, thẩm định hồ sơ: `05d-quan-ly-report.md`, `b9-backend-report.md`;
  - quản trị tạo nhân viên, khóa/mở, đặt lại mật khẩu, chặn xóa danh mục đang dùng: `05d-quan-ly-report.md`.

## Đối chiếu "Định nghĩa xong" (`ui-rebuild.md`)

| Mục | Trạng thái | Bằng chứng |
|---|---|---|
| Không view nào tự viết lại mẫu UI đã có component | Đạt | Các bước 02–05d dựng component trước và đưa vào UI Kit; rà ở bước 04 xóa CSS/partial cũ; bước 06 không thấy mẫu lặp mới |
| `grep` không ra `style="` hay mã hex mới trong view | Đạt | 0 / 0 (bảng trên) |
| Mọi trang đạt ở 3 kích thước × 2 chế độ màu × 2 ngôn ngữ | Đạt (có giới hạn) | 768 cấu hình VI + chạy lại 375; tiếng Anh kiểm bằng quét 630 trang của 7 vai trò và ảnh 1280 của 11 trang khách, chưa chụp đủ mọi trang nội bộ ở EN × 3 kích thước |
| Không bỏ sót route nào; mỗi route có trạng thái | Đạt | 89 action GET: 128 trang theo vai trò trong bảng dưới; phần còn lại không phải trang (tải giấy tờ, ảnh bill, JSON báo giá/đêm kín/trạng thái đơn) hoặc cần token một lần (`Partner/Activate`, `Admin/Accounts/TemporaryPassword`) — đã kiểm ở 05b/05d |
| `dotnet build` không cảnh báo mới, `dotnet test` pass | Đạt | 0 cảnh báo; 1719 pass + 1 skip |
| Không đổi file trong `Services/`, `Data/`, `Models/Entities/`, `database/` | **Có ngoại lệ đã được chốt** | Bước backend B9 (người dùng chốt 19/10) sửa `Services/` có test; bước 06 sửa 1 dòng ghi chú audit trong `SettlementService` (lỗi #5). Không đổi `Data/`, `Models/Entities/`, migration, `database/` |

## Còn lại (mức thấp, không chặn merge)

| # | Mô tả | Ghi chú |
|---|---|---|
| L1 | `autofocus` ở form đăng nhập, đăng ký và form tên danh mục | Ô nhập chính duy nhất; trên điện thoại có thể bật bàn phím ngay |
| L2 | Hero trang chủ trên điện thoại khá tối | Lớp phủ đậm để chữ đạt tương phản khi chữ phủ gần hết chiều rộng (đã ghi ở 03y2) |
| L3 | `Partner/Status` của chủ thử B9: nhãn hồ sơ "Đã duyệt" nằm cạnh nhãn cơ sở "Chờ duyệt" (chưa có hợp đồng) | Đúng dữ liệu, nhưng có thể làm chủ hiểu nhầm; nên thêm một câu giải thích nếu cần |
| L4 | Ghi chú lịch sử, nội dung thông báo do service lưu bằng tiếng Việt vẫn hiện tiếng Việt khi giao diện tiếng Anh | Quyết định 02-C (không dịch dữ liệu) |
| L5 | Khối 3D tự chạy dừng khi ẩn tab và thao tác trên điện thoại thật chưa kiểm ngoài mô phỏng | Ghi ở 03y2; nên thử trên một máy thật trước khi demo |
| L6 | Không còn nút Tạm dừng cho nội dung tự chạy | Theo yêu cầu người dùng (03y3), khác khuyến nghị WCAG 2.2.2 |
| L7 | Database Development có nhiều đơn thử từ các bước (phần lớn đã hết hạn giữ phòng) | Giữ đơn demo `FJ261019-001` theo yêu cầu |

## Gap backend còn lại

- **G-19:** gửi lời mời kích hoạt đối tác qua SMS/Zalo — không làm theo yêu cầu, chốt kênh khi triển khai (hiện chuyển link thủ công).
- **G-23 mở rộng:** chi từng phần cho đối soát cần thêm cột/migration; hiện số tiền phải bằng đúng số phải trả.
- **G-26 dữ liệu cũ:** audit trước B9 không có snapshot nên trang duyệt chỉ so sánh được các trường thực có.
- Các gap G-01 → G-27 khác: xong ở B8/B9 (xem `backend-gaps.md`).

## Dữ liệu Development tạo ở bước 06

- Đơn **FJ261019-006** (khách thử "Khách QA 06", SĐT `0977060606`, Ba Đình Green Villa, 27–29/12/2026), trạng thái chờ thanh toán; sẽ hết hạn giữ phòng theo job hiện có. Không thanh toán, không xác nhận.

## Bảng route × vai trò × kích thước × chế độ màu

S = sáng, T = tối. ✓ = đạt mọi kiểm tự động; ✗ = lỗi ở lượt đầu (đều đã sửa và chạy lại 375px đạt). Ảnh: `/private/tmp/claude-501/-Users-phamngocphu-Downloads-LTW/d5b5bcfe-95ef-4718-b0eb-3298b33edca2/scratchpad/qa/shots/<tên file>` (thay `1280-light` bằng `375-dark`… cho cấu hình khác); ảnh 375px sau sửa ở `/private/tmp/claude-501/-Users-phamngocphu-Downloads-LTW/d5b5bcfe-95ef-4718-b0eb-3298b33edca2/scratchpad/qa/shots375/`.

| Vai trò | Route | 375 S/T | 768 S/T | 1280 S/T | Kết quả | Ảnh (1280 sáng) |
|---|---|---|---|---|---|---|
| anon | `/` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `anon_-1280-light.png` |
| anon | `/Account/EmployeeLogin` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `anon_Account_EmployeeLogin-1280-light.png` |
| anon | `/Account/Login` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `anon_Account_Login-1280-light.png` |
| anon | `/Account/Register` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `anon_Account_Register-1280-light.png` |
| anon | `/Bookings/Create?roomTypeId=67&guests=2` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `anon_Bookings_Create_roomTypeId_67_guests_2-1280-light.png` |
| anon | `/Bookings/Lookup` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `anon_Bookings_Lookup-1280-light.png` |
| anon | `/Home/StatusCode/404` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `anon_Home_StatusCode_404-1280-light.png` |
| anon | `/Homestays` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `anon_Homestays-1280-light.png` |
| anon | `/Homestays/Details/27` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `anon_Homestays_Details_27-1280-light.png` |
| anon | `/Partner` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `anon_Partner-1280-light.png` |
| customer | `/` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `customer_-1280-light.png` |
| customer | `/Account/ChangePassword` | ✗/✗ | ✓/✓ | ✓/✓ | tràn ngang 33px ở 375 → đã sửa | `customer_Account_ChangePassword-1280-light.png` |
| customer | `/Bookings/Create?roomTypeId=67&guests=2` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `customer_Bookings_Create_roomTypeId_67_guests_2-1280-light.png` |
| customer | `/Bookings/Details/FJ261011-010` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `customer_Bookings_Details_FJ261011_010-1280-light.png` |
| customer | `/Bookings/Mine` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `customer_Bookings_Mine-1280-light.png` |
| customer | `/Homestays` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `customer_Homestays-1280-light.png` |
| customer | `/Homestays/Details/27` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `customer_Homestays_Details_27-1280-light.png` |
| customer | `/Notifications` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `customer_Notifications-1280-light.png` |
| customer | `/Partner` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `customer_Partner-1280-light.png` |
| owner | `/Notifications` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `owner_Notifications-1280-light.png` |
| owner | `/Owner` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `owner_Owner-1280-light.png` |
| owner | `/Owner/Bookings` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `owner_Owner_Bookings-1280-light.png` |
| owner | `/Owner/Bookings/Details/3008` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `owner_Owner_Bookings_Details_3008-1280-light.png` |
| owner | `/Owner/Calendar` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `owner_Owner_Calendar-1280-light.png` |
| owner | `/Owner/Dashboard` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `owner_Owner_Dashboard-1280-light.png` |
| owner | `/Owner/Homestays` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `owner_Owner_Homestays-1280-light.png` |
| owner | `/Owner/Homestays/Edit/6` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `owner_Owner_Homestays_Edit_6-1280-light.png` |
| owner | `/Owner/Reviews` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `owner_Owner_Reviews-1280-light.png` |
| owner | `/Owner/Schedule` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `owner_Owner_Schedule-1280-light.png` |
| owner | `/Owner/Settlements` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `owner_Owner_Settlements-1280-light.png` |
| owner | `/Owner/Settlements/Details/52` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `owner_Owner_Settlements_Details_52-1280-light.png` |
| NV001 | `/Notifications` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `NV001_Notifications-1280-light.png` |
| NV001 | `/Staff` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `NV001_Staff-1280-light.png` |
| NV001 | `/Staff/Availability` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `NV001_Staff_Availability-1280-light.png` |
| NV001 | `/Staff/Bookings` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `NV001_Staff_Bookings-1280-light.png` |
| NV001 | `/Staff/Bookings/Create` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `NV001_Staff_Bookings_Create-1280-light.png` |
| NV001 | `/Staff/Bookings/Details/4` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `NV001_Staff_Bookings_Details_4-1280-light.png` |
| NV001 | `/Staff/ChangeRequests` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `NV001_Staff_ChangeRequests-1280-light.png` |
| NV001 | `/Staff/ChangeRequests/Create/4` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `NV001_Staff_ChangeRequests_Create_4-1280-light.png` |
| NV001 | `/Staff/Customers` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `NV001_Staff_Customers-1280-light.png` |
| NV001 | `/Staff/Customers/Details/5` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `NV001_Staff_Customers_Details_5-1280-light.png` |
| NV001 | `/Staff/Dashboard` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `NV001_Staff_Dashboard-1280-light.png` |
| NV001 | `/Staff/OwnerRegistrations` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `NV001_Staff_OwnerRegistrations-1280-light.png` |
| NV001 | `/Staff/OwnerRegistrations/Details/1` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `NV001_Staff_OwnerRegistrations_Details_1-1280-light.png` |
| KT001 | `/Accounting` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `KT001_Accounting-1280-light.png` |
| KT001 | `/Accounting/AdditionalDeposits` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `KT001_Accounting_AdditionalDeposits-1280-light.png` |
| KT001 | `/Accounting/Compensations` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `KT001_Accounting_Compensations-1280-light.png` |
| KT001 | `/Accounting/Dashboard` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `KT001_Accounting_Dashboard-1280-light.png` |
| KT001 | `/Accounting/Receipts` | ✗/✗ | ✓/✓ | ✓/✓ | 9 nút < 44px ở 375 → đã sửa | `KT001_Accounting_Receipts-1280-light.png` |
| KT001 | `/Accounting/Refunds` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `KT001_Accounting_Refunds-1280-light.png` |
| KT001 | `/Accounting/Settlements` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `KT001_Accounting_Settlements-1280-light.png` |
| KT001 | `/Accounting/Settlements/Details/53` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `KT001_Accounting_Settlements_Details_53-1280-light.png` |
| KT001 | `/Accounting/Transactions` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `KT001_Accounting_Transactions-1280-light.png` |
| KT001 | `/Accounting/Transactions/Process/1` | ✗/✗ | ✓/✓ | ✓/✓ | 1 nút < 44px ở 375 → đã sửa | `KT001_Accounting_Transactions_Process_1-1280-light.png` |
| KT001 | `/Notifications` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `KT001_Notifications-1280-light.png` |
| KT001 | `/Staff` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `KT001_Staff-1280-light.png` |
| KT001 | `/Staff/ChangeRequests` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `KT001_Staff_ChangeRequests-1280-light.png` |
| QL001 | `/Accounting` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Accounting-1280-light.png` |
| QL001 | `/Accounting/Compensations` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Accounting_Compensations-1280-light.png` |
| QL001 | `/Accounting/Dashboard` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Accounting_Dashboard-1280-light.png` |
| QL001 | `/Accounting/Receipts` | ✗/✗ | ✓/✓ | ✓/✓ | 9 nút < 44px ở 375 → đã sửa | `QL001_Accounting_Receipts-1280-light.png` |
| QL001 | `/Accounting/Refunds` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Accounting_Refunds-1280-light.png` |
| QL001 | `/Accounting/Settlements` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Accounting_Settlements-1280-light.png` |
| QL001 | `/Accounting/Settlements/Details/53` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Accounting_Settlements_Details_53-1280-light.png` |
| QL001 | `/Accounting/Transactions` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Accounting_Transactions-1280-light.png` |
| QL001 | `/Accounting/Transactions/Process/1` | ✗/✗ | ✓/✓ | ✓/✓ | 1 nút < 44px ở 375 → đã sửa | `QL001_Accounting_Transactions_Process_1-1280-light.png` |
| QL001 | `/Admin` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin-1280-light.png` |
| QL001 | `/Admin/Accounts` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Accounts-1280-light.png` |
| QL001 | `/Admin/Accounts/Details/3df96a6a-8196-4777-9712-b6e9f5ed34cd` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Accounts_Details_3df96a6a_8196_4777_9712_b6e9f5ed34cd-1280-light.png` |
| QL001 | `/Admin/Accounts/Details/93317b46-82fc-486f-993a-d8d786f63b89` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Accounts_Details_93317b46_82fc_486f_993a_d8d786f63b89-1280-light.png` |
| QL001 | `/Admin/Accounts/Details/ba6bf082-50aa-4026-befe-c64773f49df3` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Accounts_Details_ba6bf082_50aa_4026_befe_c64773f49df3-1280-light.png` |
| QL001 | `/Admin/Accounts/Details/c30f83a0-0b47-47eb-b167-6910ab47dd9b` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Accounts_Details_c30f83a0_0b47_47eb_b167_6910ab47dd9b-1280-light.png` |
| QL001 | `/Admin/Accounts/Details/d2507b59-e06a-4422-b6ec-cc260760ceed` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Accounts_Details_d2507b59_e06a_4422_b6ec_cc260760ceed-1280-light.png` |
| QL001 | `/Admin/Accounts/Details/e9fdc3ae-62a4-4cad-ae80-b9a9292bc8dd` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Accounts_Details_e9fdc3ae_62a4_4cad_ae80_b9a9292bc8dd-1280-light.png` |
| QL001 | `/Admin/Amenities` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Amenities-1280-light.png` |
| QL001 | `/Admin/Amenities/Create` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Amenities_Create-1280-light.png` |
| QL001 | `/Admin/Amenities/Edit/5` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Amenities_Edit_5-1280-light.png` |
| QL001 | `/Admin/Areas` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Areas-1280-light.png` |
| QL001 | `/Admin/Areas/Create` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Areas_Create-1280-light.png` |
| QL001 | `/Admin/Areas/Edit/2` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Areas_Edit_2-1280-light.png` |
| QL001 | `/Admin/Contracts` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Contracts-1280-light.png` |
| QL001 | `/Admin/Contracts/Create` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Contracts_Create-1280-light.png` |
| QL001 | `/Admin/Contracts/Details/1002` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Contracts_Details_1002-1280-light.png` |
| QL001 | `/Admin/Dashboard` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Dashboard-1280-light.png` |
| QL001 | `/Admin/Holidays` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Holidays-1280-light.png` |
| QL001 | `/Admin/Holidays/Create` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Holidays_Create-1280-light.png` |
| QL001 | `/Admin/Holidays/Edit/2026-01-01` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Holidays_Edit_2026_01_01-1280-light.png` |
| QL001 | `/Admin/Holidays/Edit/2026-02-16` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Holidays_Edit_2026_02_16-1280-light.png` |
| QL001 | `/Admin/Holidays/Edit/2026-02-17` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Holidays_Edit_2026_02_17-1280-light.png` |
| QL001 | `/Admin/Holidays/Edit/2026-02-18` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Holidays_Edit_2026_02_18-1280-light.png` |
| QL001 | `/Admin/Holidays/Edit/2026-02-19` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Holidays_Edit_2026_02_19-1280-light.png` |
| QL001 | `/Admin/Holidays/Edit/2026-02-20` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Holidays_Edit_2026_02_20-1280-light.png` |
| QL001 | `/Admin/Holidays/Edit/2026-04-26` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Holidays_Edit_2026_04_26-1280-light.png` |
| QL001 | `/Admin/Holidays/Edit/2026-04-30` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Holidays_Edit_2026_04_30-1280-light.png` |
| QL001 | `/Admin/Holidays/Edit/2026-05-01` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Holidays_Edit_2026_05_01-1280-light.png` |
| QL001 | `/Admin/Holidays/Edit/2026-09-01` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Holidays_Edit_2026_09_01-1280-light.png` |
| QL001 | `/Admin/Homestays` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Homestays-1280-light.png` |
| QL001 | `/Admin/Homestays/Details/1002` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Homestays_Details_1002-1280-light.png` |
| QL001 | `/Admin/Reports` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Reports-1280-light.png` |
| QL001 | `/Admin/RoomCategories` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_RoomCategories-1280-light.png` |
| QL001 | `/Admin/RoomCategories/Create` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_RoomCategories_Create-1280-light.png` |
| QL001 | `/Admin/RoomCategories/Edit/4` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_RoomCategories_Edit_4-1280-light.png` |
| QL001 | `/Admin/Vouchers` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Vouchers-1280-light.png` |
| QL001 | `/Admin/Vouchers/Create` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Admin_Vouchers_Create-1280-light.png` |
| QL001 | `/Dev/SimulateBank` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Dev_SimulateBank-1280-light.png` |
| QL001 | `/Dev/UiKit` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Dev_UiKit-1280-light.png` |
| QL001 | `/Notifications` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Notifications-1280-light.png` |
| QL001 | `/Staff` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff-1280-light.png` |
| QL001 | `/Staff/Availability` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_Availability-1280-light.png` |
| QL001 | `/Staff/Bookings` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_Bookings-1280-light.png` |
| QL001 | `/Staff/Bookings/Create` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_Bookings_Create-1280-light.png` |
| QL001 | `/Staff/Bookings/Details/4` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_Bookings_Details_4-1280-light.png` |
| QL001 | `/Staff/ChangeApprovals` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_ChangeApprovals-1280-light.png` |
| QL001 | `/Staff/ChangeRequests` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_ChangeRequests-1280-light.png` |
| QL001 | `/Staff/ChangeRequests/Create/4` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_ChangeRequests_Create_4-1280-light.png` |
| QL001 | `/Staff/Customers` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_Customers-1280-light.png` |
| QL001 | `/Staff/Customers/Details/5` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_Customers_Details_5-1280-light.png` |
| QL001 | `/Staff/OwnerRegistrations` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_OwnerRegistrations-1280-light.png` |
| QL001 | `/Staff/OwnerRegistrations/Details/1` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_OwnerRegistrations_Details_1-1280-light.png` |
| AD001 | `/Admin/Accounts/Create` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `AD001_Admin_Accounts_Create-1280-light.png` |
| customer | `/Account/AccessDenied` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt (403 đúng thiết kế) | `customer_Account_AccessDenied-1280-light.png` |
| customer | `/Bookings/Change/FJ261019-001` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `customer_Bookings_Change_FJ261019_001-1280-light.png` |
| customer | `/Home/Error` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `customer_Home_Error-1280-light.png` |
| QL001 | `/Staff/Homestays` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_Homestays-1280-light.png` |
| QL001 | `/Staff/Homestays/Details/6` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_Homestays_Details_6-1280-light.png` |
| QL001 | `/Staff/Overbooking/Index/3017` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_Overbooking_Index_3017-1280-light.png` |
| QL001 | `/Staff/OwnerRegistrations/Invitation/1003` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `QL001_Staff_OwnerRegistrations_Invitation_1003-1280-light.png` |
| partner | `/Partner/Status` | ✓/✓ | ✓/✓ | ✓/✓ | Đạt | `partner_Partner_Status-1280-light.png` |
