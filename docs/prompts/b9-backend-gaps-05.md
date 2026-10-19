# B9 – Lấp gap backend phát sinh ở bước 05

> Chạy trong **phiên Codex mới**, **sau khi bước 05d đã commit và được review**. Không chạy song song với bất kỳ bước UI nào, vì bước này sửa `Services/`.
>
> Ngày 19/10/2026, người dùng chốt làm backend cho các gap ghi lại trong bước 05: G-16 → G-25 (trừ G-19), G-26, G-27 của 05d, và mọi gap mới khác vào `docs/ui/backend-gaps.md`. Sau phần backend, nối kết quả vào đúng chỗ UI đang làm tạm và sửa các điểm yếu ở mục "Sửa các điểm yếu".

Đọc:
- `QUY_TRINH_BTL.md` (bảng FR, mục 7.x, mục 7.10 của B8);
- `docs/ui/backend-gaps.md` (G-16 trở đi, kèm cột "Cách làm tạm" và "Trạng thái");
- `docs/prompts/b8-backend-ui-gaps.md` (cách B8 làm);
- `docs/ui/decisions.md`, các báo cáo `docs/ui/05*-report.md`;
- code liên quan trong `HomestayBooking.Web/Services/`.

Style viết như B8: primary constructor, record DTO, kiểm quyền qua `Actor` + `HomestayAccessService`/`PartnerServiceSupport`, `TimeProvider`, test theo các `*TestHost` trong `HomestayBooking.Tests/Services/`.

## Giới hạn

- **Phần 1 (backend):** chỉ sửa `Services/`, DTO trong `Models/ViewModels/` nếu DTO nằm đó, test, tài liệu.
- **Phần 2 (nối UI):** chỉ sửa controller/view/resource đúng những chỗ mà cột "Cách làm tạm" của từng gap đang làm tạm. Không làm lại màn, không đổi bố cục ngoài phần cần nối, giữ phong cách theo `DESIGN.md`.
- **Không thêm migration, không đổi schema.** Gap nào bắt buộc đổi schema thì **không làm**, ghi lý do vào báo cáo mục "Cần người dùng quyết định".
- Không sửa dữ liệu nghiệp vụ trong database bằng SQL trực tiếp.

## Việc cần làm

| # | Gap | Việc |
|---|---|---|
| 1 | **G-16** – danh mục cho trình sửa của chủ | `HomestayContentService.EditorAsync` (hoặc hàm đọc riêng, ví dụ `EditorCatalogAsync(actor)`) trả thêm danh mục **đầy đủ, đang dùng được** của tiện nghi (Id, tên, icon nếu có, cờ đang chọn) và loại phòng (Id, tên). Danh mục này **không** lọc theo homestay đang Active như `AvailabilityService.SearchAsync`. Kiểm quyền như `EditorAsync`. Đọc các hàm ghi hiện có (cập nhật tiện nghi, tạo/sửa loại phòng) để DTO khớp đúng dữ liệu chúng nhận. Test: chủ khác bị chặn; tiện nghi đang chọn được đánh dấu đúng; danh mục không bị lọc theo homestay Active. |
| 2 | **G-17** – số đơn chờ xác nhận của chủ | Thêm hàm đọc kiểm quyền theo chủ, ví dụ `OwnerBookingSummaryAsync(Actor actor)`, trả: số đơn `Deposited` đang chờ chủ xác nhận (kèm số đơn đã quá hạn xác nhận), số lượt nhận phòng hôm nay. Chủ chỉ đếm đơn thuộc homestay của mình. Đặt ở service hợp lý nhất (`OwnerScheduleService`, `BookingService` hoặc service mới cho phía chủ). Nếu tiện, thêm luôn hàm danh sách đơn của chủ để `Owner/BookingsController` thôi tự truy vấn DbContext; không bắt buộc. Test: đếm đúng, chủ khác không thấy đơn của nhau, tài khoản không phải chủ bị chặn. |
| 3 | **G-18** – thông báo của chủ không có link | Sửa nguồn tạo thông báo: `BookingService` (khoảng dòng 430–434, nhánh đặt cọc/xác nhận/hủy) và mọi chỗ `NotificationService.Add` khác trong `Services/` đang thiếu URL. Thông báo cho chủ dùng `/Owner/Bookings/Details/{bookingId}`, cho khách dùng `/Bookings/Details/{booking.Code}`, cho nhân viên dùng đúng màn của 05b nếu có. Nếu `Add` chỉ nhận một URL cho nhiều người nhận thuộc vai trò khác nhau, tách thành các lần gọi theo vai trò. **Không backfill** thông báo cũ (bảng không lưu mã đơn riêng, không đoán URL từ nội dung); ghi điều này vào báo cáo. Test: mỗi loại thông báo có URL đúng theo người nhận. |
| 4 | **G-20** – nhãn homestay/phòng/người nhận ở màn Staff | DTO của `StaffBookingService.SearchAsync/DetailsAsync`, `AvailabilityService.StaffAvailabilityAsync` trả thêm tên homestay, mã/tên phòng, tên người phụ trách (và người trong lịch sử). Bộ lọc danh sách đơn cho chọn homestay theo tên (danh sách lựa chọn từ service), không bắt gõ mã số. |
| 5 | **G-21** – phòng trống theo lô | Thêm hàm trả ma trận phòng trống theo lô (homestay × loại phòng × đêm, tối đa 31 đêm) bằng một số truy vấn cố định, không gọi lặp từng đêm. Thêm test hiệu năng theo style `AvailabilityPerformanceTests` (có thể để skip mặc định như test hiện có) cho 150 cơ sở × 31 đêm và ghi số đo vào báo cáo. |
| 6 | **G-22** – link thông báo kết quả thẩm định gửi chủ | Sửa URL tại nguồn (`OwnerRegistrationService`) sang màn của chủ (`/Partner/Status` hoặc màn Owner tương ứng), không mở quyền Staff. Không backfill. Test URL theo người nhận. |
| 7 | **G-23** – xác nhận đối soát và ngày chuyển (người dùng chốt 19/10/2026) | (a) Thêm hàm cho **Kế toán xác nhận thay chủ**, ví dụ `ConfirmOnBehalfAsync(id, reason, actor)`: chỉ Accountant (và Manager/Administrator nếu style quyền hiện có cho phép), chỉ từ `Sent`, **bắt buộc lý do**, ghi audit nêu rõ "kế toán xác nhận thay chủ", gửi thông báo cho chủ kèm URL màn đối soát của chủ. Giữ nguyên `ConfirmAsync` của chủ. (b) Mở rộng ghi nhận chuyển tiền: nhận **ngày chuyển thực tế** (lưu vào `PaidAt`; không được ở tương lai, không trước thời điểm gửi/xác nhận) và **số tiền đã chuyển**. **Không đổi schema** (bảng không có cột số tiền đã chi): số tiền nhập **phải bằng đúng `PayoutAmount`**, sai thì báo lỗi rõ để chặn gõ nhầm; **không hỗ trợ chi từng phần**. Ghi số tiền và ngày vào audit. Test: thành công, thiếu lý do, sai trạng thái, sai số tiền, ngày tương lai, chủ/nhân viên khác bị chặn. |
| 8 | **G-24** – từ chối khoản bù | Thêm hàm đọc danh sách khoản bù **đủ điều kiện từ chối** (đúng điều kiện mà `RejectAsync` đang cho phép), kèm trạng thái và đơn liên quan, quyền Accountant. Test: chỉ trả khoản đủ điều kiện; khoản Completed không có trong danh sách từ chối. |
| 9 | **G-25** – tên homestay và liên kết kỳ đối soát | `SettlementService.ListAsync/DetailsAsync` trả tên homestay (tên tại thời điểm hiện tại là đủ) và tên chủ. Các DTO/row của bốn màn kế toán bước 04 (`ReceiptRow`, `RefundRow`, `BankTransactionRow`/`TransactionProcessViewModel`, cọc bổ sung) trả thêm `SettlementId` khi đơn **thực sự** nằm trong một `SettlementItem` (không đoán theo ngày). Đặt ở service hiện đang dựng các row đó. |
| 10 | **G-19** và gap khác còn mở | G-19 (gửi lời mời qua SMS/Zalo) **không làm**: kênh gửi thực tế chốt khi triển khai. Gap mới khác do 05d ghi: làm nếu không cần đổi schema và nằm trong phạm vi service; không thì ghi lý do. |

## Phần 2 – Nối UI

- **G-16:** tab Tiện nghi của trình sửa chủ (`/Owner/Homestays/Edit/{id}`) cho chọn/bỏ tiện nghi và lưu qua hàm ghi có sẵn. Form loại phòng cho chọn danh mục; tạo loại phòng mới nếu service ghi đã hỗ trợ. Bỏ trạng thái "chưa hỗ trợ".
- **G-17:** `Owner/Dashboard` hiện số đơn chờ xác nhận (và số quá hạn nếu có) bằng `_StatCard`, link tới `/Owner/Bookings?status=Deposited`. Mục "Việc cần xử lý" hiện số tương ứng.
- **G-18:** chuông và trang `/Notifications` của chủ mở đúng màn với thông báo mới. Thông báo cũ không có URL vẫn hiển thị bình thường, không lỗi.
- **G-20:** danh sách/chi tiết đơn Staff và bảng phòng trống hiện tên homestay, phòng, người phụ trách; bộ lọc đơn chọn homestay theo tên. Phòng trống dùng hàm theo lô của G-21.
- **G-22:** thông báo kết quả thẩm định mới mở đúng màn của chủ.
- **G-23:** chi tiết đối soát kế toán có nút "Xác nhận thay chủ" (chỉ khi `Sent`, form lý do bắt buộc, hộp xác nhận); form "Ghi nhận đã chuyển tiền" có ô ngày chuyển và ô số tiền (điền sẵn số phải trả, báo lỗi nếu khác), giữ mã tham chiếu ngân hàng. Bỏ đoạn giải thích "ngày ghi nhận tự lưu".
- **G-24:** màn Khoản bù có danh sách/tab "Có thể từ chối" với form lý do + hộp xác nhận `danger`.
- **G-25:** danh sách và chi tiết đối soát hiện tên homestay thay cho "Homestay #id"; bốn màn kế toán bước 04 có lối tắt "Xem kỳ đối soát" khi có `SettlementId`.
- **Gap khác:** nối theo cột "Cách làm tạm" của từng gap.

### Sửa các điểm yếu ghi nhận khi Claude review (19/10/2026)

1. **Dashboard chủ trên điện thoại (375px):** 4 thẻ số liệu xếp mỗi thẻ một hàng, trang quá dài. Cho `_StatCard` trong hàng số liệu xếp 2 cột ở điện thoại (sửa ở component/CSS dùng chung, kiểm cả dashboard các vai trò khác).
2. **Bảng giao dịch kế toán (`/Accounting/Transactions`) 1280px:** cột thời gian xuống dòng giữa giờ và ngày ("09:01 / 21/08/2026"). Giữ giờ và ngày trên cùng một dòng (ví dụ `NoWrap` của cột hoặc định dạng gọn hơn).
3. **Chi tiết đối soát kế toán:** ô "Khoản bù chi riêng" để trống thay vì `0đ` — hiện `<money>` 0 nhất quán với các cột khác. Trạng thái rỗng của bảng khoản bù đang là một thẻ lồng trong thẻ bảng — dùng biến thể rỗng gọn trong bảng của `_DataTable`/`_EmptyState` (sửa ở component nếu là lỗi chung).
4. **Danh sách đơn Staff:** cột "Mã homestay" hiện "#6" và bộ lọc "Mã homestay" bắt gõ số (xử lý cùng G-20 ở trên).
5. **Báo cáo Admin (`/Admin/Reports`) khi khoảng ngày không có dữ liệu:** các biểu đồ vẫn vẽ trục 0–1 rỗng và bảng toàn 0. Khi không có dữ liệu thì hiện `_EmptyState` thay cho biểu đồ rỗng (giữ thẻ số liệu). Kiểm thêm: seed có dữ liệu năm 2026 (77 đơn) nhưng năm 2027–2028 hiện 0; ghi rõ trong báo cáo dữ liệu seed thực có ở những năm nào, không bịa dữ liệu.
6. **Dashboard Admin:** nút "Xem báo cáo" dùng icon dấu cộng (nghĩa "tạo mới"); đổi sang icon phù hợp hành động xem (ví dụ `bar-chart` hoặc `arrow-right`). Trạng thái rỗng của bảng "Hợp đồng sắp hết hạn" cũng là thẻ lồng trong thẻ: dùng cùng cách sửa với điểm 3.

### Dữ liệu thử để nghiệm thu 05d (người dùng chốt 19/10/2026)

Báo cáo 05d chưa nghiệm thu được **tạo hợp đồng** và **duyệt nội dung thành công** vì thiếu dữ liệu đủ điều kiện. Ở bước này **được phép** chuẩn bị dữ liệu thử trong database Development, nhưng **chỉ qua luồng thật của app** (ví dụ chủ `0912000001` đổi địa chỉ một homestay qua trình sửa để đưa nó về chờ duyệt; hoàn tất một hồ sơ đối tác thử qua `/Partner` → thẩm định → kích hoạt để có chủ đủ điều kiện ký hợp đồng). Không sửa dữ liệu bằng SQL trực tiếp. Sau đó đăng nhập `QL001` tạo/gia hạn một hợp đồng và duyệt một nội dung. Ghi mọi dữ liệu thử vào báo cáo. Giữ đơn demo `FJ261019-001`.

Cập nhật `docs/ui/backend-gaps.md`: mục nào xong ghi "Xong (B9)" kèm tên method. Thêm mục 7.11 "Backend bổ sung cho giao diện (B9)" vào `QUY_TRINH_BTL.md`. Ghi quyết định vào `docs/ui/decisions.md`.

## Kiểm tra trước khi báo xong

- `dotnet build -c Release -p:UseSharedCompilation=false` không cảnh báo mới. `dotnet test HomestayBooking.sln -c Release --no-build` pass toàn bộ, báo số test trước/sau.
- Mỗi method mới có ít nhất 1 test thành công và 1 test bị chặn quyền (nếu có kiểm quyền).
- `git diff --stat` không có file trong `Migrations/`.
- Chạy app riêng cổng 5012 (không tắt app cổng 5011; dừng app của mình bằng đúng PID; không `pkill -f`/`killall` mẫu ngắn; SQL Server ở container `sql-homestay`).
  - Đăng nhập chủ `0912000001` (mật khẩu `Fuji@2026`) trên 375px: sửa tiện nghi một homestay, xem số đơn chờ trên dashboard (thẻ 2 cột).
  - Đăng nhập `KT001`: xác nhận thay chủ một bảng đối soát `Sent` (có lý do), ghi nhận chuyển tiền có ngày thực tế; thử nhập sai số tiền thì bị chặn; từ chối một khoản bù đủ điều kiện (nếu không có dữ liệu thì chứng minh bằng test tích hợp, không chèn dữ liệu tài chính giả bằng SQL).
  - Đăng nhập `NV001`: danh sách đơn hiện tên homestay, lọc theo tên homestay; bảng phòng trống hiện tên.
  - Tạo một thông báo mới qua luồng thật (ví dụ khách `0987000001` đặt và đặt cọc qua `SimulateBank`), mở thông báo từ chuông của chủ thì đến đúng đơn.
- `python3 -I tools/ui-crawl.py http://localhost:5012`: không có trang 500.
- **Giữ đơn demo `FJ261019-001`.** Dữ liệu thử mới ghi vào báo cáo.

## Commit

1. `feat(backend): danh mục trình sửa, tổng hợp đơn của chủ và link thông báo (B9)` (backend + test)
2. `feat(ui): nối tiện nghi, số đơn chờ và link thông báo của chủ (B9)` (phần nối UI)

Có thể tách thêm commit theo nhóm (Staff G-20/21/22, Kế toán G-23/24/25, điểm yếu giao diện), mỗi commit build và test pass.

## Báo cáo rồi dừng

Viết `docs/ui/b9-backend-report.md`:
- method mới, test mới, số test trước/sau;
- chỗ UI đã nối;
- gap chưa làm và lý do;
- mục "Cần người dùng quyết định".
