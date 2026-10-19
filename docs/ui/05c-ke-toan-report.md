# Bước 05c — Màn kế toán

Ngày 19/10/2026, nhánh `feature/foundation`. Chỉ nối service sẵn có; không sửa Services, Data, Entities, migration, database, policy hoặc tạo ảnh mới. Không đổi nhánh/push. App QA chạy riêng cổng 5012; giữ nguyên app 5011 và container `sql-homestay`.

## Màn và service đã nối

| Màn | Đã làm | Service |
|---|---|---|
| Đối soát chủ homestay | Lọc đủ năm/tháng/kỳ, mã homestay, trạng thái; phân trang; lập kỳ gần nhất hết grace hoặc lập kỳ cụ thể/toàn bộ hay một homestay | SettlementService.ListAsync, GeneratePeriodAsync, GenerateAsync; SettlementCalculator.PeriodOf |
| Chi tiết đối soát | Dòng snapshot: mã đơn, tiền phòng, cọc, hoa hồng, giảm giá, khoản bù chi riêng, phải trả chủ; tổng tiền service cuối bảng; Timeline từ audit; alias URL thông báo | DetailsAsync |
| Thao tác đối soát | Gửi chủ; ghi nhận toàn bộ đã chuyển bằng mã ngân hàng; mở lại kèm lý do và xác nhận danger; hiện thao tác theo trạng thái/quyền | SendAsync, RecordPaidAsync, ReopenAsync, CanTransition |
| Xác nhận chủ | Giữ màn Owner có sẵn, dùng đúng chủ của cơ sở; không giả quyền Owner cho kế toán | ConfirmAsync tại Owner/Settlements |
| Khoản bù | Danh sách Submitted đủ điều kiện chi, mã tham chiếu ngân hàng, xác nhận trước ghi nhận; quản lý/quản trị chỉ đọc | CompensationService.PendingAsync, RecordPaidAsync; RejectAsync đã nối POST nhưng chưa có dữ liệu đọc hợp lệ để dựng form từ chối (G-24) |
| Dashboard | Giữ giao dịch/bill/hoàn tiền/cọc bổ sung; thêm số kỳ và tổng PayoutAmount chờ gửi/chờ chuyển; duyệt hết mọi trang Draft, Sent, Confirmed | AccountingService hiện có + SettlementService.ListAsync |

Tab và sidebar đã thêm hai màn mới. Dùng PageHead, Tabs, FilterBar, DataTable/Pager, PriceBreakdown, Breadcrumb, Timeline, Alert, EmptyState, money/status-badge sẵn có; không thêm component/CSS/JS/thư viện. Có 48 resource EN mới. Tiền căn phải, định dạng VND và `tabular-nums`; view không tính công thức tiền. Khoản bù không cộng vào PayoutAmount. Không gọi `GenerateRecentAsync` hoặc Actor.System từ controller.

## Kiểm chứng

- `dotnet build -c Release -p:UseSharedCompilation=false`: 0 warning, 0 error. Log `/private/tmp/fuji-05c/build-final.log`.
- 13 test tích hợp kế toán mới pass: đọc theo quyền, antiforgery trên mọi POST, lập kỳ gần nhất/lập riêng, kỳ sai/chưa đóng, luồng gửi–chủ xác nhận–chi, Paid bất biến, quản lý chỉ đọc/lập, khoản bù chi qua service và Completed không được từ chối. Test đối chiếu các tổng HTML với `SettlementService.DetailsAsync`.
- `python3 -I tools/ui-crawl.py http://localhost:5012`: 840 trang, không có 500. Đúng bốn 404 đã biết: Bookings/Create thiếu roomTypeId và ba link đơn mẫu ở UI Kit. Log `/private/tmp/fuji-05c/crawl.log`.
- Đã xem `/Dev/UiKit` bằng QL001 và bố cục tham khảo `/accounting` trong tài liệu. Mọi trang mới qua `_Tabs`, policy Accounting; service kiểm quyền lại bằng Actor Employee thật.
- 30 ảnh playwright-cli cho Dashboard, danh sách đối soát, chi tiết Paid, chi tiết Draft, khoản bù × 375/768/1280 × sáng/tối. Đo được 30/30 HTTP 200, không tràn ngang toàn trang, không input thiếu label; mọi money có tabular-nums. Ảnh chụp sau khi bật reduced-motion trước khi nạp trang để không bắt giữa hiệu ứng hiện thẻ.
- Rà web-design-guidelines trên ba view mới và dashboard: không lỗi mức cao còn lại. Các input có label, native validation; hành động có tên, antiforgery, loading; mở lại dùng danger; giá trị người dùng được Razor encode. Không style inline, hex, onclick hoặc motion mới. Ngoại lệ đã chốt: VND vi-VN, kiểu viết hoa tiếng Việt, token/font/góc/Bootstrap Icons theo DESIGN.md.
- Bộ resource không có key trùng; nhãn mới có EN. Kiểm tra tiếng Anh và no-JS trên trình duyệt được ghi trong `/private/tmp/fuji-05c/` và `.playwright-cli/`.

## Luồng thật và dữ liệu Development đã tác động

- Đăng nhập KT001, bấm lập kỳ gần nhất: kỳ 1 tháng 10/2026 (01–15/10), hết grace 2 ngày theo đồng hồ VN. Đã thử gửi qua hộp xác nhận.
- Bảng **#53**, homestay **#15**, đơn **FJ261001-001**: Draft → Sent bằng KT001; chủ **0912000005** xác nhận → Confirmed; KT001 ghi nhận → Paid với tham chiếu **UI05C-DEMO-SETTLEMENT-53**. Snapshot hiển thị: tiền phòng **380.000đ**, cọc/giữ lại **114.000đ**, hoa hồng **57.000đ**, phải trả chủ **57.000đ**. Paid không còn nút gửi/chi/mở lại; timeline hiện đúng các bước.
- Bảng **#52**, homestay **#31**, kỳ 2 tháng 9/2026: Sent → Draft bằng KT001, lý do “UI05c: mở lại bảng demo để rà số liệu trước khi chi.”; chụp được hộp xác nhận danger. Tổng sau mở lại: phòng **600.000đ**, giữ lại **180.000đ**, hoa hồng **60.000đ**, phải trả **120.000đ**. Giữ nháp để demo tiếp.
- Không tạo đơn mới, không hủy/sửa đơn demo **FJ261019-001**. Các thao tác sinh audit/thông báo theo service. Ghi nhận chuyển tiền là thao tác mô phỏng trên Development, không gọi ngân hàng thật.
- Danh sách khoản bù Development hiện rỗng; đường ghi nhận chi và chặn từ chối Completed được thử thành công bằng integration test với DB InMemory riêng, không chèn dữ liệu tài chính giả vào SQL Server để chụp ảnh.

## Ảnh bàn giao

Tất cả ở `/private/tmp/fuji-05c/`, không commit. Mẫu desktop 1280:

| Màn | Sáng | Tối |
|---|---|---|
| Dashboard | `dashboard-1280-light.png` | `dashboard-1280-dark.png` |
| Danh sách đối soát | `settlements-1280-light.png` | `settlements-1280-dark.png` |
| Chi tiết Paid | `settlement-paid-1280-light.png` | `settlement-paid-1280-dark.png` |
| Chi tiết Draft | `settlement-draft-1280-light.png` | `settlement-draft-1280-dark.png` |
| Khoản bù | `compensations-1280-light.png` | `compensations-1280-dark.png` |

Ảnh mobile/tablet cùng tên với 375/768. `reopen-danger.png` là xác nhận mở lại; `settlement-1280-en-dark.png` là kiểm tra EN. Snapshot và log trình duyệt ở `.playwright-cli/` hoặc thư mục tạm, không commit.

## Gap đã ghi và chỗ lệch prompt

- **G-23:** ConfirmAsync chỉ cho chủ; RecordPaidAsync không nhận amount/date. UI giữ toàn bộ payout + ngày ghi nhận hệ thống, không có ô nhập dữ liệu rồi bỏ qua. Paid không mở lại theo luật đã có.
- **G-24:** PendingAsync chỉ trả Completed nhưng RejectAsync cấm Completed. Chưa làm form từ chối/lý do/danger khi thiếu read API các khoản bù trước Completed; không đưa nút luôn thất bại lên màn.
- **G-25:** Bốn ViewModel/màn bước 04 chưa nạp liên kết đối soát thật, nên chưa thêm shortcut từ giao dịch/đơn. Settlement list thiếu tên homestay/catalog lịch sử: hiển thị và lọc ID có nhãn rõ.

Không có thay đổi nhận diện lệch DESIGN.md. Các giới hạn trên là hợp đồng backend, không thay nghiệp vụ trong bước giao diện.

## Cần người dùng quyết định

1. Nếu vẫn cần kế toán xác nhận thay chủ hoặc nhập số tiền/ngày chuyển thực tế: cần chốt quyền, chi toàn phần/từng phần và ý nghĩa ngày, rồi giao bước backend. Hiện giữ luật B5/D15: chủ xác nhận tùy chọn, chi toàn bộ, thời gian hệ thống.
2. Để từ chối khoản bù trước Completed qua UI: cần backend trả danh sách đủ điều kiện cùng trạng thái; sau đó mới thêm form lý do + danger. Hiện danh sách chờ chi chỉ được chi, không được từ chối.
3. Ưu tiên bổ sung SettlementId và tên homestay lịch sử vào read DTO cho bốn màn bước 04 nếu muốn hoàn thiện lối tắt. Hiện không đoán kỳ từ ngày giao dịch hoặc mở catalog công khai thay dữ liệu lịch sử.
