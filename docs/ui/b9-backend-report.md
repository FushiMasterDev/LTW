# B9 — Backend bổ sung và nối giao diện bước 05

Ngày 19/10/2026, nhánh `feature/foundation`. Không đổi nhánh, không push, không migration/schema mới, không SQL ghi dữ liệu nghiệp vụ, không tạo ảnh mới. App QA riêng cổng 5012; ảnh/log/script chỉ ở `/private/tmp/fuji-b9/` và `.playwright-cli/`.

## Backend và test

| Gap | Method / dữ liệu |
|---|---|
| G-16 | `HomestayContentService.EditorAsync`: `Amenities` (Id, tên, icon, Selected), `Categories` đầy đủ. Bảng không có cờ Active nên dùng mọi mục tồn tại, đúng điều kiện service ghi; không lọc theo homestay Active. |
| G-17 | `OwnerScheduleService.OwnerBookingSummaryAsync`: Deposited, quá hạn ConfirmDeadlineAt, lượt Confirmed nhận hôm nay theo giờ VN, chỉ đúng chủ Active/không Employee. Giữ cách đếm lượt đến như lịch hiện có. |
| G-18 | `BookingService.NotifyAsync/AlertOverdueConfirmationsAsync`, `ReceiptReviewService`, `BookingChangeRequestFinance`, `VoucherService`: bổ sung URL; tách người nhận khách/chủ/nhân viên khi đường dẫn khác nhau. |
| G-20 | `StaffBookingService.SearchAsync/DetailsAsync` nạp homestay, loại/phòng, người phụ trách và actor lịch sử kể cả đã nghỉ; `HomestaysAsync` trả danh mục tên mọi trạng thái; `AvailabilityService.FreeRoomsAsync` trả Id + mã phòng. |
| G-21 | `AvailabilityService.StaffNightAvailabilityAsync`: ma trận tối đa 31 đêm, **4 truy vấn dữ liệu + 1 truy vấn quyền** không phụ thuộc số đêm/cơ sở; điều kiện bán/hợp đồng, giá và RoomNights đúng từng đêm, không cache. |
| G-22 | `OwnerRegistrationService.DecideAsync` gửi chủ `/Partner/Status`; thông báo nội bộ giữ màn Staff. |
| G-23 | `SettlementService.ConfirmOnBehalfAsync`: chỉ Accountant, từ Sent, lý do 1–500, audit ghi rõ kế toán xác nhận thay chủ và thông báo chủ. Overload `RecordPaidAsync(id, bankRef, amount, paidAt, actor)` nhận đúng PayoutAmount, thời điểm UTC không tương lai/không trước SentAt hoặc ConfirmedAt; audit số tiền/ngày. Giữ ConfirmAsync của chủ và overload cũ cho caller hiện có. Không chi từng phần. |
| G-24 | `CompensationService.RejectableAsync`: đúng Compensation Submitted có đơn chưa Completed, quyền Accountant, cùng điều kiện RejectAsync. |
| G-25 | `SettlementService.ListAsync/DetailsAsync` nạp Homestay/Owner/Person. `AccountingService.ReceiptsAsync/RefundsAsync/TransactionsAsync/TransactionAsync/AdditionalDepositsAsync` kiểm Actor và trả liên kết từ **SettlementItem thật**. Chuyển phép dựng row hiện có từ controller vào service. |
| G-26 | `UpdateLocationAsync`/`HomestayDocumentService.UploadAsync` lưu snapshot editor trước khi Active vào PendingReview trong JSON `AuditLog.OldValues` hiện có. `PendingDetailsAsync` trả ChangeReason, BeforeContent, Changes và LegacyDocuments còn hạn của hồ sơ Approved cùng chủ. Diff so với **nội dung hiện tại**, kể cả sửa tiếp trong lúc chờ; audit cũ thiếu snapshot chỉ trả trường thực có. Snapshot là thông tin/danh sách tệp, không sao lưu byte file. |
| G-27 | `AccountQuery.Position` lọc tại DB trước phân trang; `AccountAdminService.DetailsAsync` kiểm Management; bỏ vòng đọc mọi trang nhân viên ở UI. |
| G-15 | `ReviewService.ListAsync` nạp Booking/Customer/Person để `_ReviewCard` hiện tên có thật theo model hiện có, giữ encode/fallback. |

Test mới: `B9OwnerTests`, `B9StaffTests`, `B9FinanceTests`, `B9AdminTests`, `B9WorkspaceUiTests`. Mở rộng `PartnerReviewTests` (URL ba kết quả), `HomestayMediaPolicyTests` (snapshot giấy pháp lý), `AvailabilityPerformanceTests` (ma trận), các test UI dashboard/kế toán/báo cáo. Mỗi public method mới có đường thành công và chặn quyền; test đọc tài chính kiểm SettlementId thật/không có item; test chuyển tiền kiểm lý do, trạng thái, sai tiền, tương lai, trước gửi/xác nhận và actor không hợp lệ. G-26 kiểm snapshot, sửa tiếp khi PendingReview, giấy legacy còn hạn và audit cũ null.

- Baseline ngoài sandbox: **1.689 pass + 1 skip, 0 fail** (1.690 tổng), `baseline-test-unrestricted.log`.
- Backend đầu tiên: **1.715 pass + 1 skip, 0 fail**, `backend-tests-full.log`; build 0 warning/error. Sau rà quyền đọc phòng lịch sử: 26/26 B9 pass, `backend-tests-reviewed.log`.
- UI trọng tâm: **40/40 pass**, `ui-tests-focused-2.log`. Sau bổ sung POST từ chối khoản bù: **1.719 pass + 1 skip, 0 fail** (1.720 tổng), `final-tests.log`.
- Lượt xác minh sau snapshot G-26 và giữ tab từ chối: xem kết quả cuối ở phần kết thúc. Tổng tăng **30 test case**, không tăng skip.
- Một lần baseline trong sandbox lỗi TLS macOS (`bad certificate format`); đã dừng đúng PID của lần đó. Các kết quả nghiệm thu dùng lần chạy ngoài sandbox có exit 0, không dùng lần lỗi/đang chạy để khẳng định pass.

## Hiệu năng

`RUN_PERF_TESTS=1 dotnet test HomestayBooking.sln -c Release --no-build --filter FullyQualifiedName~AvailabilityPerformanceTests --logger 'console;verbosity=detailed'`: pass. Dữ liệu ConstraintTest: **150 homestay, 750 loại phòng, 1.500 phòng, 108.900 khóa, 326.700 đêm bận**; transaction đã rollback, kiểm lại Homestays = 0.

Chậm nhất trong 3 lần: ma trận 150 cơ sở × 31 đêm **317 ms**; đọc trống xuyên khoảng cũ **3.252 ms**; tìm kiếm có ngày **352 ms**. Ma trận trả đủ 23.250 dòng. Test SQLite đếm query kiểm 1 và 31 đêm cùng số truy vấn, đối chiếu từng đêm với API cũ. Đây là phép đo tại máy/SQL Server hiện tại, không phải cam kết tải đồng thời. Log `backend-performance.log`.

## UI đã nối và điểm yếu đã sửa

- Chủ: tiện nghi chọn/bỏ và lưu qua UpdateAsync; loại phòng chọn catalog, tạo với đủ ba giá qua CreateTypeAsync. Dashboard có thẻ chờ xác nhận/link Deposited và số quá hạn trong thẻ/công việc.
- Chuông/trang Notifications giữ thông báo cũ không URL bình thường; thông báo mới mở đúng khu vực. **Không backfill** thông báo cũ, không đoán URL từ nội dung tự do.
- Staff: tên cơ sở, phòng, người phụ trách/lịch sử; dropdown cơ sở theo tên; chọn phòng bằng mã thật ở chi tiết/trùng phòng; phòng trống gọi API ma trận một lần.
- Kế toán: tên cơ sở/chủ ở đối soát; xác nhận thay chủ có lý do + modal; tiền chuyển điền sẵn, ngày/giờ thực tế theo VN → UTC, giữ bankRef. HTML number parse invariant để VI/EN không lệch dấu thập phân. Tab Có thể từ chối có lý do + modal danger và giữ tab sau POST. Bốn màn tài chính có shortcut khi có SettlementId; Process giao dịch dùng cùng row.
- Admin: Position/Details API mới; duyệt có lý do, snapshot/diff thật và legacy hợp lệ. Không thêm lý do bắt buộc khi ApproveAsync chưa có luật đó.
- CSS StatCard dùng chung hai cột ở 375px, kiểm cả bốn dashboard; Transactions dùng NoWrap cho giờ/ngày ở 1280px; khoản bù không có hiện 0đ; DataTable/EmptyState.Compact bỏ thẻ rỗng lồng; báo cáo không dữ liệu giữ StatCard, hiện EmptyState thay trục/bảng toàn 0; vẫn giữ bảng nếu chỉ tiêu khác có dữ liệu thật. PageHead.ActionIcon cho Admin dùng bar-chart.

42 lượt chụp theo vai trò/màn × 375/768/1280 × sáng/tối: HTTP 200, **0 tràn ngang**, các dashboard 375px có hai thẻ mỗi hàng (rộng 163,5px, x=16/195,5). Đã xem ảnh desktop giao dịch, báo cáo mobile tối và trình sửa phòng mobile. Log `capture-{owner|staff|kt|admin}.log`; ảnh cùng tiền tố. Thêm ảnh form/lỗi tiền/modal/diff/chuông. Bảng #52 có 0đ, empty border 0px/shadow none; cột thời gian nowrap. Mười lượt năm 2027/2028 × năm tab báo cáo: 200, 0 chart rỗng, 0 overflow (`admin-empty-reports.log`).

`python3 -I tools/ui-crawl.py http://localhost:5012`: **840 lượt trang, 0 trang 500**, chỉ 4 URL 404 đã biết: `/Bookings/Create` thiếu roomTypeId và ba mã mẫu UI Kit `FJ2611-0012/0013/0014`. Log `crawl.log`. Kiểm lại sau snapshot xem phần kết thúc.

## Dữ liệu Development qua luồng thật

| Dữ liệu | Thao tác và trạng thái |
|---|---|
| Homestay #6 — Ba Đình Green Villa | Chủ 0912000001 chọn tiện nghi #3 rồi bỏ lại; tập lựa chọn cuối như ban đầu. Đổi tạm địa chỉ thêm nhãn B9 → QL001 duyệt thành công → khôi phục **15 Đội Cấn, Ba Đình, Hà Nội** → duyệt lại Active. Không đổi giá/phòng/đơn cũ. Bổ sung kiểm snapshot mới được ghi ở phần kết thúc. |
| Hồ sơ #1003 / homestay #1003 | Tự đăng ký qua `/Partner`, chủ thử **0977091919 — Chủ kiểm thử B9**, cơ sở **Cơ sở thử B9 - Không mở bán**. Địa chỉ minh họa Ba Đình, ngân hàng/tài khoản đánh dấu minh họa. Tải lại file demo có sẵn `images/demo/sofa-corner-sm.webp` cho ba loại giấy tờ thử, Id **1083/1084/1085**, hạn 31/12/2028; không phải chứng từ thật, không tạo ảnh. Gửi → QL001 nhận → duyệt Approved. Chủ tự có tài khoản Active từ luồng đăng ký; thông báo mới mở đúng `/Partner/Status`. Homestay chưa có phòng/giá, giữ PendingReview và **không mở bán**. |
| Hợp đồng #1002 | QL001 tạo **B9-DEV-20261019-1003** cho cơ sở thử #1003, 19/10/2026–19/10/2027, 10%; gia hạn đến **20/10/2027**, giữ 10%. Có audit; không chấm dứt hay sửa hợp đồng seed. |
| Đối soát #52 | Dữ liệu nháp demo từ 05c, Ba Vì Mountain Lodge, kỳ 2 tháng 9/2026, đơn FJ260902-001. KT001 gửi → xác nhận thay chủ với lý do **“B9: kế toán xác nhận thay chủ sau khi đối chiếu số liệu demo.”** → thử 120.001đ bị chặn → ghi đúng **120.000đ**, PaidAt **19/10/2026 21:10:49 VN** (14:10:49 UTC), bankRef **B9-DEV-SETTLEMENT-52**, trạng thái Paid. Đây là ghi nhận mô phỏng Development, không gọi ngân hàng thật. |
| Đơn mới #3017 — FJ261019-005 | Khách **0987000001**, cơ sở #6/loại #14, 20–21/12/2026, 2 khách; tổng **905.000đ**, cọc **272.000đ**. SimulateBank của QL001 nhận đủ tiền → Deposited. Chủ mở từ chuông đến đúng **/Owner/Bookings/Details/3017** (`owner-bell-opened-booking.log`). Giữ đơn thử, không hủy để tránh sinh khoản hoàn không cần thiết. |
| Đơn demo #3013 — FJ261019-001 | **Giữ nguyên Confirmed**, 15–17/12/2026, tổng **1.800.000đ**; chỉ đọc, không đổi trạng thái/phòng/người phụ trách. |
| Khoản bù | Development không có khoản đủ điều kiện từ chối. Đã kiểm tab rỗng; chứng minh thành công qua `B9FinanceTests.RejectableCompensationsUseExactlyRejectConditionsAndHideCompleted` (SQLite có CHECK) và `B9WorkspaceUiTests.RejectableCompensationTabPostsReasonAndRemovesProcessedRow` (POST thật trên test host). Không chèn khoản tài chính giả vào database app. |

Trước B9: **77 đơn**, toàn bộ CreatedAt năm **2026**; 2027/2028 không có đơn. Sau thêm FJ261019-005: **78 đơn năm 2026, 0 năm 2027, 0 năm 2028**. Không dùng hợp đồng/ngày lễ năm 2027–2028 để bịa đơn/doanh thu. `seed-before.log`, `final-data-read.log`. Shortcut thật đã mở/đọc: giao dịch FJ260809-001 → đối soát #42; các bill Approved có #47/#48/#33/#31/#30/#29 (`settlement-shortcuts-browser.log`).

## Gap chưa làm và Cần người dùng quyết định

- **G-19 không làm theo yêu cầu**: giữ chuyển liên kết mời thủ công, chốt SMS/Zalo thật khi triển khai. Không gửi tin ngoài app.
- Audit trước B9 thiếu snapshot không thể phục dựng đầy đủ; chỉ hiện trường thực có. Nếu cần lịch sử cũ đầy đủ, cần nguồn dữ liệu lịch sử/backup có thẩm quyền. Không backfill giả.
- Nếu cần giữ byte ảnh/giấy tờ đã xóa để xem lại bản trước, cần chốt kho lưu và thời hạn giữ file. B9 lưu metadata trong AuditLog hiện có, không đổi cơ chế xóa file/schema.
- Nếu muốn bắt buộc lý do khi **duyệt** nội dung, cần chốt luật riêng. B9 giữ ApproveAsync hiện hành; từ chối/mở lại và xác nhận thay chủ đã lưu lý do đúng yêu cầu.

## Kết thúc và kiểm chứng cuối

Phiên Codex hết hạn mức ChatGPT trong vòng kiểm cuối (sau khi đã commit phần backend `8b984e8`). Claude làm nốt vòng kiểm và commit phần nối UI:

- `dotnet build -c Release -p:UseSharedCompilation=false`: 0 cảnh báo, 0 lỗi. `dotnet test HomestayBooking.sln -c Release --no-build`: **1.719 pass + 1 skip, 0 fail** (1.720 tổng).
- `rg`: 0 `style="` trong Views/Areas, 0 mã màu ngoài `tokens.css`; không có file trong `Migrations/`, `Data/`, `Models/Entities/`.
- `python3 -I tools/ui-crawl.py http://localhost:5012`: 840 trang, 7 vai trò, không có 500; đúng 4 lỗi 404 đã biết.
- Chụp lại các điểm yếu đã sửa: dashboard chủ 375px thẻ 2 cột có "Chờ xác nhận 3 · 2 đơn quá hạn"; danh sách đơn Staff hiện tên homestay/phòng/người phụ trách, lọc homestay theo tên; chi tiết đối soát #52 hiện "Ba Vì Mountain Lodge · Lê Văn Hùng", khoản bù `0đ`, trạng thái rỗng không lồng thẻ, lịch sử có "Kế toán xác nhận thay chủ"; báo cáo 2028 hiện trạng thái rỗng thay biểu đồ trống. Không lỗi JavaScript.
- Còn một điểm nhỏ: mục "Đã chuyển tiền" trong lịch sử đối soát hiện nguyên chuỗi audit ("Số tiền: 120000; ngày chuyển UTC: 2026-10-19T14:10:49…") thay vì định dạng tiền/ngày giờ Việt Nam — để bước 06 QA.
- App 5012 của Claude đã dừng đúng PID; trình duyệt kiểm thử Codex để lại đã đóng. Không đụng app 5011, container `sql-homestay` vẫn chạy.
