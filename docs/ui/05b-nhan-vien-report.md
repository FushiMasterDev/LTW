# Báo cáo 05b — Màn mới cho nhân viên

Ngày: 19/10/2026. Tiếp tục từ working tree 05b chưa commit trên `feature/foundation`, sau 05a `0540729`. Giữ phần dở làm điểm xuất phát, không revert. Trong lúc kiểm tra có commit tài liệu B9 `27f7353` được thêm trên cùng nhánh; giữ nguyên commit này, không triển khai B9. Không đổi nhánh, không push, không bắt đầu 05c, không tạo tài nguyên ảnh mới hoặc thêm thư viện.

## Màn và service đã nối

| Màn | Kết quả | Service |
|---|---|---|
| Quản lý đơn FR-09 | Danh sách có tìm mã/tên/SĐT, trạng thái, mã homestay, ngày nhận, người phụ trách/chưa giao; bảng thành thẻ, giữ bộ lọc khi phân trang | `StaffBookingService.SearchAsync`, `AssigneesAsync` |
| Chi tiết đơn | Stepper, mô tả khách/phòng/ngày, bảng giá từng đêm, timeline; Tư vấn nhận đơn chưa giao hoặc giao lại đơn mình, Quản lý giao đơn; đổi trạng thái có ghi chú và xác nhận; lối tắt đổi/hủy và báo trùng | `DetailsAsync`, `AssignAsync`, `ChangeStatusAsync`; `AvailabilityService.FreeRoomIdsAsync` |
| Đặt hộ FR-07 | Nhập SĐT tìm khách; khách mới do service tạo, chọn Phone/Zalo/Messenger/Other, khu vực/phòng/ngày/số khách, xem giá rồi xác nhận; chỉnh thông tin sẽ ẩn xác nhận cũ; có fallback không JS | `CustomerService.SearchAsync`, `AvailabilityService.SearchAsync`/`StaffAvailabilityAsync`, `PricingService.QuoteAsync`, `StaffBookingService.CreateForCustomerAsync` |
| Phòng trống FR-08 | Bảng homestay × loại phòng × đêm, trống/tổng và giá đêm; lọc khu vực/ngày/số khách, phân trang 30 dòng, nút Làm mới, thời điểm cập nhật, NoStore; không lịch tháng hoặc cache client | `AvailabilityService.StaffAvailabilityAsync`; catalog khu vực từ `SearchAsync` |
| Khách hàng FR-12 | Danh sách, chi tiết, lịch sử đơn và link chi tiết; sửa ghi chú, tên/email với khách chưa có tài khoản; giữ định danh khách có tài khoản | `CustomerService.SearchAsync`, `DetailsAsync`, `UpdateAsync` |
| Trùng phòng FR-10 | Báo từ đơn, thẻ phương án giá/khu vực/chênh lệch theo thứ tự service, phân trang sáu thẻ; chuyển phòng cùng loại, thay đơn/homestay, hoặc tạo hoàn tiền; ghi chú và xác nhận từng thao tác; hiển thị chênh lệch/tiền hoàn | `OverbookingService.ReportAsync`, `AlternativesAsync`, `MoveRoomAsync`, `ReplaceAsync`, `RefundAsync`; `FreeRoomIdsAsync` |
| Thẩm định FR-26 | Danh sách lọc trạng thái/tên/SĐT/quá SLA; chi tiết đối tác, cơ sở, giấy tờ riêng, ghi chú và timeline; nhận, yêu cầu bổ sung, duyệt/từ chối theo quyền | `OwnerRegistrationQueryService.ListAsync`, `DetailsAsync`, `OpenDocumentAsync`; `OwnerRegistrationService.TakeAsync`, `RequestRevisionAsync`, `ApproveAsync`, `RejectAsync` |
| Lời mời đối tác | Tạo/tạo lại lời mời, liên kết riêng hiện một lần; chủ mở `Partner/Activate`, tự nhập SĐT và đặt mật khẩu. NoStore/no-referrer, không đưa token vào log/báo cáo/ảnh; không gửi tin ngoài app | `InviteAsync`, `ResendInviteAsync`, `ActivateAsync` |
| Dashboard và FR-11 | Đơn Deposited chờ xác nhận, số yêu cầu đổi/hủy, đơn quá hạn theo `ConfirmDeadlineAt`, hồ sơ PendingReview/UnderReview và tỷ lệ chốt của chính actor; đọc hết trang để không bỏ sót quá hạn | `StaffBookingService.SearchAsync`, `BookingChangeRequestService.PendingCountAsync`, `OwnerRegistrationQueryService.ListAsync`, `ReportService.EmployeesAsync` |
| Chuông và thông báo | Dùng lại chuông/trang `/Notifications` của bước 02, gồm thông báo quá hạn do job hiện có; không tạo cơ chế thông báo riêng | `NotificationService` và `BookingTimeoutJob` hiện có |
| FR-27 | Không dựng màn duyệt nội dung; NV001 bị chặn tại `/Staff/Homestays/6`. QL/AD nhận 404 ở URL Staff cũ cho tới 05d. Dashboard không có hàng chờ duyệt nội dung | Không gọi service duyệt nội dung |

Controller mới dùng area Staff, policy `Employee`; lớp cơ sở chặn chức vụ mà service không hỗ trợ. Hoàn tiền trùng phòng và duyệt/từ chối hồ sơ có thêm `Management`. Kế toán tới Staff Dashboard từ luồng đăng nhập cũ được chuyển về Accounting; các màn nghiệp vụ Staff mới vẫn bị chặn. Chỉ tra Employee.Id từ PersonId để tạo Actor; không thêm truy vấn nghiệp vụ. Mọi POST mới có antiforgery, PRG và TempData. Không sửa `Services/`, `Data/`, `Models/Entities/`, migration, database script hoặc policy.

## Component và rà giao diện

Dùng lại PageHead, Breadcrumb, FilterBar, DataTable/Pager, Steps/BookingStepper, DescriptionList, PriceBreakdown, Timeline, StatCard/TaskList, DateRangePicker, form-field và Tag Helper tiền/trạng thái/ngày. Hai mẫu mới `_OverbookingAlternative`, `_RegistrationDocuments` được đăng ký trong UI Kit. Thêm SidebarLink cho màn mới, resource EN và module nhỏ `staff-booking.js` để loại bỏ xác nhận báo giá đã cũ.

Giữ nhận diện/token Manrope/Fraunces, xanh rừng/cam đất, sáng/tối và motion dashboard hiện có. Không thêm chuyển động hoặc biểu đồ thiếu dữ liệu. Form đặt hộ/khách hàng dùng mẫu tối đa 40rem và nút cuối form trên điện thoại.

Đã đọc lại [web-design-guidelines](https://raw.githubusercontent.com/vercel-labs/web-interface-guidelines/main/command.md), rà tất cả view mới, component và script. Phát hiện rồi sửa tràn tên file giấy tờ dài, tên cơ sở/loại phòng không có khoảng trắng và phụ đề tên khách trong PageHead bằng utility Bootstrap có sẵn. Kiểm tra DOM với tên file 240 ký tự, nội dung 200 ký tự trên 375px trở lại overflow 0. Mở rộng vùng bấm liên kết bảng, breadcrumb khi hiện và TaskList lên 44px ở dưới 992px; lối tắt hồ sơ khách dùng nút link 44px. Label, focus theo component, xác nhận thao tác, giữ trạng thái URL, mã hóa nội dung người dùng và reduced-motion được rà trên app. Chi tiết theo file: `/private/tmp/fuji-05b/resume-web-guidelines-review.md`.

Mẫu đo tương phản tại chín màn × hai chế độ có mức thấp nhất **5,55:1 sáng, 6,62:1 tối**; không có mẫu dưới 4,5:1 (`resume-contrast.log`). Đây là mẫu chữ/nền thực tế, không khẳng định đo mọi pixel hoặc đã thử điện thoại vật lý. Đã mở lại bố cục tham khảo `/staff` bằng playwright-cli; không lấy số liệu/biểu đồ mẫu.

## Kiểm tra và bằng chứng

- Build Release dùng `dotnet build -c Release -p:UseSharedCompilation=false`: **0 warning, 0 error**, log cuối `final-build.log`. Toàn bộ test dùng `dotnet test HomestayBooking.sln -c Release --no-build`: bản cuối sau sửa vùng bấm đạt **1.655 pass + 1 skip, 0 fail**, thời gian **3 phút 19 giây**, log `resume-tests-final-44px.log`.
- Lần xác minh sau khi thêm test dashboard: **1.655 pass + 1 skip, 0 fail**, tổng 1.656 (`resume-tests-final.log`); sau sửa PageHead, chạy lại cùng kết quả (`resume-tests-complete.log`). So với nền 1.638 pass của 05a, có thêm 17 ca: sáu route thành công/chặn quyền và các luồng đơn, khách, phòng trống, hồ sơ, trùng phòng, kích hoạt, cảnh báo qua nhiều trang; có test tạo đơn thật bằng SQLite. Một skip là ca hiệu năng opt-in yêu cầu `RUN_PERF_TESTS=1`, giữ như nền; không tuyên bố đạt NFR từ bộ test thường.
- Integration test kiểm tra tìm SĐT/nhận/giao đơn, chặn đổi trạng thái tiền, thiếu antiforgery, PRG khi dữ liệu sai, ngày biên, không dùng RoomCalendar, NoStore, giữ định danh khách có tài khoản, encode ghi chú, duyệt hồ sơ bằng Quản lý và chặn Tư vấn, báo trùng/hoàn tiền và quyền Management. Token mời cũ bị chặn sau resend, link chỉ hiện một lần, token mới cho chủ tự kích hoạt; không tạo lời mời thật trên SQL Server của app trong phiên tiếp tục.
- `python3 -I tools/ui-crawl.py http://localhost:5012`: **840 lượt trang, không có 500**, chỉ bốn 404 đã biết: `/Bookings/Create` thiếu roomTypeId và ba mã đơn mẫu `FJ2611-0012/0013/0014` trong UI Kit; đã chạy lại sau sửa PageHead (`resume-crawl.log`, `resume-crawl-complete.log`).
- **144 ảnh chính mới của phiên tiếp tục:** 12 màn/biến thể × 375/768/1280 × sáng/tối × Việt/Anh. Sáu log `resume-capture-<width>-<vi|en>.log` xác nhận tất cả 200, overflow 0, không thiếu label ở input/select/textarea đang hiện, không có ngày 0001. Có thêm UI Kit, no-JS và ảnh thao tác; ảnh từ phiên trước vẫn giữ nguyên.
- NV001 trên app SQL Server: tìm `0977050505`, nhận đơn thử chưa giao, đặt hộ SĐT mới `0977050519` qua Zalo, xem/làm mới phòng trống; URL duyệt nội dung bị chuyển AccessDenied và trả **403**. Log: `resume-state.log`, `resume-claim.log`, `resume-create.log`, `resume-availability.log`.
- QL001: giao đơn thử cho NV002; nhận thẩm định hồ sơ seed rồi yêu cầu bổ sung; mở giấy tờ riêng `/Staff/OwnerRegistrations/Document/76` trả 200/image/png; báo và chuyển phòng cho một ca trùng. Log: `resume-assign.log`, `resume-registration.log`, `resume-document.log`, `resume-overbooking-verified.log`.
- Tắt JavaScript: hai input ngày native và phần xác nhận giá đều hiện; sửa form có JS thì xác nhận cũ ẩn. Đọc lại đơn demo `FJ261019-001` vẫn Confirmed, 15–17/12/2026, tổng 1.800.000đ (`resume-fallback.log`). Rà thêm activation ở 12 tổ hợp viewport/theme/ngôn ngữ với token preview vô hiệu (`resume-extra-qa.log`); overflow 0. Số dashboard normal/reduced-motion đều giữ đúng `4/4/4/0` (`resume-final-state.log`).
- Đo vùng bấm sau sửa: liên kết bảng ở ba danh sách, lối tắt hồ sơ khách, bốn link TaskList và breadcrumb khi hiện đều cao **44px**, overflow 0 (`resume-touch-targets-final.log`).
- `rg` trên Views/Areas không có `style="`; CSS ứng dụng ngoài `tokens.css` không có hex/rgb/hsl; khóa L literal mới đều có EN, resource không có khóa trùng; `git diff --check` sạch. Không thay file backend bị cấm.

Ảnh/log/script nằm ở `/private/tmp/fuji-05b/` hoặc `.playwright-cli/`, không commit. Không chụp màn lời mời đang chứa token; chỉ chụp trạng thái đã xem và activation với token preview không hợp lệ.

## Dữ liệu thử trên SQL Server

| Dữ liệu | Thao tác và trạng thái |
|---|---|
| `FJ261019-001`, Id 3013 | Giữ nguyên đơn demo của bước 04; chỉ đọc, không hủy, không đổi phòng/trạng thái/người phụ trách |
| `FJ261019-003`, Id 3015; khách Id 2007, SĐT `0977050505` | Đã có từ phiên 05b trước: đơn đặt hộ và xác nhận bằng giao dịch mô phỏng, sau đó chuyển 101 → 102. Phiên tiếp tục giữ người phụ trách NV002, báo sự cố và chuyển **102 → 101** cùng loại. Cuối luồng Confirmed, 05–07/12/2026, tổng **1.985.000đ**, cọc **596.000đ**, không thay giá/cọc |
| `FJ261019-002`, Id 3014 | Đơn thử đã Expired trước phiên tiếp tục; NV001 bấm Nhận đơn qua modal. Chỉ đổi người phụ trách sang NV001, giữ nguyên Expired/tiền/phòng |
| `FJ261019-004`, Id 3016; khách mới Id 2008, SĐT `0977050519` | NV001 tạo đặt hộ qua Zalo, Ba Đình Green Villa/loại 14/phòng 101, 10–12/12/2026, hai khách; tổng **1.985.000đ**, cọc **596.000đ**, ban đầu AwaitingPayment. Không gửi tiền hoặc xác nhận; job chuyển sang **Expired lúc 15:49** theo luật hiện có, đã đọc lại ở `resume-final-state.log` |
| Khách Id 2008 | Lưu ghi chú “Kiểm thử 05b tiếp tục: khách liên hệ qua Zalo, kiểm tra ghi chú hồ sơ.”; không tạo tài khoản đăng nhập cho khách |
| Hồ sơ #1002, SĐT `0977123456` | Phiên trước đã chuyển sang NeedsRevision; phiên tiếp tục chỉ đọc, không thay quyết định |
| Hồ sơ seed #6, SĐT `0912000006` | PendingReview/quá SLA → QL001 nhận UnderReview → NeedsRevision, ghi chú bổ sung thông tin cơ sở gắn hồ sơ. Không duyệt hồ sơ chưa có cơ sở; không đổi nội dung homestay |

Thay homestay/hoàn tiền/duyệt cuối/gửi lại và kích hoạt lời mời được kiểm tra bằng fixture test riêng; không hủy đơn hay phát khoản hoàn/voucher mới trên SQL Server của app trong phiên tiếp tục. Service tự ghi audit/thông báo cho các thao tác UI trên.

## Cần người dùng quyết định

Không tự đổi nghiệp vụ. Những phần cần chốt ở bước sau:

- **G-20:** DTO danh sách thiếu tên homestay/người nhận lịch sử; phòng trống chỉ trả Id. Hiện ghi rõ Mã homestay/Phòng #Id/Nhân viên #Id, không dựng tên hoặc gán nhầm “Chưa giao”. Cần bổ sung DTO backend nếu muốn nhãn đầy đủ.
- **G-21:** hiện gọi service kiểm toàn khoảng rồi từng đêm, tối đa 31 đêm, không cache. Cần API ma trận theo lô và kiểm thử tải trước khi tuyên bố đạt NFR ở 150 cơ sở/31 đêm.
- **G-22:** thông báo kết quả hồ sơ gửi chủ có URL Staff mà chủ không có quyền mở. Cần sửa URL tại nguồn sang màn của chủ và quyết định backfill; không mở quyền Staff để chữa link.
- **G-19:** đã nối lời mời/activation bằng service có sẵn. Chuyển link thủ công riêng tư là lựa chọn hiện có trong decisions.md; SMS/Zalo tự động và kênh gửi thực tế cần chốt khi triển khai, không tự gửi tin ngoài app.
- Kỳ tỷ lệ chốt mặc định đầu tháng đến hôm nay là lựa chọn trình bày đã ghi trong decisions.md; có bộ lọc ngày. Nếu muốn kỳ khác, người dùng chốt riêng; công thức và phạm vi actor giữ đúng `ReportService`.

Prompt B9 hiện chỉ là tài liệu cho bước backend tiếp theo. Báo cáo này không thực hiện B9, 05c hoặc FR-27 của 05d.

## Commit và kết thúc phiên

Chia hai nhóm vì màn đơn/đặt hộ/phòng trống/khách/trùng phòng dùng chung actor và component, còn bộ integration test nối xuyên hồ sơ/dashboard/lời mời. Nhóm đầu có bản sao đúng nội dung staged ở `/private/tmp/fuji-05b/commit1-verify` để build và chạy toàn bộ test độc lập; nhóm sau xác minh trên working tree hoàn chỉnh. Các sửa ngắt nội dung dài sau rà giao diện nằm trong nhóm sau. Mỗi commit có dòng kết thúc `Co-Authored-By: Codex <noreply@openai.com>`.

- `d0a2561` — nhóm đơn/đặt hộ/phòng trống/khách hàng/trùng phòng và component: build 0 cảnh báo, **1.639 pass + 1 skip, 0 fail**; log `commit1-build.log`, `commit1-tests.log`. Patch staged đã được so sánh byte với patch xuất sang bản kiểm tra trước khi commit.
- Commit cuối (HEAD) — thẩm định/kích hoạt, dashboard, điều hướng, chặn FR-27, sửa UX, integration test và báo cáo: build 0 cảnh báo, **1.655 pass + 1 skip, 0 fail**. Các thay đổi sau lượt test cuối chỉ là bổ sung bằng chứng vào báo cáo.

Lần thử trong sandbox bị SSL khi nối SQL Server; đã dừng đúng PID và chạy lại với quyền kết nối đầy đủ. Một script thao tác và script chụp từng điều hướng cùng tab, làm lần chuyển phòng đầu bị gián đoạn; đã chạy lại tuần tự và xác nhận lưu thành công, dùng `resume-overbooking-verified.log` làm bằng chứng. Không dùng các log lỗi/ngắt để tuyên bố pass.

App kiểm tra chỉ chạy cổng **5012**. Đã đóng riêng browser `fuji05b`, gửi TERM đúng app PID **14215** và tiến trình cha **14214**, xác nhận cả hai PID kết thúc và 5012 không còn listener. Container **sql-homestay vẫn Up 39 hours** ở lần kiểm tra cuối. Không có lệnh dừng app 5011 hoặc dừng/xóa container. Không push, không đổi nhánh; dừng ở 05b.
