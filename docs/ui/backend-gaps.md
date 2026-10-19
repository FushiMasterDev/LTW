# Backend gaps cho giao diện

Lập ở Bước 0 (17/10/2026), sau khi đọc method public trong `HomestayBooking.Web/Services/`. Cập nhật B9 (19/10/2026): xem trạng thái riêng từng gap; G-19 hoãn kênh thật, G-26 chỉ có phần dữ liệu audit có thể kiểm chứng.

Backend đã có service cho đủ FR-01…FR-27 sau B7. Zalo/SMS thật (FR-04, FR-23) là phần rút gọn đã chấp thuận nên không ghi ở đây. Các gap dưới đây là **dữ liệu đọc hoặc quyền** mà màn cần nhưng service chưa trả, cộng thêm một lỗi đường dẫn trong thông báo.

Quy tắc của các bước UI: không sửa `Services/`. Gặp gap thì làm phần màn không phụ thuộc gap, phần còn lại để trống hoặc ẩn, rồi ghi rõ trong báo cáo bước.

Mức độ:
- **Chặn**: không làm được màn nếu thiếu.
- **Một phần**: làm được màn nhưng thiếu một chức năng.
- **Nhỏ**: có cách làm tạm hợp lệ, ghi vào `decisions.md`.

| Mã | Mức | Màn / FR | Bước | Thiếu gì | Hiện có | Cách làm tạm trong UI | Đề xuất backend | Trạng thái |
|---|---|---|---|---|---|---|---|---|
| G-01 | Chặn | `NotificationBell`; trang Thông báo của chủ (FR-15); cảnh báo đơn quá hạn qua chuông (FR-11) | 02, 05a, 05b | Đọc thông báo: danh sách theo tài khoản, số chưa đọc, đánh dấu đã đọc | `NotificationService` chỉ có `Add(...)`. Bảng `Notifications` có `IsRead`, `Url`, `CreatedAt` | Chưa làm chuông và trang thông báo. FR-11 trên Staff Dashboard lọc `StaffBookingService.SearchAsync(Status = Deposited)` rồi so `Booking.ConfirmDeadlineAt` với giờ hiện tại | `NotificationService.ListAsync(accountId, page)`, `UnreadCountAsync(accountId)`, `MarkReadAsync(id, accountId)`, `MarkAllReadAsync(accountId)` | Xong (B8): `NotificationService.ListAsync/LatestAsync/UnreadCountAsync/MarkReadAsync/MarkAllReadAsync` |
| G-02 | Chặn | Homestay của tôi + 6 tab sửa (FR-13); chọn homestay cho Lịch phòng (FR-14) và Đánh giá (FR-06 phía chủ) | 05a | (a) Danh sách homestay của chủ đang đăng nhập. (b) Dữ liệu đổ vào form sửa: loại phòng, phòng, giá hiện hành và giá tương lai theo `EffectiveFrom`, ảnh, tiện nghi đang chọn, chính sách hủy đang áp dụng, giấy tờ | Có đủ hàm **ghi**. `HomestayAccessService.AuthorizeAsync(id, actor, readOnly: true)` chỉ trả entity `Homestay` trần (không Include). Danh sách homestay hiện do `Owner/DashboardController` tự truy vấn (code cũ) | (a) dùng lại truy vấn của `Owner/DashboardController`. (b) chưa làm, bỏ qua tab sửa | `HomestayContentService.OwnerHomestaysAsync(actor)` và `EditorAsync(id, actor)`, trả DTO đủ các phần trên, kiểm quyền bằng `HomestayAccessService` | Xong (B8): `HomestayContentService.OwnerHomestaysAsync`, `EditorAsync` |
| G-03 | Nhỏ | Lịch nhận–trả (FR-16) | 05a | `OwnerScheduleService.ReadAsync` cần `ownerId`, service không tự suy từ `actor` | Bảng `Owners` theo `PersonId` (`Owner/DashboardController` đang tra như vậy) | Controller tra `Owners.Id` theo `User.PersonId()`. Đây là tra định danh, không phải truy vấn nghiệp vụ; ghi vào `decisions.md` | Thêm overload `ReadAsync(actor, from, through)` | Xong (B8): `OwnerScheduleService.ReadAsync(actor, from?, through?, homestayId?)` |
| G-04 | Một phần | Giao đơn cho nhân viên khác (FR-09) | 05b | Danh sách người nhận hợp lệ (Consultant/Manager đang làm việc) cho cả Tư vấn | `StaffBookingService.AssignAsync` có kiểm người nhận. `AccountAdminService.ListAsync` chỉ cho Manager/Administrator gọi | Manager/Administrator: lấy từ `AccountAdminService.ListAsync(Role = Employee, Status = Active)` rồi lọc chức vụ. Tư vấn: chỉ hiện nút "Nhận đơn" (giao cho chính mình) | `StaffBookingService.AssigneesAsync(actor)` | Xong (B8): `StaffBookingService.AssigneesAsync` |
| G-05 | Nhỏ | Lịch tháng dùng chung chủ (FR-14) và nhân viên (FR-08) theo `ui-rebuild.md` | 05a, 05b | `RoomBlockService.CalendarAsync` gọi `AuthorizeAsync(allowConsultant: false)`, nên Tư vấn bị chặn | `AvailabilityService.StaffAvailabilityAsync` trả số phòng trống theo loại phòng (≤ 31 đêm) | FR-08 dùng bảng số phòng trống. Lịch tháng chỉ có ở Owner, và với Manager/Administrator | Cho Tư vấn đọc `CalendarAsync` ở chế độ chỉ xem | Không làm (quyết định 17/10): Tư vấn dùng bảng `StaffAvailabilityAsync`, lịch tháng chỉ Chủ + Quản lý/Quản trị |
| G-06 | Một phần | Bộ lọc tìm homestay (FR-01); thẻ homestay có điểm đánh giá (DESIGN.md) | 03 | (a) Danh sách loại phòng và tiện nghi cho bộ lọc. (b) Điểm và số đánh giá trên `HomestayCard` | `HomestaySearchViewModel` chỉ có `Areas`. `HomestayCard` không có `Rating`/`ReviewCount` (chỉ `FeaturedHomestay` có). `SearchAsync` đã nhận `CategoryId`, `AmenityIds`, `Sort` | (a) ghi `decisions.md` rồi để controller đọc `RoomCategories`/`Amenities` (danh mục, giống CRUD Admin), hoặc chỉ làm lọc giá + sắp xếp. (b) thẻ ở trang tìm kiếm chưa hiện điểm | `SearchAsync` trả thêm danh mục lọc; thêm `Rating`, `ReviewCount` vào `HomestayCard` (lấy từ `ReviewRatings.Summaries`) | Xong (B8): `HomestaySearchViewModel.Categories/Amenities`; `HomestayCard.Rating/ReviewCount` |
| G-07 | Một phần | Lịch phòng trống trên trang chi tiết (FR-02) | 03 | Ngày kín theo loại phòng, cho khách xem công khai (API `/api/rooms/{id}/availability` trong `QUY_TRINH_BTL.md` mục 8 chưa có) | `HomestaysController.Details` có số phòng trống cho khoảng ngày đã chọn | Chỉ hiện "Còn N phòng" sau khi chọn ngày (đã có); Flatpickr không bôi ngày kín | `AvailabilityService.BlockedDatesAsync(roomTypeId, from, to)` | Xong (B8): `AvailabilityService.BlockedDatesAsync(roomTypeId, from, to)` (≤ 92 đêm) |
| G-08 | Lỗi | Link trong thông báo của khách khi xử lý trùng phòng (FR-10) | — | `OverbookingService` dòng 73, 108, 123 ghi `/Bookings/Details/{id}` bằng **Id số** (hoặc `replacement.Id`). `BookingsController.Details` lại tìm theo **mã đơn** nên trả 404 | — | Không xử lý được ở UI | Đổi sang `booking.Code` / `replacement.Code` | Xong (B8, commit 3505966): link theo mã đơn; đổi phòng gửi chủ `/Owner/Bookings/Details/{id}` |
| G-09 | Nhỏ | Form viết đánh giá trên chi tiết đơn (FR-06) | 03 | Đơn này đã có đánh giá chưa (để ẩn form) | `ReviewService.CreateAsync` báo lỗi khi gửi lần hai (UNIQUE `BookingId`). `BookingDetailsViewModel` không có cờ này | Hiện form khi `Status == Completed`; gửi lần hai thì hiện lỗi của service qua `_Alerts` | `ReviewService.ForBookingAsync(bookingId)` hoặc cờ `HasReview` trong view model | Xong (B8): `BookingDetailsViewModel.HasReview` |
| G-10 | Nhỏ | Nút "Lập bảng đối soát gần nhất" (FR-18) | 05c | `05c-ke-toan.md` bảo gọi `GenerateRecentAsync()`, nhưng hàm này **không nhận `Actor`** (hàm của job, không kiểm quyền) | `GeneratePeriodAsync(period, actor)`, `GenerateAsync(homestayId, period, actor)`, `SettlementCalculator.PeriodOf(date)` | Nút gọi `GeneratePeriodAsync` với kỳ gần nhất đã hết grace. Không gọi `GenerateRecentAsync` từ controller | Không cần sửa backend, chỉ sửa prompt 05c | Xong: sửa prompt `05c` sang `GeneratePeriodAsync`, không sửa backend |
| G-11 | Chặn | Danh sách đối tác và hợp đồng, chi tiết hợp đồng (FR-19); "hợp đồng sắp hết hạn" trên Dashboard Admin | 05d | Hàm đọc danh sách/chi tiết hợp đồng, kèm homestay, chủ, hoa hồng, ngày hết hạn | `ContractService` chỉ có `CreateAsync`, `ExtendAsync`, `TerminateAsync`, `ExpireAsync`. Thông báo hết hạn trỏ tới `/Staff/Contracts/{id}` | Chưa làm màn danh sách. Không có Id thì cũng không gọi được form gia hạn/chấm dứt | `ContractService.ListAsync(query, actor)`, `DetailsAsync(id, actor)`. Chốt luôn area (Staff hay Admin) cho URL thông báo | Xong (B8): `ContractService.ListAsync/DetailsAsync`; URL thông báo `/Admin/Contracts/{id}` |
| G-12 | Nhỏ | Form tạo nhân viên (FR-21) | 05d | `05d-quan-ly.md` có ô "phòng ban", nhưng `EmployeeAccountInput` không có `Department` (cột `Employees.Department` thì có) | `CreateEmployeeAsync(EmployeeAccountInput, actor)` | Bỏ ô phòng ban khỏi form | Thêm `Department` vào `EmployeeAccountInput` và `AccountSummary` | Xong (B8): `EmployeeAccountInput.Department`, `AccountSummary.Department` |
| G-13 | Chặn (với Tư vấn) / Một phần | Duyệt nội dung homestay (FR-27) | 05b | (a) `PendingAsync`, `ApproveAsync`, `RejectAsync` chỉ cho Manager/Administrator, trong khi FR-27 ghi tác nhân là "Nhân viên" và `05b-nhan-vien.md` kiểm tra bằng `NV001` (Tư vấn). (b) `PendingHomestay` trả `Homestay` không Include, nên trang so sánh thiếu ảnh/giá/loại phòng | `HomestayReviewService.PendingAsync/ApproveAsync/RejectAsync/ResumeAsync` | Màn chỉ mở cho Manager/Administrator (kiểm tra bằng `QL001`). Trang duyệt hiện mô tả/địa chỉ/liên hệ có trong entity; phần ảnh/giá để G-02 | Hoặc chốt lại FR-27 chỉ dành cho Quản lý (sửa prompt 05b), hoặc mở quyền đọc cho Tư vấn. Thêm DTO nội dung đầy đủ cho `PendingAsync` | Xong (B8): (a) chốt chỉ Quản lý/Quản trị duyệt, màn ở khu Admin (05d); (b) `HomestayReviewService.PendingDetailsAsync` |
| G-14 | Nhỏ | View Component `Sidebar` có số đếm | 01 | Đếm yêu cầu đổi/hủy đang `Pending` | `_DashboardLayout` đang `@inject ApplicationDbContext` và tự đếm. `AccountingService.PendingAdditionalDeposits()` có sẵn cho số cọc bổ sung | Bước 1 chuyển nguyên phép đếm cũ vào View Component, ghi `decisions.md` (giữ hành vi, không thêm truy vấn mới) | `BookingChangeRequestService.PendingCountAsync()` | Xong (B8): `BookingChangeRequestService.PendingCountAsync` |

## Không phải gap

Các mục sau đã có service, không cần ghi gap:
- **FR-03**: đặt phòng không đăng nhập.
- **FR-07**: tạo khách mới theo SĐT khi đặt hộ, đã có ở `StaffBookingService.CreateForCustomerAsync` qua `GuestCustomerService`.
- **FR-26**: danh sách/chi tiết hồ sơ thẩm định, đã có `OwnerRegistrationQueryService.ListAsync/DetailsAsync/OpenDocumentAsync`.
- **FR-20**: báo cáo `ReportService` đủ 5 nhóm.
- **FR-21**: tài khoản, trừ G-12.
- **FR-25**: đăng ký/nộp/kích hoạt/xem hồ sơ của tôi.

`ui-rebuild.md` từng ghi FR-03, FR-07, FR-26 là gap đã biết. Ba mục này đã được B7 làm xong (`dd1e8ba`).

## G-15 — Bổ sung từ bước 03y (18/10/2026)

`ReviewService.ListAsync` chưa nạp tên hiển thị của người viết (không Include Booking/Customer). Yêu cầu avatar theo chữ cái đầu tên vì vậy chỉ thực hiện khi tên đã có trong model; với dữ liệu hiện tại, `_ReviewCard` dùng nhãn “Khách đã lưu trú” và chữ K, không dựng tên giả hoặc gọi DbContext trong view. Đây là gap nhỏ về trình bày, không chặn đặt phòng. Một bước backend sau có thể trả tên hiển thị đã được phép công khai trong DTO đánh giá; bước 03y không sửa service/entity/controller để lấy thêm tên.

## G-16 — Danh mục đầy đủ cho trình sửa Owner (05a, 19/10/2026)

**Một phần, còn mở.** `HomestayContentService.EditorAsync` trả các AmenityIds đã chọn và CategoryId của từng loại phòng, nhưng không có tên/tuỳ chọn của danh mục tiện nghi, danh mục loại phòng đầy đủ. `AvailabilityService.SearchAsync` chỉ trả tiện nghi/danh mục gắn cơ sở Active (có lọc), không phải danh mục quản lý. Không dùng danh mục bị lọc để ghi đè lựa chọn của chủ và không truy vấn DbContext trong controller mới.

05a: sửa thông tin loại phòng hiện có, mã phòng, thêm phòng, bật/tắt và giá vẫn làm được; giữ CategoryId và AmenityIds. Tab tiện nghi hiển thị trạng thái chưa hỗ trợ; chưa tạo loại phòng hoặc đổi danh mục. Khu vực lấy từ `AvailabilityService.SearchAsync(...).Areas` (danh sách này không lọc Active); địa chỉ/khu vực/toạ độ vẫn sửa qua service. Cần backend bổ sung DTO catalog được kiểm quyền cho editor, kèm nhãn tiện nghi đang chọn; sau đó nối các phần còn thiếu. Không sửa Services/Data/Entities trong bước UI.


**Trạng thái: Xong (B9)** — HomestayContentService.EditorAsync — Amenities/Categories đầy đủ, Selected theo cơ sở.

## G-17 — Số đơn chờ xác nhận của chủ (05a, 19/10/2026)

**Một phần, còn mở.** `OwnerScheduleService.ReadAsync` chỉ trả Confirmed/CheckedIn, không có Deposited. `BookingService` không có API danh sách/đếm đơn của chủ. `StaffBookingService.SearchAsync` chỉ cho nhân viên, không được gọi bằng quyền giả Employee. Danh sách Owner/Bookings là truy vấn controller cũ, chưa có service đọc tương ứng.

05a: dashboard có số khách đến hôm nay qua `OwnerScheduleService`, lối tắt tới `/Owner/Bookings?status=Deposited`, không dựng số đơn chờ hoặc thêm truy vấn. Cần service `OwnerBookingSummaryAsync(actor)` hoặc API đếm Deposited kiểm quyền theo chủ để hoàn thiện số đơn chờ trên dashboard.


**Trạng thái: Xong (B9)** — OwnerScheduleService.OwnerBookingSummaryAsync — Deposited, quá hạn và lượt nhận hôm nay đúng chủ.

## G-18 — Thông báo chủ cũ không có URL (05a, 19/10/2026)

**Nhỏ, còn mở cho dữ liệu cũ/nguồn tạo thông báo.** Khi rà tài khoản `0912000001`, bốn thông báo hiện có trả `Url = null`, nên trang/chuông không có liên kết để mở đơn. `BookingService.NotifyAsync` vẫn có nhánh gọi `NotificationService.Add` không truyền URL cho đơn đặt cọc/hủy. Không phân tích mã đơn từ nội dung tự do để đoán URL hoặc truy vấn lại trong UI. Các URL đã có của service khác đã được nối: `/Owner/Homestays/{id}`, `/Owner/Settlements/{id}`, nhắc nhận phòng qua `Bookings?arrivals=…`, đánh giá qua Booking Details có lối tắt tới Reviews. Bước backend sau cần bổ sung URL tại nguồn và quyết định backfill dữ liệu cũ nếu cần.


**Trạng thái: Xong (B9)** — BookingService.NotifyAsync/AlertOverdueConfirmationsAsync; ReceiptReviewService; BookingChangeRequestFinance; VoucherService — URL theo vai trò. Không backfill thông báo cũ.

## G-19 — Gửi lời mời kích hoạt đối tác (05b, 19/10/2026)

**Đã nối đủ UI qua service hiện có.** Ban đầu thiếu màn nhận token/kênh gửi. 05b thêm `Partner/Activate` GET/POST gọi `OwnerRegistrationService.ActivateAsync`; chủ tự nhập SĐT và đặt mật khẩu. `InviteAsync/ResendInviteAsync` nối ở Staff; liên kết riêng được chuyển qua TempData mã hóa, hiện đúng một lần trên trang NoStore/no-referrer để nhân viên sao chép và chuyển thủ công qua kênh riêng đã thống nhất. Không in token vào log/audit/ảnh/báo cáo, không đặt mật khẩu thay chủ, không tự gửi tin ngoài app. Resend vô hiệu token cũ theo service; test đi hết cả token cũ bị chặn và token mới kích hoạt được.

Kênh tự động Zalo/SMS thật vẫn là phần rút gọn đã chấp thuận; lựa chọn kênh gửi tương lai ghi trong báo cáo, không chặn UI hiện tại. Không sửa service.

## G-20 — Nhãn homestay/phòng trong màn Staff (05b, 19/10/2026)

**Nhỏ, còn mở.** `StaffBookingService.SearchAsync` không Include Homestay hay nhân viên đã ngừng hoạt động; `AssigneesAsync` chỉ trả người nhận hiện còn hợp lệ, không phải catalog lịch sử. `AvailabilityService.FreeRoomIdsAsync` chỉ trả ID phòng, không trả mã phòng. Không dùng catalog công khai (chỉ cơ sở đang bán) thay danh mục lịch sử và không truy vấn thêm DbContext.

Danh sách đơn lọc/hiện **Mã homestay** theo ID; chi tiết vẫn có tên homestay/mã phòng hiện tại từ DetailsAsync. Phòng trống chọn lại được ghi rõ **Phòng #{ID}**. Người phụ trách ngoài danh sách nhận đơn hiển thị **Nhân viên #{ID}**, không gán nhãn “Chưa giao”. Cần DTO đọc trả tên homestay/người phụ trách lịch sử và mã phòng còn trống để nâng trải nghiệm; không chặn thao tác.


**Trạng thái: Xong (B9)** — StaffBookingService.SearchAsync/DetailsAsync/HomestaysAsync; AvailabilityService.FreeRoomsAsync — nạp tên kể cả nhân viên/cơ sở lịch sử.

## G-21 — Dữ liệu phòng trống theo đêm theo lô (05b, 19/10/2026)

**Một phần về hiệu năng, còn mở.** `StaffAvailabilityAsync` trả số phòng trống cho cả khoảng (phòng trống xuyên suốt), không trả ma trận đêm. 05b gọi service một lần để kiểm khoảng tối đa 31 đêm, rồi từng đêm để hiện số đúng; không biến số của cả khoảng thành số từng đêm, không cache và không tự truy vấn tồn phòng. Bảng có thời điểm cập nhật và Làm mới. Cần API trả ma trận theo lô để giữ số truy vấn cố định trên dữ liệu lớn; chưa tuyên bố đạt NFR-02 ở tải 150 homestay/31 đêm với cách ghép này.


**Trạng thái: Xong (B9)** — AvailabilityService.StaffNightAvailabilityAsync — tối đa 31 đêm, bốn truy vấn dữ liệu + kiểm quyền, không lặp truy vấn theo đêm.

## G-22 — Link thông báo kết quả thẩm định gửi chủ (05b, 19/10/2026)

**Nhỏ, còn mở.** `OwnerRegistrationService.DecideAsync` gửi thông báo cho chủ khi NeedsRevision/Approved/Rejected nhưng URL là `RegistrationUrl(id)` = `/Staff/OwnerRegistrations/{id}`. Chủ không có policy Employee, nên không mở được URL này; màn đúng để chủ theo dõi đã có ở `/Partner/Status`. Giữ nguyên chặn quyền của Staff, không mở hồ sơ nhân viên cho chủ hoặc sửa service trong bước UI. Cần sửa URL tại nguồn theo người nhận và quyết định backfill thông báo cũ; thông báo nội bộ vẫn dùng URL Staff.


**Trạng thái: Xong (B9)** — OwnerRegistrationService.DecideAsync — thông báo chủ dùng /Partner/Status; không backfill.

## G-23 — Xác nhận chủ và ngày/số tiền chuyển đối soát (05c, 19/10/2026)

**Một phần, còn mở so với prompt 05c; giữ nghiệp vụ B5/D15.** `SettlementService.ConfirmAsync` chỉ nhận Actor Owner đúng cơ sở, không cho Accountant xác nhận thay chủ. `RecordPaidAsync(id, bankRef, actor)` ghi toàn bộ PayoutAmount và PaidAt = đồng hồ hệ thống, không nhận số tiền/ngày chuyển thực tế. UI kế toán không giả Actor Owner, không thêm form nhập số tiền/ngày rồi bỏ qua dữ liệu: hiển thị số phải trả từ service, giải thích ngày ghi nhận tự lưu; chủ xác nhận tại màn Owner hiện có. Sent hoặc Confirmed đều được chi theo D15; Paid bất biến, không hiện mở lại. Nếu cần chuyển từng phần/ngày thực tế hoặc xác nhận thay chủ, cần quyết định nghiệp vụ và backend riêng; không sửa service trong 05c.


**Trạng thái: Xong (B9)** — SettlementService.ConfirmOnBehalfAsync; RecordPaidAsync(id, bankRef, amount, paidAt, actor) — Accountant, lý do, toàn phần, ngày UTC, audit.

## G-24 — Danh sách khoản bù được phép từ chối (05c, 19/10/2026)

**Một phần, còn mở.** `CompensationService.PendingAsync` chỉ trả Compensation Submitted của đơn thay thế Completed, chưa thay tiếp. `RejectAsync` lại cấm đơn Completed. Không có API đọc danh sách khoản bù trước Completed cho kế toán, vì vậy không thể dựng form từ chối hợp lệ từ dữ liệu hiện có. 05c nối POST Reject qua service, antiforgery/PRG và kiểm quyền Accountant nhưng ẩn nút từ chối trên danh sách chờ chi; không mở UI nhập ID tùy ý hoặc thêm truy vấn nghiệp vụ. Muốn có form lý do + xác nhận danger cho khoản bù trước Completed cần backend trả danh sách được kiểm quyền, sau đó mới nối UI. Chi khoản bù có mã ngân hàng đã làm được.


**Trạng thái: Xong (B9)** — CompensationService.RejectableAsync — đúng Submitted, Compensation, đơn chưa Completed; Accountant.

## G-25 — Liên kết đơn/giao dịch sang đối soát và nhãn homestay (05c, 19/10/2026)

**Nhỏ, còn mở.** `ReceiptRow`, `RefundRow`, `BankTransactionRow`/`TransactionProcessViewModel` chưa có SettlementId hoặc HomestayId + CheckOut. AdditionalDeposits có Booking nhưng truy vấn hiện có không nạp SettlementItem. Không đoán kỳ từ ngày giao dịch, không coi kỳ theo CheckOut là bằng chứng đơn đã vào bảng, không thêm query vào controller/view. Chưa hiện lối tắt đối soát trên bốn màn bước 04. `SettlementService.ListAsync` không Include Homestay, nên danh sách/chi tiết kế toán dùng “Homestay #{ID}” và bộ lọc Mã homestay; không lấy catalog công khai đã lọc Active thay danh mục lịch sử. Cần DTO read trả liên kết đối soát thật và tên homestay lịch sử để hoàn thiện.


**Trạng thái: Xong (B9)** — SettlementService.ListAsync/DetailsAsync nạp Homestay/Owner/Person; AccountingService.ReceiptsAsync/RefundsAsync/TransactionsAsync/TransactionAsync/AdditionalDepositsAsync dùng SettlementItem thật.

## G-26 — Lý do cụ thể và nội dung trước thay đổi khi duyệt (05d, 19/10/2026)

**Một phần, còn mở.** `PendingAsync` có Homestay/SubmittedAt, `PendingDetailsAsync` có nội dung hiện tại, FirstReview và chủ; không có loại thay đổi địa chỉ/giấy tờ cụ thể, snapshot trước thay đổi hoặc diff. `EditorAsync.Documents` chỉ trả giấy tờ gắn HomestayId, không trả giấy tờ legacy gắn hồ sơ được phép dùng khi kích hoạt. UI không phân tích chuỗi thông báo hay tự truy vấn AuditLog/VerificationDocuments: hàng chờ phân biệt “Homestay mới” / “Duyệt lại địa chỉ / giấy tờ”, trang duyệt báo rõ chỉ có bản hiện tại, hiển thị ảnh/mô tả/phòng/giá/giấy tờ hiện có. Quyết định duyệt/mở lại vẫn để service kiểm đủ điều kiện, gồm legacy. Cần read DTO trả reason + snapshot/diff và giấy tờ legacy hợp lệ nếu muốn hoàn thiện so sánh. ApproveAsync không nhận lý do, nên UI không đưa ô lý do duyệt rồi bỏ qua; Reject/Resume nhận và lưu lý do. Nếu cần lý do khi duyệt cần bổ sung API ghi. Không sửa service trong 05d.

## G-27 — Bộ lọc chức vụ và đọc một tài khoản nhân viên (05d, 19/10/2026)

**Nhỏ về hiệu năng, còn mở.** `AccountAdminService.ListAsync` có Term/Role/Status/Page nhưng chưa có Position hoặc DetailsAsync. UI lấy mọi trang Role Employee qua service đã kiểm quyền rồi lọc chức vụ/phân trang ở bộ nhớ, chi tiết tìm trong cùng tập được phép đọc. Không lọc sau một trang (tránh bỏ sót), không dùng DbContext để đọc nghiệp vụ. Cần bổ sung Position vào AccountQuery và DetailsAsync được kiểm quyền để giữ số truy vấn ổn định khi số nhân viên lớn. Không thay đổi quyền ghi chỉ Administrator.


**Trạng thái: Xong (B9)** — AccountAdminService.ListAsync(AccountQuery.Position), DetailsAsync — lọc trước phân trang, kiểm Management.


### B9 — Phần đã xử lý của G-26 và giới hạn còn lại

`HomestayReviewService.PendingDetailsAsync` trả `ChangeReason`, `Changes` từ audit chuyển PendingReview có cấu trúc và `LegacyDocuments` còn hạn của hồ sơ Approved cùng chủ. Nguồn ghi vị trí/giấy pháp lý ghi ChangeReason cho thao tác mới. Không đoán lý do cũ nếu thiếu audit; không có snapshot toàn bộ ảnh/phòng/giá/giấy tờ trước thay đổi. Phần snapshot lịch sử đầy đủ cần quyết định lưu phiên bản/schema, chưa làm. ApproveAsync giữ nguyên không bắt lý do mới.

G-15: Xong (B9) — `ReviewService.ListAsync` nạp tên hiển thị người đánh giá qua Booking/Customer/Person; UI tiếp tục encode và fallback khi thiếu tên.
