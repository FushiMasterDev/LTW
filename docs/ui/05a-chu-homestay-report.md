# Báo cáo 05a — Màn mới cho chủ homestay

Ngày: 19/10/2026. Tiếp tục từ working tree bị tạm dừng ở HEAD `4580f42`; giữ nguyên năm commit trước và phần sửa chưa commit làm điểm xuất phát. Nhánh: `feature/foundation`. Không đổi nhánh, không push. Thực hiện theo `docs/prompts/ui/05a-chu-homestay.md`, nhận diện trong `DESIGN.md`; không tạo ảnh mới và không thêm thư viện.

## Màn đã làm và service đã nối

| Màn | Kết quả | Service |
|---|---|---|
| Homestay của tôi | Danh sách có tìm tên, phân trang, trạng thái duyệt; chỉ homestay của actor | `HomestayContentService.OwnerHomestaysAsync` |
| Tab Thông tin | Đổ dữ liệu sửa, mô tả/hướng dẫn, liên hệ, giờ nhận/trả; giữ tiện nghi hiện tại. Bản nhập lỗi thông tin/địa chỉ được giữ trong session theo tài khoản + homestay, sau redirect chỉ hiện lại cho đúng chủ | `EditorAsync`, `UpdateAsync` |
| Tab Địa chỉ | Địa chỉ, khu vực, tọa độ; báo trước việc đưa cơ sở đang hoạt động về chờ duyệt, sau lưu có “Đang chờ Quản lý duyệt” khi trạng thái thực sự PendingReview | `EditorAsync`, `UpdateLocationAsync`; danh sách khu vực đầy đủ từ `AvailabilityService.SearchAsync(...).Areas` |
| Tab Phòng và giá | Sửa loại phòng hiện có, mã phòng, thêm phòng, bật/tắt phòng/loại phòng, giá thường/cuối tuần/lễ theo ngày hiệu lực; hiện cả giá hiện hành và tương lai | `RoomService.UpdateTypeAsync`, `AddRoomAsync`, `RenameRoomAsync`, `SetRoomActiveAsync`, `SetTypeActiveAsync`, `SetPriceAsync` |
| Tab Ảnh | `_UploadZone`, gắn loại phòng, thứ tự, chọn bìa, xóa có modal xác nhận | `HomestayImageService.UploadAsync`, `UpdateAsync`, `SetCoverAsync`, `DeleteAsync` |
| Tab Tiện nghi | Hiện thông báo chưa có catalog quản lý; giữ nguyên AmenityIds, không dựng danh mục giả | G-16, xem bên dưới |
| Tab Chính sách hủy | Chính sách chung/lễ, thêm/bỏ mốc hoàn, thay phiên bản hoặc gỡ chính sách riêng có xác nhận; đơn cũ giữ snapshot | `HomestayPolicyService.ReplaceAsync`, `RemoveAsync` |
| Tab Giấy tờ | Tải, liệt kê, mở qua endpoint kiểm quyền, xóa chỉ ở trạng thái service cho phép; báo trước việc duyệt lại giấy pháp lý | `HomestayDocumentService.UploadAsync`, `DeleteAsync`; `OwnerRegistrationQueryService.OpenDocumentAsync` |
| Lịch phòng | Một phòng mỗi lượt, tháng có trạng thái thật, kéo chọn nhiều đêm hoặc dùng bộ chọn ngày; khóa và mở khóa qua PRG | `RoomBlockService.CalendarAsync`, `CreateAsync`, `UnlockAsync` |
| Lịch nhận–trả | Hôm nay / Ngày mai / 7 ngày tới; ngày, tên, SĐT, homestay/phòng, số đêm và link đơn | `OwnerScheduleService.ReadAsync(actor, from, through, homestayId)` |
| Đối soát | Danh sách kỳ, lọc trạng thái, phân trang; chi tiết đơn, tỷ lệ/tiền hoa hồng, số được nhận, khoản bù chi riêng; xác nhận và phản hồi sai lệch | `SettlementService.ListAsync`, `DetailsAsync`, `ConfirmAsync`, `ReportDiscrepancyAsync` |
| Đánh giá | Chọn homestay, phân trang, điểm, bình luận, trả lời/sửa trả lời công khai; dùng `_ReviewCard`, encode toàn bộ nội dung | `ReviewService.ListAsync`, `ReplyAsync` |
| Dashboard và điều hướng | Bổ sung SidebarLink/lối tắt tới các màn, link đơn Deposited; bốn ô số liệu thật, trong đó “Lượt nhận phòng hôm nay” đếm số đơn/lượt đến, không phải số người | `OwnerScheduleService.ReadAsync` cho số lượt; giữ nguyên truy vấn dashboard cũ, không thêm truy vấn DbContext |
| Thông báo | Rà chuông và `/Notifications` trên điện thoại; alias `/Owner/Homestays/{id}`, `/Owner/Settlements/{id}`; link nhắc cũ `Bookings?arrivals=…` chuyển Schedule đúng ngày; Booking Details có link tới đánh giá của homestay | Giữ component/service bước 02; G-18 cho thông báo cũ thiếu URL |

Tất cả controller mới dùng area Owner, policy `ApprovedOwner`, actor từ tài khoản đăng nhập. POST có antiforgery, PRG và TempData cho `_Alerts`. Không sửa `Services/`, `Data/`, `Models/Entities/`, migration hoặc database script. Không giả quyền Employee để đọc dữ liệu của chủ.

`OwnerScheduleService` chỉ trả Confirmed/CheckedIn; đây là hai trạng thái cho phép liên hệ theo `BookingContactVisibility`. Không mở thêm liên hệ ở trạng thái khác. Tên khách đánh giá vẫn dùng “Khách đã lưu trú” theo G-15, không dựng tên.

## Component và UX

- Dùng `_PageHead`, `_Breadcrumb`, `_Tabs`, `_FilterBar`, `_DataTable`, `_Pager`, `_StatCard`, `_TaskList`, `_PriceBreakdown`, `_ReviewCard`, `_UploadZone`, `_RoomCalendar`, `_DateRangePicker`, `form-field` và các Tag Helper tiền/trạng thái/ngày/điểm.
- `_RoomCalendar` thêm tùy chọn `RangeSelection` và `MinSelectableDate`; chỉ phát sự kiện chọn khoảng, không ghi nghiệp vụ. Có ví dụ trong UI Kit, chọn bằng bàn phím/bộ chọn ngày thay thế, và form native khi tắt JS.
- `_ReviewCard` thêm vùng Actions kiểu `IHtmlContent` để dùng form trả lời chung trong thẻ, có mẫu UI Kit. Badge thêm nhãn/tone cho SettlementStatus và PaymentStatus, có bảy test.
- Dùng token hiện có, Manrope/Fraunces, nút cam và các màu sáng/tối hiện hành. Form tối đa 40rem; thao tác chính dùng mẫu nút dính dưới trên điện thoại. Không thêm chuyển động dashboard hoặc thư viện; giữ motion/reduced-motion hiện có.
- Giá đã có hiệu lực không bị ghi đè: UI hiển thị quy tắc của `SetPriceAsync`. Có thể thêm giá hiệu lực hôm nay nếu chưa có dòng cùng ngày, hoặc nhập ngày mới. Không thay nghiệp vụ giá để ép sửa cùng ngày.

## Đo thao tác khóa/mở

Trên viewport 375, homestay 6, phòng 102 (`RoomId=20`):

1. Chọn phòng 102: lịch tự tải, không có nút Áp dụng trung gian.
2. Kéo một lần qua hai đêm 27 và 28/10/2026: form nhận `start=2026-10-27`, `end=2026-10-29`.
3. Bấm “Xác nhận khóa ngày”: thành công.

**Khóa: 3 thao tác nghiệp vụ.** Không tính cuộn trang; chọn option trong select tính là một thao tác chọn phòng, kéo là một cử chỉ liên tục. Đã đo thêm bằng sự kiện touch của Chromium (`pointerType=touch`), chọn đúng hai đêm. Bộ chọn ngày vẫn là phương án thay thế cho người không kéo.

**Mở: 3 thao tác:** chọn phòng → chọn khoảng khóa → “Xác nhận mở khóa”. Không thêm modal thứ hai. UI nói rõ mở toàn bộ phần tương lai của block, kể cả ngày ngoài tháng đang xem, đúng hợp đồng `UnlockAsync`. Sau mở, cả 27 và 28/10 đều trở về `Free` trên app dùng SQL Server.

## Kiểm tra và bằng chứng

- Build Release với `dotnet build -c Release -p:UseSharedCompilation=false`: chạy lại thành công, **0 warning, 0 error**; log `/private/tmp/fuji-05a/resume-build.log`. Full suite dùng đúng lệnh `dotnet test HomestayBooking.sln -c Release --no-build`.
- Full suite của phiên trước: **1.634 pass + 1 skip, 0 fail**, log `/private/tmp/fuji-05a/tests-final-release.log`. Phiên tiếp tục bổ sung ba test dashboard (lọc đúng chủ/ngày/trạng thái, chủ chưa duyệt, anonymous/khách); bộ riêng **3 pass**, log `/private/tmp/fuji-05a/resume-dashboard-tests.log`. Full suite chạy lại: **1.638 pass + 1 skip, 0 fail**, tổng 1.639 test, thời gian **4 phút 21 giây**; log `/private/tmp/fuji-05a/resume-tests-full.log`. So với nền 1.597 pass, bước 05a có thêm 41 test gồm test ngày thiếu đang dở được giữ lại và ba test dashboard mới.
- Test theo màn trước commit: Homestays + badge 19 pass; Calendar 4 pass; Schedule 6 pass; Settlements 4 pass; Reviews 4 pass.
- `python3 -I tools/ui-crawl.py http://localhost:5012` chạy lại: **840 lượt trang, không có 500**, chỉ bốn 404 đã biết: `/Bookings/Create` thiếu roomTypeId và ba mã đơn mẫu ở UI Kit. Log mới: `/private/tmp/fuji-05a/resume-crawl.log`. Lần cũ từng gặp thêm 403 ở link công cụ Development của khách; lần chạy lại không có 403. Không sửa endpoint hoặc mở rộng quyền.
- Chụp lại 17 màn/biến thể × 375/768/1280 × sáng/tối = **102 ảnh chính**. Tất cả trả 200, document width bằng viewport, không có input/select/textarea thiếu label. Bốn log mới 375/1280 là `resume-capture-<width>-<theme>.log`; tablet có hai log 768 cùng mẫu tên. Có thêm bốn ảnh chuông và hai ảnh sidebar điện thoại; chờ chuyển động kết thúc trước chụp và thử reduced-motion.
- Rà thêm tiếng Anh của Homestays, địa chỉ, Calendar, Schedule, Settlement Details và Reviews: 200, heading/nhãn form được dịch. Chuỗi thông báo/dữ liệu người dùng giữ nguyên.
- Quyền chéo chủ: `/Owner/Homestays/Edit/2` trả 404 cho chủ `0912000001`; test còn kiểm tra filter homestay khác, phòng/loại phòng khác, đối soát khác, đánh giá khác, chủ chưa duyệt, khách và anonymous. Có test POST thiếu antiforgery.
- Có test giữ nguyên tiện nghi khi sửa nội dung; thay địa chỉ về PendingReview; giá theo ngày hiệu lực; reply encode chống HTML/script; xác nhận đối soát; khóa/mở. Fixture test riêng, không thay dữ liệu SQL Server của app.
- Đã kiểm tra fallback khi tắt JavaScript: hai input date native và nút Xem lịch đều hiện. Có ảnh chọn khoảng bằng touch và kiểm tra ô phản hồi đang focus không bị nút dính che. Đã sửa PRG khi form no-JS gửi thiếu ngày: quay lại tháng hiện tại với lỗi, không tạo block; bỏ giới hạn năm tùy ý của UI, chỉ giữ chặn tràn DateOnly.
- `rg` không có inline `style="` trong view/JS, không có mã màu ngoài `tokens.css` trong CSS ứng dụng; `git diff --check` sạch. Không thêm `transition: all` hay `will-change` thường trực.

Log thao tác ghi của phiên tiếp tục: `/private/tmp/fuji-05a/resume-smoke.log`; chọn touch/no-JS: `resume-touch.log`; điều hướng/chặn quyền/đơn demo: `resume-navigation.log`; bản dịch: `resume-english.log`. Đã mở lại `/Dev/UiKit` bằng QL001 và bố cục tham khảo `/owner` (log `resume-uikit.log`, ảnh `reference-owner.png`); không sao chép số liệu hoặc biểu đồ mẫu. Ba bố cục tham khảo còn lại đã có bằng chứng từ phiên trước.

Ảnh, log, script đo nằm ở `/private/tmp/fuji-05a/` và snapshot CLI ở `.playwright-cli/`; không commit các artifact này. Mẫu tên ảnh: `edit-rooms-375-light.png`, `calendar-375-dark.png`, `settlement-details-1280-light.png`, `reviews-1280-dark.png`; sáu log `resume-capture-<width>-<theme>.log` lưu kết quả status/overflow/label của phiên tiếp tục.

### web-design-guidelines

Đã tải lại [quy tắc từ nguồn của skill](https://raw.githubusercontent.com/vercel-labs/web-interface-guidelines/main/command.md) và rà toàn bộ view mới, hai component được mở rộng, script và CSS liên quan. Kết quả theo file nằm ở `/private/tmp/fuji-05a/resume-web-guidelines-review.md`; kiểm tra tĩnh ở `resume-static-checks.log`.

| Phạm vi | Kết quả |
|---|---|
| `Areas/Owner/Views/Homestays/*` | Label, validation, upload có nhãn, ảnh có kích thước/alt; xóa có xác nhận; cảnh báo duyệt lại ở đúng tab |
| `Areas/Owner/Views/Calendar/Index.cshtml` | Select/date có label, hỗ trợ touch và bàn phím/no-JS; thông báo chọn khoảng qua live region, không phụ thuộc màu |
| `Areas/Owner/Views/Schedule/Index.cshtml` | Bảng thành thẻ ở điện thoại, SĐT bấm gọi, link đơn và số đêm đọc được |
| `Areas/Owner/Views/Settlements/*` | Tiền qua `<money>`, tỷ lệ 0,1 snapshot hiển thị 10%; khoản bù tách rõ; form có label/maxlength |
| `Areas/Owner/Views/Reviews/Index.cshtml` + `_ReviewCard` | Nội dung encode, fallback tên đúng G-15, textarea có label/hint và giới hạn 1000 ký tự |
| Dashboard / sidebar / chuông / Notifications | Lối tắt đúng đích, nhãn qua component, số liệu thật theo quyền; chuông nằm trong viewport; menu điện thoại tới Calendar; G-18 giữ nguyên dữ liệu thiếu URL |
| Shared calendar / form script / CSS | Giữ focus, thao tác thay thế cho kéo, cảnh báo bản nhập chưa lưu, không thêm animation phá reduced-motion |

Không còn phát hiện mức cao. Mẫu đo chữ/nền thực tế (form, nút, badge, ô lịch tương tác, nhãn, chữ phụ) thấp nhất **5,55:1 sáng; 8,51:1 tối**. Đây là đo các phần tử đại diện trong bốn trang, không khẳng định đo từng pixel/ảnh. Chưa thử trên thiết bị điện thoại vật lý; đã thử viewport 375/768/1280 và touch emulation. Đo lại 12 lượt trang/chế độ, gồm Dashboard và Notifications, kết quả ở `/private/tmp/fuji-05a/resume-contrast.log`; không có mẫu đo dưới 4,5:1.

## Dữ liệu thử

| Dữ liệu | Thao tác và trạng thái cuối |
|---|---|
| Đơn `FJ261019-001` | Giữ lại để demo; kiểm tra cuối vẫn **Đã xác nhận**, tổng tiền 1.800.000đ, 15–17/12/2026. Không gửi thao tác hủy hay thay đổi trạng thái đơn này |
| Homestay 6 — Ba Đình Green Villa, loại Phòng đôi | Giá ngày thường mới **905.000đ**, hiệu lực **19/10/2026**, thay giá áp dụng trước đó 900.000đ; giữ phiên bản mới để demo, không sửa giá các đơn cũ |
| Ảnh thử Id 348 | Tải lại file đã có `images/demo/sofa-corner-sm.webp`, không tạo ảnh. Upload thành công vào homestay 6, sau đó xóa đúng ảnh qua modal; file upload cũng đã được service xóa khỏi đĩa. Không thay ảnh bìa |
| Block BL7, BL8 | Khóa phòng 102, homestay 6, hai đêm 27–28/10; cả hai block thử đã được mở/xóa, lịch trở lại Free. BL8 dùng để đo luồng 3 thao tác |
| Review Id 7, homestay 16 — Văn Miếu Scholar Home | Trước đó chưa có trả lời. Đã lưu: “Cảm ơn bạn đã chia sẻ trải nghiệm. Fuji đã ghi nhận góp ý để chuẩn bị phòng chu đáo hơn cho lần lưu trú tới.” Giữ để demo; service tự ghi audit và thông báo khách theo luồng hiện có |
| Phiên tiếp tục — giá loại phòng 14, homestay 6 | Lưu mức ngày thường **905.000đ**, hiệu lực **20/10/2026**, cùng mức hiện hành 19/10; giữ dòng giá mới, không sửa snapshot đơn cũ |
| Phiên tiếp tục — ảnh Id 349 | Tải lại cùng file `sofa-corner-sm.webp` có sẵn; xóa đúng ảnh thử qua modal sau khi kiểm tra upload, không đổi ảnh bìa |
| Phiên tiếp tục — block BL9 | Phòng 102/RoomId 20, 27–28/10; khóa rồi mở, kiểm tra lại hai đêm Free |
| Phiên tiếp tục — đánh giá 7 | Lưu lại nguyên văn câu trả lời demo đang có; không đổi nội dung. Service ghi audit/thông báo theo luồng sẵn có |
| Đối soát | Đã xem kỳ/chi tiết số 52 trên app thật; không xác nhận hoặc phản hồi sai lệch lên SQL Server. Thao tác ghi được thử trong fixture riêng |
| Địa chỉ / giấy tờ pháp lý | Không đổi trên SQL Server để tránh đưa cơ sở demo về chờ duyệt. Luồng đổi địa chỉ và thông báo chờ duyệt đã được test tích hợp bằng fixture riêng |

## Gap và phần chưa làm

- **G-16:** chưa sửa tiện nghi, tạo loại phòng mới hoặc đổi danh mục loại phòng vì thiếu catalog quản lý đầy đủ. Catalog công khai loại phòng/tiện nghi bị lọc Active nên không dùng để ghi đè. Khu vực có danh sách đầy đủ nên đã nối bình thường.
- **G-17:** chưa có con số đơn chờ xác nhận trên dashboard. OwnerSchedule không trả Deposited; BookingService không có API đếm/danh sách của chủ, StaffBookingService chặn Owner. Có link lọc Deposited; không thêm truy vấn DbContext hoặc bịa số.
- **G-18:** bốn thông báo cũ của tài khoản thử không có URL; giữ nguyên, không đoán đường từ câu chữ. Các mẫu URL có sẵn đã được nối.
- **G-15 có trước:** tên người viết đánh giá chưa có trong DTO service; tiếp tục fallback.

## Cần người dùng quyết định

Không tự đặt quyết định nghiệp vụ mới. Cần chốt một bước backend tiếp theo để bổ sung catalog editor đầy đủ (G-16), API đếm đơn Deposited có kiểm quyền chủ (G-17), và URL tại nguồn/backfill thông báo cũ nếu cần (G-18). Sau đó UI có thể nối các phần còn treo mà không đổi quyền hoặc quy tắc giá hiện tại.

Giá đã có hiệu lực không được ghi đè và mở khóa toàn bộ phần tương lai của block là hành vi service hiện hành; bước này giữ nguyên. Nếu muốn đổi hai quy tắc đó, cần một yêu cầu backend riêng. Service chỉ chuyển cơ sở Active về PendingReview khi đổi địa chỉ/giấy pháp lý; cơ sở Suspended giữ trạng thái và gửi yêu cầu kiểm tra. Nếu muốn áp dụng PendingReview cho cả Suspended như cách hiểu rộng của prompt, cần chốt riêng ở backend; UI hiện cảnh báo đúng phạm vi Active và giữ hành vi an toàn của service.

## Commit và kết thúc phiên

- `f08d58b` — Homestays, editor, component, bản dịch và test.
- `60b0b8a` — Lịch phòng, khóa/mở.
- `fadd6a3` — Lịch nhận–trả và link nhắc.
- `debdb34` — Đối soát của chủ.
- `4580f42` — Đánh giá và trả lời khách.
- Commit cuối — Dashboard, sidebar, link đánh giá, cập nhật inventory và báo cáo này (xem HEAD).

Mọi commit có dòng kết thúc `Co-Authored-By: Codex <noreply@openai.com>`. App kiểm tra chỉ chạy cổng 5012. Lần đầu trong sandbox bị chậm khi kết nối localhost, đã dừng đúng PID 10961/10930 rồi chạy lại với môi trường đầy đủ. Lần kiểm tra chính dùng app PID **10992**, tiến trình `dotnet run` PID **10990**; đã gửi `TERM` đúng PID 10992 sau kiểm tra, xác nhận app và tiến trình cha kết thúc, cổng 5012 không còn listener. Đã đóng riêng browser `fuji-resume`; container `sql-homestay` vẫn Up. Không có lệnh dừng app 5011 hoặc dừng/xóa container SQL Server.

Một lần chạy test riêng trong sandbox bị dừng để chạy lại ngoài sandbox; kết quả nghiệm thu là lần chạy đầy đủ kết thúc exit 0, không dùng log của lần bị dừng để khẳng định pass. Hai lỗi định vị/timing của script chụp (trùng nút mở/đóng menu, nút EN ẩn trên điện thoại) đã sửa trong script tạm và chạy lại thành công; không phải thay đổi nghiệp vụ hay mã sản phẩm.

Kiểm tra trạng thái cuối: `/private/tmp/fuji-05a/resume-final-ui.log` xác nhận hai đêm Free, ảnh 349 đã bỏ, dòng giá 20/10 hiện đúng, demo giữ nguyên, reduced-motion giữ số cuối `7/7/26/0`, và nhãn mới tiếng Anh “Check-ins today”. Artifact không nằm trong commit. Không push, không đổi nhánh.
