# Bước 05d — Màn quản lý / quản trị viên

Ngày 19/10/2026, nhánh `feature/foundation`. Không đổi nhánh, không push, không tạo ảnh mới. App kiểm thử chạy riêng cổng 5012; không dừng app 5011 hoặc container `sql-homestay`. Không sửa Services, Data, Entities, migration, database hoặc policy.

## Màn và service đã nối

| Màn | Kết quả | Service |
|---|---|---|
| Đối tác và hợp đồng FR-19 | Danh sách tìm/lọc trạng thái, sắp hết hạn 7/30 ngày, phân trang; chủ/homestay, hoa hồng, hạn. Chi tiết, lịch sử, nhãn hiệu lực hôm nay; tạo, gia hạn, chấm dứt với lý do/xác nhận danger. Alias URL thông báo `/Admin/Contracts/{id}` | ContractService.ListAsync/DetailsAsync/CreateAsync/ExtendAsync/TerminateAsync; ContractEligibility |
| Duyệt nội dung FR-27 | Hàng chờ mới/duyệt lại, thời điểm gửi; ảnh hiện tại, mô tả, địa chỉ, phòng/giá hiện hành và tương lai, trạng thái phòng, giấy tờ riêng tư. Duyệt, từ chối có lý do, mở lại Suspended có lý do/xác nhận. URL Staff cũ chuyển tới Admin, Tư vấn vẫn bị chặn | HomestayReviewService.PendingAsync/PendingDetailsAsync/ApproveAsync/RejectAsync/ResumeAsync; HomestayContentService.EditorAsync; OwnerRegistrationQueryService.OpenDocumentAsync |
| Hồ sơ FR-25/26 | Dùng lại màn 05b qua sidebar Admin; hồ sơ Approved có nút tạo hợp đồng theo cơ sở | Giữ các service thẩm định 05b, không dựng lại màn |
| Báo cáo FR-20 | Một trang, năm tab giữ khoảng ngày, StatCard, biểu đồ so sánh cam đất và xu hướng tháng xanh rừng, bảng số liệu thay thế. Tỷ lệ chốt giữ đúng ConversionPercent của service, không tự chia lại | ReportService.ManagementAsync/BookingsAsync/FinancialAsync/EmployeesAsync/HomestaysAsync |
| Tài khoản FR-21 | Tìm, lọc chức vụ/trạng thái, phân trang, chi tiết. Tạo đủ mã/họ tên/SĐT/email/chức vụ/Department; đổi chức vụ/nghỉ việc, khóa/mở có lý do, reset đều xác nhận. Quản lý chỉ đọc; Quản trị viên ghi | AccountAdminService.ListAsync/CreateEmployeeAsync/ChangeEmploymentAsync/SetLockedAsync/ResetEmployeePasswordAsync |
| Danh mục | Rà Amenities/Areas/Holidays/RoomCategories: xóa qua service an toàn. Sửa câu xác nhận Amenities để không hứa gỡ cascade khỏi homestay | CatalogDeletionService.DeleteAsync giữ nguyên |
| Dashboard | Số đơn, tiền phòng hoàn thành, hoa hồng tháng hiện tại; hợp đồng hết hạn trong 30 ngày, nội dung chờ duyệt, hồ sơ PendingReview + UnderReview; lối tắt báo cáo và các màn quản lý | ReportService.ManagementAsync; ContractService.ListAsync; HomestayReviewService.PendingAsync; OwnerRegistrationQueryService.ListAsync |

Tất cả controller mới kế thừa Management; chỉ tra EmployeeId bằng PersonId như quyết định 05b. Mọi POST mới có antiforgery, PRG và TempData/Alerts. Form tạo lỗi giữ trường không bí mật và validation qua TempData. Không giả Actor hoặc truy vấn nghiệp vụ trong controller/view.

Component `_ReportChart` và badge ba enum mới có trong UI Kit. Chart.js 4.4.1 có sẵn; màu đọc `--fx-primary`, `--fx-accent`, tone qua getComputedStyle; MutationObserver vẽ lại khi đổi theme; chuyển động 800ms, reduce tắt animation. Canvas có tên và liên kết bảng HTML. Không JS ẩn canvas, bảng vẫn đọc được. Chỉ bổ sung token fill theo primary và class chiều cao chart; không thêm thư viện, màu hex ngoài tokens hoặc style inline. Giữ DESIGN.md và Bootstrap Icons.

Mật khẩu tạm sinh bằng RandomNumberGenerator, qua TempData mã hóa, đọc một lần tại trang NoStore/no-referrer; có nút sao chép, phản hồi tại nút và xóa giá trị DOM khi rời trang. Không đưa bí mật vào URL, ảnh, log hay báo cáo. Backend chưa bắt đổi mật khẩu lần đầu, UI chỉ nhắc nhân viên tự đổi.

## Kiểm chứng

- `dotnet build -c Release -p:UseSharedCompilation=false`: **0 warning, 0 error**.
- `dotnet test HomestayBooking.sln -c Release --no-build`: **1.689 pass, 1 skip, 0 fail** (1.690 tổng); skip là test hiệu năng opt-in có sẵn. Sau thay đổi nhãn cuối, **21/21 test Admin pass**.
- Node kiểm cú pháp hai module JS pass; `git diff --check` sạch; không có thay đổi Services/Data/Entities/database, style inline, hex mới ngoài tokens hoặc resource EN trùng key.
- **90/90** ảnh theo ma trận màn × 375/768/1280 × sáng/tối: HTTP 200, 0 tràn ngang toàn trang, 0 input thiếu label. Màu cột đổi từ `#a94d2c` sang `#f0ab83`, khớp `--fx-accent`; đường/miền đọc `--fx-primary`, tone sự cố đổi theo token. Số tiền trong bảng đã đối chiếu service và xem ảnh sau sửa.
- Năm tab năm 2028 đều HTTP 200 và hiển thị dữ liệu 0 đúng thực tế; EN có tiêu đề Reports, giữ tiền VND vi-VN. Không JS vẫn có hai bảng tài chính với số tiền đầy đủ, vùng canvas ẩn. Mẫu chart UI Kit hiển thị được.

- Build Release: `/private/tmp/fuji-05d/build-delivery.log`.
- Toàn bộ test: `/private/tmp/fuji-05d/test-final-evidence.log`; test Admin sau thay đổi nhãn cuối: `/private/tmp/fuji-05d/test-admin-delivery.log`.
- Test hồi quy các ô tiền được chạy đỏ trước khi đổi `amount` sang `value` của MoneyTagHelper: `/private/tmp/fuji-05d/money-red.log`. Test đối chiếu trực tiếp `.fx-money` với FinancialAsync, không chỉ đọc StatCard.
- Test mới bao phủ đọc thành công/chặn quyền cho năm controller, antiforgery mọi POST, tạo/gia hạn/chấm dứt và chặn đơn giữ phòng, giới hạn DateOnly.MaxValue, duyệt/từ chối/mở lại, Manager chỉ đọc tài khoản, tạo/Department/lọc/khóa/mở/reset/nghỉ việc, mật khẩu chỉ một lần/NoStore, năm tab năm 2026 và 2028, đối chiếu ConversionPercent/tổng tiền với service, xóa danh mục đang dùng. Fixture dùng CSDL InMemory riêng; ảnh bìa của fixture chỉ là bản ghi tham chiếu, không tạo file ảnh.
- Crawl: 840 trang, không có 500, đúng bốn 404 đã biết: `/Bookings/Create` thiếu roomTypeId và ba thẻ đơn mẫu UI Kit. Log `/private/tmp/fuji-05d/crawl-final.log`.
- Rà theo [Web Interface Guidelines](https://raw.githubusercontent.com/vercel-labs/web-interface-guidelines/main/command.md): nhãn input, tên nút/icon, focus và modal dùng component sẵn có; chart có bảng thay thế; danger cho hành động nhạy cảm; không màu viết cứng/style inline/onclick, không thư viện mới. Ngoại lệ theo DESIGN.md: viết hoa kiểu Việt Nam, VND vi-VN, font/góc/Bootstrap Icons đã chốt.

## Luồng thật và dữ liệu Development

- QL001 mở đủ năm tab với năm **2026** có dữ liệu seed và năm **2028**. SQL Server hiện tại trả 0 đơn/tiền/hiệu suất cho 2028; không biến hợp đồng/ngày lễ seed 2028 thành đơn đặt phòng giả. Ví dụ năm 2026 trên SQL: tiền phòng hoàn thành 52.730.000đ, hoa hồng/doanh thu thuần Fuji 8.191.500đ; tỷ lệ chốt NV001 62,5%, NV002 82,4% theo service.
- QL001 gia hạn hợp đồng **#31 — HD-2026-0031**, homestay **#31 Ba Vì Mountain Lodge**: EndDate **31/05/2028 → 01/06/2028**, hoa hồng giữ **10%**. Có audit, không thay snapshot đơn cũ.
- QL001 thử tạo **UI05D-20261019-1002** cho homestay #1002: bị chặn “Chủ homestay chưa được duyệt.”, **không tạo hợp đồng**. Các cơ sở seed có chủ Approved đang có hợp đồng Active; không chấm dứt hợp đồng thật chỉ để tạo dữ liệu thử. Đường tạo thành công được kiểm tra qua integration test riêng.
- QL001 thử duyệt nội dung đang chờ **#1002 — Nhà Gỗ Thử Giao Diện**: bị chặn, thiếu chủ được duyệt, hợp đồng hiệu lực, loại phòng có phòng bán và ba giá. Trạng thái vẫn PendingReview. **Chưa có lượt duyệt thành công trên SQL Server**; đường duyệt/từ chối/mở lại thành công được kiểm tra trong CSDL test riêng.
- Automatic approval review từ chối việc đổi tạm địa chỉ homestay #1 để chuẩn bị nội dung chờ duyệt hợp lệ: thay đổi dữ liệu nghiệp vụ phía Owner nằm ngoài phần nghiệm thu Admin được cho phép rõ ràng. Không thực thi hoặc tìm đường vòng; địa chỉ #1 và đơn demo **FJ261019-001** giữ nguyên. Đây là giới hạn nghiệm thu thật, không báo là đã duyệt thành công.
- AD001 tạo **UI05D001 — Nguyễn An Kiểm Thử**, SĐT **0905050519**, email **ui05d001@example.test**, chức vụ **Tư vấn**, Department **Vận hành thử nghiệm**, AccountId **d2507b59-e06a-4422-b6ec-cc260760ceed**. Sau đó khóa → mở khóa → reset mật khẩu qua modal; cuối cùng Active/đang làm việc. Tạo và reset đều hiện mật khẩu một lần; sao chép thành công, mở lại URL mật khẩu chuyển về danh sách. Không lưu mật khẩu vào báo cáo/ảnh.
- AD001 thử xóa tiện nghi **#5 Bãi đỗ xe**, đang gắn **22 homestay**: bị chặn “Không thể xóa danh mục vì đang được sử dụng.”; mục/liên kết vẫn còn. Không có danh mục bị xóa.

## Ảnh và kết quả trình duyệt

Ảnh/playwright snapshot chỉ ở `/private/tmp/fuji-05d/` hoặc `.playwright-cli/`, không commit. Không chụp trang chứa mật khẩu tạm.

- Báo cáo: `report-{overview|bookings|finance|employees|homestays}-{1280|768|375}-{light|dark}.png`.
- Các màn: `{dashboard|contracts|contract-create|contract-details|reviews|review-details|amenities|accounts|account-create|account-details}-{1280|768|375}-{light|dark}.png`.
- Bổ sung `report-*-2028.png`, `report-finance-en.png`, `report-finance-nojs.png`, `uikit-chart.png`, `account-lock-confirm.png`; ảnh bản mẫu `/admin` chỉ để tham khảo bố cục.
- Log/metrics trình duyệt và luồng tài khoản cùng thư mục tạm. Kiểm tra theme bằng nút thật, đối chiếu màu dataset với CSS token; kiểm tra không tràn ngang, input có label, bảng/card ở tablet/mobile, EN và không JS.

## Gap và phạm vi còn giới hạn

- **G-26:** chưa có lý do chi tiết hoặc snapshot trước thay đổi/diff; danh sách giấy tờ editor thiếu legacy gắn hồ sơ. Trang duyệt báo rõ bản hiện tại, không dựng so sánh giả. ApproveAsync không nhận lý do, nên chỉ duyệt có xác nhận; Reject/Resume lưu lý do qua service. Nếu cần ghi lý do duyệt cần bổ sung backend.
- **G-27:** lọc chức vụ/đọc một tài khoản phải lấy mọi trang Employee rồi lọc ở bộ nhớ; cần Position/DetailsAsync trong read API nếu số nhân viên lớn.
- Xu hướng tháng gọi API báo cáo theo từng lát ngày trong bộ lọc, tối đa 13 tháng; không cache, không tự tính lại tài chính. Chưa đo tải lớn cho cách ghép này.
- Chưa nghiệm thu thành công tạo hợp đồng/duyệt nội dung trên SQL vì dữ liệu hiện có chưa đủ điều kiện; đã kiểm chứng cả thành công và chặn trong integration test. Không thay đổi nghiệp vụ để làm test qua.
- Mục ẩn/hiện đánh giá FR-06 trong inventory nền chưa nằm trong bảy màn của prompt 05d hiện hành, nên chưa mở thêm màn này.

## Cần người dùng quyết định

1. Có cho phép chuẩn bị một cơ sở/hồ sơ thử đủ điều kiện trong Development để nghiệm thu tạo hợp đồng và duyệt nội dung thành công trên SQL Server hay không. Thao tác đổi tạm địa chỉ để chuẩn bị đã bị automatic approval review từ chối; cần phạm vi dữ liệu/thao tác được cho phép rõ ràng ở lượt sau. Hiện không thay đổi cơ sở hoặc duyệt hồ sơ thật để né điều kiện.
2. Nếu cần so sánh trước/sau, lý do thay đổi chính xác, giấy tờ legacy và lý do khi **duyệt**: bổ sung read DTO/API theo G-26 trước. Hiện giữ bản hiện tại và quyền/kiểm tra của service.
3. Khi cần mở rộng số nhân viên, ưu tiên backend Position + DetailsAsync (G-27). Cửa sổ cảnh báo hợp đồng mặc định 30 ngày và kỳ báo cáo đầu tháng đến hôm nay là lựa chọn hiển thị an toàn, đã ghi decisions; không thay luật hết hạn hoặc kế toán.


## Kết thúc phiên

- Đã commit riêng hợp đồng, duyệt nội dung/hồ sơ, báo cáo, tài khoản và dashboard/nghiệm thu với tiền tố `feat(admin):`; mỗi commit kết thúc bằng `Co-Authored-By: Codex <noreply@openai.com>`.
- Đã dừng đúng app QA PID **17691** ở 5012, đóng ba browser CLI riêng `fuji05d`, `fuji05d-admin`, `fuji05d-owner`. Không chạy pkill/killall, không dừng app 5011. Container `sql-homestay` vẫn Up 44 hours khi kiểm tra cuối.
- Không đổi nhánh, không push, không commit ảnh/video/trace. Các hạn chế nghiệm thu SQL và quyết định cần người dùng nằm ở các mục trên; không báo chúng là đã hoàn tất.
