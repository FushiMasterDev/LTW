# TỔNG QUAN QUY TRÌNH THỰC HIỆN BÀI TẬP LỚN LẬP TRÌNH WEB

**Đề tài:** Hệ thống đặt phòng homestay cho công ty trung gian (Công ty Fuji)
**Công nghệ:** ASP.NET Core MVC (.NET 10) · Entity Framework Core 10 · ASP.NET Core Identity · SQL Server · Bootstrap 5 · jQuery
**Nhóm:** Phạm Ngọc Phú (trưởng nhóm) · Lã Quốc Thái · Nguyễn Quang Nghĩa · Trần Kỳ Vĩ
**Đầu vào:** Báo cáo *Phân tích thiết kế yêu cầu* (`phantichyeucaubanchinh.docx`): khảo sát, quy tắc nghiệp vụ BR-01…BR-13, yêu cầu FR-01…FR-27

---

## Mục lục

1. [Mục tiêu và tiêu chí hoàn thành](#1-mục-tiêu-và-tiêu-chí-hoàn-thành)
2. [Mô hình phát triển](#2-mô-hình-phát-triển)
3. [Phạm vi hệ thống](#3-phạm-vi-hệ-thống)
4. [Đối chiếu yêu cầu của hướng dẫn BTL](#4-đối-chiếu-yêu-cầu-của-hướng-dẫn-btl)
5. [Kiến trúc và công nghệ](#5-kiến-trúc-và-công-nghệ)
6. [Thiết kế cơ sở dữ liệu (tổng quan)](#6-thiết-kế-cơ-sở-dữ-liệu-tổng-quan)
7. [Quy tắc nghiệp vụ áp dụng trong code](#7-quy-tắc-nghiệp-vụ-áp-dụng-trong-code)
8. [Danh sách REST API](#8-danh-sách-rest-api)
9. [Sitemap](#9-sitemap)
10. [Kế hoạch triển khai theo giai đoạn](#10-kế-hoạch-triển-khai-theo-giai-đoạn)
11. [Phân công công việc](#11-phân-công-công-việc)
12. [Quy ước làm việc nhóm](#12-quy-ước-làm-việc-nhóm)
13. [Kế hoạch kiểm thử](#13-kế-hoạch-kiểm-thử)
14. [Bố cục báo cáo và sản phẩm nộp](#14-bố-cục-báo-cáo-và-sản-phẩm-nộp)
15. [Rủi ro và cách xử lý](#15-rủi-ro-và-cách-xử-lý)

---

## 1. Mục tiêu và tiêu chí hoàn thành

| Mục tiêu (theo hướng dẫn) | Cách nhóm đạt được |
|---|---|
| Vận dụng kiến thức Lập trình web để xây dựng hệ thống thực tiễn | Xây dựng hệ thống đặt phòng 4 lớp người dùng dựa trên khảo sát thực tế tại Fuji |
| Tiếp cận quy trình phát triển phần mềm và công cụ hỗ trợ | Agile/Scrum rút gọn, Visual Studio/VS Code, .NET CLI, NuGet, Git/GitHub, draw.io, xUnit, Swagger |
| Rèn kỹ năng làm việc nhóm, báo cáo, thuyết trình | Họp sprint hằng tuần, review code qua Pull Request, viết báo cáo song song với code |

**Hệ thống được coi là hoàn thành khi:**

- Đủ tất cả mục trong checklist ở [mục 4](#4-đối-chiếu-yêu-cầu-của-hướng-dẫn-btl).
- Chạy được trọn luồng: khách đặt phòng/giữ đêm → nộp cọc (QR tự khớp hoặc kế toán duyệt bill) → chủ homestay xác nhận → nhận phòng → hoàn thành → khách đánh giá.
- Không thể tạo hai đơn trùng phòng trùng ngày (kể cả khi gửi đồng thời).
- Có script `.sql` dựng lại được CSDL kèm dữ liệu mẫu trên máy khác.

---

## 2. Mô hình phát triển

Áp dụng **Agile/Scrum rút gọn**, mỗi sprint 1 tuần:

```
Đầu tuần: Sprint planning (30')  →  Code + review PR  →  Cuối tuần: Demo + Retrospective (30')
```

- **Product backlog:** lấy từ FR trong báo cáo PTTKYC, đã rút gọn ở [mục 3](#3-phạm-vi-hệ-thống).
- **Công cụ quản lý:** GitHub Projects (bảng Kanban: *Todo → In progress → Review → Done*).
- **Definition of Done** của một chức năng:
  1. Có Controller + View + Validation, giao diện responsive.
  2. Chuỗi hiển thị đã đưa vào file tài nguyên (i18n).
  3. Đã kiểm tra phân quyền.
  4. Có ít nhất 1 test case trong bảng kiểm thử.
  5. PR được 1 thành viên khác review và merge vào `develop`.
  6. Đã chụp màn hình cho mục 5.3 của báo cáo.

---

## 3. Phạm vi hệ thống

### 3.1. Lớp người dùng

| Nhóm | Định danh/quyền | Phạm vi |
|---|---|---|
| Khách | Guest hoặc role Customer | Tìm/đặt/tra cứu đơn; khách đăng nhập gửi yêu cầu đổi; vẫn tự hủy ngay |
| Chủ homestay | Role Owner; quyền quản lý kiểm hồ sơ đã Approved | Chỉ dữ liệu homestay của mình; xác nhận đơn, quản lý cơ sở/lịch, xem đối soát |
| Tư vấn | Role Employee, Position Consultant | Tạo đơn/gửi yêu cầu đổi hoặc hủy thay khách, giao lại và xử lý đơn |
| Kế toán | Role Employee, Position Accountant | Bill/giao dịch/hoàn tiền, cọc bổ sung, đối soát |
| Quản lý | Role Employee, Position Manager | Duyệt nghiệp vụ đổi/hủy, đối tác/hợp đồng, voucher, báo cáo |
| Quản trị | Role Employee, Position Administrator | Tài khoản và danh mục; không có quyền Manager/Accountant ở hai policy đổi/hủy chuyên biệt |

Không dùng role Guest/Staff/Admin để thay cho ba role Customer/Owner/Employee trong code. Chức vụ và trạng thái làm việc của Employee quyết định quyền chi tiết. Policy ChangeRequestApprove chỉ Manager, ChangeRequestFinance chỉ Accountant.

### 3.2. Phạm vi đã chốt

- Đặt theo **đêm**, mỗi đơn một phòng, khoảng `[CheckIn, CheckOut)`; giữ phòng ngay lúc đặt, 15 phút để nộp tiền.
- Chủ xác nhận sau khi có cọc, hạn 30 phút; QR tự khớp 24/7, bill là dự phòng. Quá hạn chủ xác nhận chỉ cảnh báo nhân viên.
- Chính sách hủy riêng được ưu tiên; có voucher, giao dịch ngân hàng giả lập và luồng cọc bổ sung.
- Thông báo trong ứng dụng đã có; tích hợp gửi Zalo/SMS cần cấu hình và backend riêng. Không có cổng thanh toán hoặc đồng bộ OTA trong phạm vi hiện tại.
- Đối soát hai kỳ theo CheckOut; chủ xác nhận số liệu tùy chọn, kế toán đánh dấu chuyển tiền. Demo chỉ seed tài khoản/dữ liệu mẫu ở Development.

### 3.3. Đối chiếu đủ FR-01…FR-27

Bảng sau truy vết đề bài gốc đến code và test sau B6. Cột Service ghi cả truy vấn controller cũ nếu còn dùng; “thiếu” là giới hạn thực tế, không coi test service là bằng chứng đã có giao diện hoặc kênh Zalo/SMS thật.

| Mã | Yêu cầu đề bài | Tác nhân | Service | Test chính |
|---|---|---|---|---|
| FR-01 | Tìm kiếm homestay theo khu vực, ngày nhận/trả, khoảng giá, loại phòng, số người, tiện nghi; sắp xếp theo giá và điểm đánh giá | Khách hàng | AvailabilityService.SearchAsync, FreeRoomIdsAsync | AvailabilityServiceTests; AvailabilitySeedRegressionTests |
| FR-02 | Xem chi tiết homestay/phòng: hình ảnh, tiện nghi, giá theo ngày, chính sách hủy, đánh giá, lịch phòng trống; hiển thị tổng tiền cuối cùng và số tiền cọc trước khi gửi yêu cầu (BR-01, BR-08) | Khách hàng | PricingService.QuoteAsync; AvailabilityService; RefundService; ReviewService.ListAsync; HomestaysController.Details (truy vấn cũ) | PricingServiceTests; AvailabilityServiceTests; BookingContactVisibilityTests |
| FR-03 | Gửi yêu cầu đặt phòng trực tuyến bằng số điện thoại, không bắt buộc tạo tài khoản | Khách hàng | BookingsController.Create (không bắt đăng nhập; chưa đăng nhập thì không dùng voucher, mã đơn vào lookup session như FR-04); GuestCustomerService.FindOrCreateAsync; BookingService.CreateAsync. Shortcut: chưa xác minh OTP khi đặt không đăng nhập (chờ dịch vụ SMS) | AnonymousBookingTests; AnonymousBookingPageTests; GuestCustomerServiceTests; BookingServiceTests.Create_HoldsRoomImmediatelyFor15Minutes |
| FR-04 | Tra cứu đơn bằng mã đơn + SĐT; xem trạng thái đơn và trạng thái cọc; nhận thông báo qua Zalo/SMS | Khách hàng | BookingsController.Lookup/Details/Status; ReceiptReviewService; MessageService; **thiếu: gửi trạng thái qua Zalo/SMS thật** | ChangeRequestUiTests (partial hiển thị); BookingContactVisibilityTests; ReceiptReviewServiceTests; MessageServiceTests (giả lập) |
| FR-05 | Gửi yêu cầu đổi/hủy đơn, xem trước số tiền được hoàn | Khách hàng | BookingChangeRequestService.QuoteAsync/SubmitAsync/ApproveAsync/ConfirmAsync; RefundService; BookingService.ChangeStatusAsync (khách tự hủy) | BookingChangeRequestServiceTests; BookingChangeRequestServiceTests (partial tài chính); ChangeRequestUiTests |
| FR-06 | Đánh giá homestay sau khi trả phòng (BR-09); chủ homestay được trả lời đánh giá | Khách hàng, Chủ homestay | ReviewService.CreateAsync/CreateByLookupAsync/ReplyAsync/SetHiddenAsync; ReviewRatings.Summaries | ReviewServiceTests |
| FR-07 | Tạo đơn thay khách (đơn nhận qua điện thoại/Zalo) | Nhân viên | StaffBookingService.CreateForCustomerAsync (kênh Phone/Zalo/Messenger/Other, tìm hoặc tạo khách theo SĐT qua GuestCustomerService); BookingService.CreateAsync với Actor nhân viên (D23); CustomerService.SearchAsync | GuestCustomerServiceTests; StaffBookingServiceTests; CustomerServiceTests |
| FR-08 | Tra cứu phòng trống của tất cả homestay, cập nhật gần thời gian thực (≤ 5 giây – NFR-02) | Nhân viên | AvailabilityService.StaffAvailabilityAsync; không cache. Đo trên SQL Server (150 homestay, 1.500 phòng, ~327 nghìn đêm bận/năm): 31 đêm chậm nhất 3,4 giây; tìm kiếm có ngày 0,3 giây | AvailabilityServiceTests; AvailabilityPerformanceTests (opt-in RUN_PERF_TESTS=1) |
| FR-09 | Quản lý đơn: tìm theo mã/tên/SĐT, cập nhật trạng thái, xem lịch sử, giao đơn cho nhân viên khác | Nhân viên | StaffBookingService.SearchAsync/DetailsAsync/AssignAsync/ChangeStatusAsync | StaffBookingServiceTests |
| FR-10 | Xử lý đổi/hủy đơn; đề xuất phòng/homestay thay thế khi trùng phòng (BR-11) | Nhân viên | BookingChangeRequestService; OverbookingService.ReportAsync/AlternativesAsync/MoveRoomAsync/ReplaceAsync/RefundAsync | OverbookingServiceTests; BookingChangeRequestServiceTests; BookingChangeRequestServiceTests (partial tài chính) |
| FR-11 | Nhận cảnh báo khi homestay quá 30 phút chưa xác nhận đơn | Nhân viên | BookingService.AlertOverdueConfirmationsAsync; BookingTimeoutJob | BookingServiceTests; BookingTimeoutJobTests |
| FR-12 | Quản lý thông tin khách hàng | Nhân viên | CustomerService.SearchAsync/DetailsAsync/UpdateAsync | CustomerServiceTests |
| FR-13 | Quản lý thông tin homestay, phòng, hình ảnh, tiện nghi, giá 3 mức, chính sách hủy riêng | Chủ homestay | HomestayContentService; RoomService; HomestayImageService; HomestayPolicyService; HomestayDocumentService | HomestayContentTests; RoomManagementTests; HomestayMediaPolicyTests; HomestayReviewFixTests |
| FR-14 | Cập nhật lịch phòng (trống/đã đặt/bảo trì); khóa ngày cho khách từ kênh khác | Chủ homestay | RoomBlockService.CreateAsync/UnlockAsync/CalendarAsync | RoomBlockServiceTests; HomestayTransactionTests |
| FR-15 | Nhận thông báo; xác nhận/từ chối yêu cầu đặt, đổi, hủy | Chủ homestay | BookingService.ConfirmAsync/ChangeStatusAsync/NotifyAsync; NotificationService; yêu cầu đổi/hủy do Manager duyệt, chủ xác nhận lại đơn sau đổi | OwnerNotificationTests; OwnerBookingTests; BookingChangeRequestServiceTests (partial tài chính) |
| FR-16 | Xem danh sách đơn, lịch nhận – trả phòng, bảng đối soát của mình | Chủ homestay | OwnerScheduleService.ReadAsync; SettlementService.ListAsync/DetailsAsync; Owner/BookingsController.Index/Details (truy vấn cũ) | OwnerScheduleServiceTests; SettlementServiceTests; OwnerBookingsAuthorizationTests |
| FR-17 | Ghi nhận và xác nhận tiền cọc, hoàn cọc gắn với từng đơn | Kế toán | BankMatchingService.ProcessAsync; ReceiptReviewService; AccountingService.RecordRefundAsync/RefundTransactionAsync; BookingChangeRequestService.ConfirmAsync | BankMatchingServiceTests; ReceiptReviewServiceTests; AccountingServiceTests; BookingChangeRequestServiceTests (partial tài chính) |
| FR-18 | Tự động lập bảng đối soát theo kỳ (BR-03), xác nhận đã chuyển tiền | Kế toán | SettlementService.GenerateRecentAsync/GenerateAsync/SendAsync/ConfirmAsync/ReopenAsync/RecordPaidAsync; CompensationService | SettlementServiceTests; CompensationServiceTests; BookingTimeoutJobTests |
| FR-19 | Quản lý homestay đối tác, hợp đồng, tỷ lệ hoa hồng | Quản lý | OwnerRegistrationService; ContractService.CreateAsync/ExtendAsync/TerminateAsync/ExpireAsync; HomestayReviewService | ContractServiceTests; OwnerRegistrationServiceTests; PartnerReviewTests |
| FR-20 | Báo cáo số đơn, doanh thu, hoa hồng, tỷ lệ hủy, hiệu suất nhân viên, điểm đánh giá homestay (nhân viên chỉ xem thống kê đơn của mình – X6) | Quản lý | ReportService.ManagementAsync/BookingsAsync/FinancialAsync/EmployeesAsync/HomestaysAsync (X6) | ReportServiceTests |
| FR-21 | Quản lý tài khoản, phân quyền, danh mục (khu vực, loại phòng, tiện nghi) | Quản trị viên | AccountAdminService.CreateEmployeeAsync/ChangeEmploymentAsync/SetLockedAsync/ResetEmployeePasswordAsync/ListAsync; AdminBootstrapService (Quản trị viên đầu tiên ngoài Development từ cấu hình Bootstrap:Administrator); CatalogDeletionService; CRUD cũ Areas/Admin | AccountAdminServiceTests; AdminBootstrapServiceTests; CatalogDeletionServiceTests |
| FR-22 | Tự động khóa lịch phòng khi homestay xác nhận đơn; tự động mở lại khi đơn đã xác nhận bị hủy (BR-05). D4/D5 đã chốt: giữ từng đêm ngay khi tạo, hết hạn cọc/hủy thì nhả; Completed giữ lịch sử | Hệ thống | BookingService.CreateAsync/ApplyStatusCoreAsync/ExpireOverdueHoldsAsync; BookingChangeRequestService; RoomNights (giữ ngay D4/D5, không chờ Confirmed) | BookingServiceTests; BookingChangeRequestServiceTests; RoomBlockServiceTests |
| FR-23 | Tự động nhắc khách bằng tin ngắn gồm mã đơn, tên homestay, ngày/giờ nhận, SĐT và link chi tiết; địa chỉ/bản đồ/hướng dẫn chỉ xem sau xác nhận qua chi tiết đơn (BR-13). Nhắc chủ lịch đón khách; kênh ngoài chỉ gửi trong giờ cho phép (KS khách câu 6: 49,6% và 28,3%) | Hệ thống | ReminderService.SendCustomersAsync/SendOwnersAsync; MessageService; BookingContactVisibility; **thiếu: IMessageSender Zalo/SMS thật** (shortcut NFR-12 đã chấp thuận) | ReminderServiceTests; ReminderDeliveryTests; MessageServiceTests; BookingContactVisibilityTests |
| FR-24 | Cung cấp môi trường chạy thử với dữ liệu mẫu để hướng dẫn nhân viên, chủ homestay và kiểm thử trước khi vận hành | Quản trị viên | SeedData.InitializeAsync (Development); DevController.SimulateBank; database/data/seed_topup_2028.sql | SeedDataSqliteTests; SeedDataTests; SeedDataMigrationTests |
| FR-25 | Gửi đăng ký trở thành đối tác trực tuyến (thông tin cơ sở, số phòng, khu vực, liên hệ, ảnh giấy phép kinh doanh lưu trú, PCCC, ảnh thực tế); theo dõi trạng thái hồ sơ | Chủ homestay chưa hợp tác | OwnerRegistrationService.RegisterAsync/CreateAssistedAsync/ActivateAsync/UploadDocumentAsync/SubmitAsync; OwnerRegistrationQueryService.MineAsync (trạng thái, ghi chú cần bổ sung, không lộ người thẩm định) | OwnerRegistrationServiceTests; OwnerRegistrationQueryServiceTests; PartnerReviewTests; PartnerWorkflowRegressionTests |
| FR-26 | Thẩm định hồ sơ đăng ký, cập nhật trạng thái hồ sơ (chờ thẩm định / cần bổ sung / từ chối / đã ký hợp đồng), kích hoạt tài khoản homestay (BR-12) | Nhân viên, Quản lý | OwnerRegistrationService.TakeAsync/RequestRevisionAsync/ApproveAsync/RejectAsync/InviteAsync/ResendInviteAsync; OwnerRegistrationQueryService.ListAsync/DetailsAsync/OpenDocumentAsync (hạn 3 ngày làm việc, lọc quá hạn, che số tài khoản với Tư vấn) | PartnerReviewTests; PartnerWorkflowRegressionTests; OwnerRegistrationQueryServiceTests |
| FR-27 | Duyệt nội dung homestay (ảnh, mô tả, giá) trước khi hiển thị cho khách; cảnh báo hồ sơ quá 3 ngày làm việc chưa xử lý (BR-12) | Quản lý, Hệ thống | HomestayReviewService.PendingAsync/ApproveAsync/RejectAsync/ResumeAsync; OwnerRegistrationService.AlertOverdueAsync (SLA hồ sơ chủ) | HomestayReviewTests; HomestayReviewFixTests; OwnerRegistrationSlaTests |

---

## 4. Đối chiếu yêu cầu của hướng dẫn BTL

| # | Yêu cầu | Hiện thực trong hệ thống | Phụ trách |
|---|---|---|---|
| 3.1 | ≥ 2 lớp người dùng, quyền khác nhau | Role Customer/Owner/Employee; Employee.Position Consultant/Accountant/Manager/Administrator, policy và lọc sở hữu ở service/controller | Phú |
| 3.2 | Template + framework front-end | Bootstrap 5, jQuery, Bootstrap Icons, Flatpickr (chọn ngày), Chart.js | Nghĩa |
| 3.3 | **Paging** | Trang tìm kiếm homestay; danh sách đơn, homestay, tài khoản. Lớp tự viết `PaginatedList<T>` (`Skip/Take` + tổng số trang) + partial `_Pager` dùng chung (hoặc gói `X.PagedList.Mvc.Core`) | Nghĩa, Vĩ |
| 3.3 | **Filtering** | Lọc homestay theo khu vực, ngày, giá, số người, tiện nghi; sắp xếp theo giá/điểm; lọc đơn theo trạng thái, ngày, mã, SĐT. Bộ lọc là ViewModel nhận qua query string (`[FromQuery]`), giữ nguyên khi chuyển trang | Nghĩa, Vĩ |
| 3.3 | **AJAX** | jQuery `$.ajax`/`fetch` gọi Web API: kiểm tra phòng trống + báo giá khi chọn ngày; xác nhận/từ chối đơn; đếm thông báo mới; khóa/mở ngày trên lịch. Lọc danh sách đơn không tải lại trang: action trả `PartialView`, jQuery thay nội dung bảng | Nghĩa, Thái |
| 3.3 | **Thêm/sửa/xóa** | Homestay, Phòng, Ảnh, Khu vực, Loại phòng, Tiện nghi, Ngày lễ, Tài khoản, Đơn đặt phòng | Cả nhóm |
| 3.3 | **Session** | `AddSession()` + `UseSession()`; extension `SetObject/GetObject` (JSON) lưu `BookingDraft`: bản nháp đặt phòng qua 3 bước (chọn phòng → thông tin khách → xác nhận); lưu danh sách homestay đã xem gần đây | Phú |
| 3.3 | **Cookies** | `Response.Cookies.Append(...)` với `CookieOptions` (`HttpOnly`, `Expires`, `SameSite`): ghi nhớ bộ lọc tìm kiếm gần nhất; cookie ngôn ngữ của `CookieRequestCultureProvider`; cookie đăng nhập Identity ("Ghi nhớ đăng nhập") | Nghĩa |
| 3.3 | **Validation + Annotation** | `[Required]`, `[StringLength]`, `[Range]`, `[RegularExpression]` cho SĐT VN, `[Display]`, `[DataType]`, `[EmailAddress]`; validation tùy biến kế thừa `ValidationAttribute` + `IClientModelValidator` (`CheckOut > CheckIn`, `CheckIn ≥ hôm nay`); `ModelState.IsValid`; client-side bằng jQuery Validation Unobtrusive (`_ValidationScriptsPartial`) | Cả nhóm |
| 3.3 | **Web API RESTful** | Controller kế thừa `ControllerBase` với `[ApiController]`, `[Route("api/[controller]")]`, nằm chung project MVC; các endpoint ở [mục 8](#8-danh-sách-rest-api); AJAX gọi chính các API này. Có Swagger (`Swashbuckle`) để thử API và chụp hình cho báo cáo | Phú |
| 3.3 | **Upload ảnh** | `List<IFormFile>` + `enctype="multipart/form-data"`; nhiều ảnh cho homestay/phòng; kiểm tra định dạng (jpg/png/webp), ≤ 5 MB (`[RequestSizeLimit]`), đổi tên file bằng GUID, lưu `wwwroot/uploads/` | Thái |
| 3.3 | **Editor cho textarea** | CKEditor 5 hoặc TinyMCE cho mô tả homestay, chính sách hủy; lọc HTML bằng `HtmlSanitizer` trước khi lưu; hiển thị bằng `@Html.Raw` chỉ cho nội dung đã lọc | Thái |
| 3.3 | **Kiểm thử** | Project xUnit cho các service nghiệp vụ (EF Core InMemory/SQLite) + bảng test case thủ công ([mục 13](#13-kế-hoạch-kiểm-thử)) | Vĩ (điều phối) |
| 3.4 | **Responsive** | Bootstrap grid; giao diện chủ homestay ưu tiên điện thoại, khóa ngày ≤ 3 thao tác | Thái, Nghĩa |
| 3.5 | **Bảo mật** | Xem [mục 5.4](#54-bảo-mật) | Phú |
| 3.5 | **Quốc tế hóa** | Tiếng Việt / English: `AddLocalization()` + `AddViewLocalization()` + `AddDataAnnotationsLocalization()`; file `SharedResource.vi.resx` / `SharedResource.en.resx`; `IViewLocalizer`/`IStringLocalizer<SharedResource>` trong View, Controller; `UseRequestLocalization` với `CookieRequestCultureProvider`; định dạng tiền và ngày theo culture | Nghĩa |

---

## 5. Kiến trúc và công nghệ

### 5.1. Công nghệ

| Thành phần | Lựa chọn |
|---|---|
| Framework | **ASP.NET Core MVC** trên **.NET 10**: mô hình Model – View – Controller, Razor Views |
| REST API | Web API của ASP.NET Core (`[ApiController]`), cùng project với MVC |
| ORM / CSDL | **Entity Framework Core 10** (Code First + Migrations) / **SQL Server** |
| Xác thực | **ASP.NET Core Identity** + Roles; `AccountController` tự viết theo MVC (đăng nhập, đăng xuất, đổi mật khẩu) dùng `SignInManager`, `UserManager` |
| Front-end | Razor Views + Tag Helpers, Bootstrap 5, jQuery, jQuery Validation Unobtrusive, Flatpickr, Chart.js |
| Phân trang | `PaginatedList<T>` tự viết (hoặc `X.PagedList.Mvc.Core`) |
| Editor | CKEditor 5 hoặc TinyMCE (self-host trong `wwwroot/lib`), lọc bằng `HtmlSanitizer` |
| Chạy nền | `BackgroundService` + `PeriodicTimer` (mỗi phút): cảnh báo đơn quá 30 phút, tự hủy đơn quá hạn cọc |
| Tài liệu API | Swagger UI (`Swashbuckle.AspNetCore`) |
| Kiểm thử | xUnit + EF Core InMemory/SQLite + Moq |
| Công cụ | Visual Studio 2022 (Windows) hoặc VS Code / JetBrains Rider (macOS), .NET CLI, `dotnet-ef`, Docker (SQL Server trên macOS), GitHub, draw.io |

**Khởi tạo project:**

```bash
dotnet new sln -n HomestayBooking
dotnet new mvc -n HomestayBooking.Web
dotnet new xunit -n HomestayBooking.Tests
dotnet sln add HomestayBooking.Web HomestayBooking.Tests
dotnet add HomestayBooking.Tests reference HomestayBooking.Web
dotnet new gitignore
dotnet tool install --global dotnet-ef
```

**Gói NuGet (project Web):** `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Swashbuckle.AspNetCore`, `HtmlSanitizer` (tùy chọn `X.PagedList.Mvc.Core`).
**Gói NuGet (project Tests):** `Microsoft.EntityFrameworkCore.InMemory` hoặc `Microsoft.EntityFrameworkCore.Sqlite`, `Moq`.

**SQL Server trên macOS** (máy Windows dùng LocalDB hoặc SQL Server Express):

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<mat-khau-manh>" -p 1433:1433 \
  --name sql-homestay -d mcr.microsoft.com/mssql/server:2022-latest
```

### 5.2. Cấu trúc solution

```
HomestayBooking.sln
├── HomestayBooking.Web/                 # ASP.NET Core MVC (.NET 10)
│   ├── Areas/
│   │   ├── Admin/                       # Controllers + Views, [Area("Admin")] [Authorize(Roles="Admin")]
│   │   ├── Owner/                       # Chủ homestay
│   │   └── Staff/                       # Nhân viên tư vấn
│   ├── Controllers/                     # Trang công khai: Home, Homestay, Booking, Lookup, Account, Language
│   ├── Controllers/Api/                 # [ApiController]: HomestaysController, RoomsController, BookingsController, NotificationsController
│   ├── Data/
│   │   ├── ApplicationDbContext.cs      # IdentityDbContext<ApplicationUser>, Fluent API
│   │   ├── Migrations/                  # EF Core Migrations
│   │   └── SeedData.cs                  # role, tài khoản mẫu, 35 homestay mẫu
│   ├── Models/
│   │   ├── Entities/                    # Lớp thực thể
│   │   └── ViewModels/                  # Model cho View + Data Annotations
│   ├── Services/                        # PricingService, BookingService, RefundService, SettlementService, ...
│   ├── BackgroundJobs/                  # ConfirmTimeoutJob, DepositExpiryJob (BackgroundService)
│   ├── Validation/                      # DateAfterAttribute, NotPastDateAttribute (+ JS adapter)
│   ├── Extensions/                      # SessionExtensions, PaginatedList<T>
│   ├── Resources/                       # SharedResource.vi.resx, SharedResource.en.resx
│   ├── Views/Shared/                    # _Layout, _Pager, _SearchFilter, _Notifications, _ValidationScriptsPartial
│   ├── wwwroot/                         # css, js, lib (bootstrap, jquery, ckeditor), uploads/
│   ├── Program.cs                       # đăng ký service + middleware
│   └── appsettings.json                 # connection string, cấu hình upload
├── HomestayBooking.Tests/               # xUnit
└── database/
    ├── HomestayBooking.sql              # script tạo CSDL + dữ liệu mẫu (để nộp)
    └── ERD.drawio
```

**Nguyên tắc:** Controller mỏng. Logic nghiệp vụ (giá, hoàn cọc, chuyển trạng thái, chống trùng) đặt trong `Services/`, đăng ký qua Dependency Injection (`AddScoped`) để viết unit test được.

**Khung `Program.cs`:**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(o => {
        o.Password.RequiredLength = 8;
        o.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews(o =>
        o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(o =>
        o.DataAnnotationLocalizerProvider = (t, f) => f.Create(typeof(SharedResource)));
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddSession(o => { o.IdleTimeout = TimeSpan.FromMinutes(30); o.Cookie.HttpOnly = true; });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<RefundService>();
builder.Services.AddScoped<SettlementService>();
builder.Services.AddHostedService<ConfirmTimeoutJob>();
builder.Services.AddHostedService<DepositExpiryJob>();

var app = builder.Build();

if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
else { app.UseSwagger(); app.UseSwaggerUI(); }

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("vi-VN").AddSupportedCultures("vi-VN", "en-US").AddSupportedUICultures("vi-VN", "en-US"));
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();   // Web API (attribute routing)

await SeedData.InitializeAsync(app.Services);
app.Run();
```

### 5.3. Luồng xử lý một yêu cầu

```
Trình duyệt ──(form)──► Controller (MVC)            ──┐
            ──(AJAX)──► API Controller ([ApiController]) ─┤  Middleware: Session → Authentication → Authorization
                                                       │  [Authorize] + Antiforgery + ModelState
                                                       ▼
                                        Service (nghiệp vụ, transaction) ← DI
                                                       ▼
                                        ApplicationDbContext (EF Core) ──► SQL Server
                                                       ▼
                    ViewResult / PartialViewResult (Razor)   hoặc   JSON (ActionResult<T>)
```

### 5.4. Bảo mật

| Nguy cơ | Biện pháp |
|---|---|
| Truy cập trái phép | `[Authorize(Roles=…)]` cho từng Area; Owner chỉ truy vấn dữ liệu có `OwnerId == currentUser`; khách tra cứu đơn phải có **cả mã đơn và SĐT**; `AccessDeniedPath` trả trang 403 |
| CSRF | Filter toàn cục `AutoValidateAntiforgeryTokenAttribute` cho mọi POST của MVC (Tag Helper `<form>` tự chèn token); API thay đổi dữ liệu gắn `[ValidateAntiForgeryToken]`, AJAX gửi token qua header `RequestVerificationToken` |
| SQL Injection | Chỉ dùng LINQ/EF Core (tham số hóa); nếu cần SQL thô dùng `FromSqlInterpolated` |
| XSS | Razor tự encode; chỉ nội dung từ editor được hiển thị bằng `@Html.Raw` và đã lọc bằng `HtmlSanitizer` trước khi lưu |
| Over-posting | ViewModel riêng cho từng form (hoặc `[Bind(...)]`), không bind Entity trực tiếp |
| Mật khẩu | Identity băm mật khẩu (PBKDF2); `PasswordOptions` (độ dài, độ phức tạp); `LockoutOptions`, khóa sau 5 lần sai |
| Upload độc hại | Kiểm tra phần mở rộng + `ContentType` + chữ ký file (magic bytes) + dung lượng; đổi tên GUID; chỉ phục vụ file tĩnh trong `wwwroot/uploads/` |
| Cookie | `HttpOnly`, `SecurePolicy = Always`, `SameSite = Lax` cho cookie đăng nhập và session |
| HTTPS | `UseHttpsRedirection()`, `UseHsts()` |
| Lộ lỗi | `UseExceptionHandler("/Home/Error")` ở môi trường Production, không hiện stack trace; `UseStatusCodePagesWithReExecute` cho 404/403 |
| Bí mật cấu hình | Chuỗi kết nối thật để trong `dotnet user-secrets` hoặc `appsettings.Development.json` (không đưa lên repo) |
| Truy vết | `BookingStatusHistories` ghi người/thời điểm đổi trạng thái; `AuditLogs` ghi thay đổi dữ liệu nghiệp vụ |

---

## 6. Thiết kế cơ sở dữ liệu (tổng quan)

Nguồn chuẩn: `ApplicationDbContext`, entity và migrations; chi tiết cột ở `fuji_homestay_database_v1.md`. Mô hình code/database thử có **9 migration, 45 bảng gồm __EFMigrationsHistory, 382 cột, 76 FK, 60 CHECK, 93 index ngoài PK** sau EnumAndReviewChecks. **v5 mới áp ChangeRequestWorkflow: 8 migration/44 CHECK; chưa áp migration thứ 9.**

### 6.1. Danh sách bảng hiện tại

| Nhóm | Bảng | Vai trò/ràng buộc chính |
|---|---|---|
| Identity (7) | AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims | Một Account/Person, nhiều vai trò; role có người/thời điểm cấp |
| Hồ sơ (7) | PersonProfiles, Customers, CustomerProfiles, Owners, OwnerRegistrations, Employees, AccountActivityLogs | SĐT unique, Customer không bắt buộc Account; một hồ sơ Owner mở; mã nhân viên unique |
| Pháp lý (1) | VerificationDocuments | Đúng một chủ sở hữu; pháp lý mới gắn từng Homestay, giữ lịch sử cũ |
| Đối tác/danh mục (6) | Areas, Homestays, Contracts, Amenities, HomestayAmenities, HomestayImages | Homestay.Status; hợp đồng có hoa hồng/ngày hiệu lực, một Active; một ảnh bìa |
| Phòng/giá/chính sách (7) | RoomCategories, RoomTypes, Rooms, RoomTypePrices, Holidays, CancellationPolicies, CancellationRules | Giá theo RoomType, DayType, EffectiveFrom; chính sách riêng ưu tiên chung |
| Lịch (2) | RoomBlocks, RoomNights | Khoảng khóa; PK (RoomId, Date), đúng một chủ giữ đêm |
| Đơn (5) | Bookings, BookingNights, BookingCodeCounters, BookingStatusHistories, BookingChangeRequests | Code unique; snapshot giá từng đêm; yêu cầu đổi/hủy hai bước |
| Tiền/voucher (3) | Payments, BankTransactions, Vouchers | Cọc đầu/cọc bổ sung/hoàn/bù; webhook idempotent, voucher có số dư |
| Đối soát (2) | Settlements, SettlementItems | UNIQUE kỳ/homestay; mỗi Booking một Item; snapshot tiền và phí đổi |
| Đánh giá/thông báo/audit (4) | Reviews, Notifications, MessageLogs, AuditLogs | Một đánh giá/đơn; log gửi ngoài và log nghiệp vụ riêng |
| Metadata (1) | __EFMigrationsHistory | Lịch sử migration; không phải dữ liệu nghiệp vụ |

EnumAndReviewChecks chỉ thêm 13 whitelist và 3 CHECK Reply/Year/Decision, không thêm bảng/cột/index. OwnerRegistration NeedsRevision/Approved/Rejected phải có người và thời điểm duyệt; NeedsRevision/Rejected phải có lý do. Năm đối soát 2020–2100; OwnerReply/RepliedAt cùng có hoặc cùng không có.

### 6.2. Cơ chế chống trùng phòng theo đêm

- Từ lúc tạo AwaitingPayment: trong một transaction chọn phòng đang bán, thêm RoomNights cho từng đêm `[CheckIn, CheckOut)`; PK `(RoomId, Date)` chặn đặt đồng thời.
- Mỗi RoomNight thuộc đúng một Booking, RoomBlock hoặc BookingChangeRequest. Khóa ngày và giữ phòng mới khi đổi dùng chung PK này.
- Đổi lịch dùng lại đêm giao nhau; chuyển đêm giữ của yêu cầu về Booking khi Applied, nhả phần cũ không dùng trong cùng transaction.
- Expired/Cancelled/Rejected/NoShow nhả đêm của đơn; Completed giữ đêm lịch sử theo code hiện tại. Không lấy số dòng lịch quá khứ làm số phòng đang bán.
- CheckIn 16/10, CheckOut 18/10 nghĩa là hai đêm 16 và 17; đơn khác được nhận ngày 18. Ngày trả phòng không tính thêm giá hoặc chiếm lịch.
- Service kiểm đủ số đêm, đúng Room/RoomType/Homestay và tổng BookingNights.Price bằng TotalAmount. CHECK/FK riêng lẻ không thay thế kiểm tra liên bảng này.

---

## 7. Quy tắc nghiệp vụ áp dụng trong code

### 7.1. Vòng đời đơn và yêu cầu đổi/hủy

```text
AwaitingPayment → PaymentSubmitted → Deposited → Confirmed → CheckedIn → Completed
       └──────── QR tự khớp ────────────┘
```

- AwaitingPayment giữ phòng 15 phút; gửi bill đúng hạn thì giữ trong lúc chờ kế toán. Cọc QR đầu tự khớp hoặc kế toán duyệt bill rồi mới Deposited.
- Chủ xác nhận Deposited → Confirmed trong 30 phút; không có chuyển Confirmed → Deposited chung cho mọi caller.
- Khách vẫn tự hủy ngay bằng BookingsController.Cancel. Nhân viên được gửi Cancel request thay khách; khách gửi Cancel request bị chặn.
- Yêu cầu đổi: Pending → Manager duyệt → Applied khi không cần tiền thêm; có thêm tiền thì AwaitingPayment → nộp QR/bill CR{id} → Accountant xác nhận → Applied. Có Rejected và Expired.
- Hạn PaymentDeadlineAt là hạn khách nộp, không phải hạn kế toán; đã nộp hợp lệ đúng hạn thì được duyệt sau hạn. Manager/Accountant không được là người gửi; Accountant không được là Manager đã duyệt.
- Applied sửa đơn tại chỗ, giữ mã, thay giá/đêm phòng trong một transaction. Đơn Confirmed về Deposited, ConfirmedAt NULL và chủ xác nhận lại. Phí đổi cộng dồn vào ChangeFeeAmount.
- Booking, Request.Status là concurrency token. Chỉ một yêu cầu Pending/AwaitingPayment mỗi Booking; một khoản AdditionalDeposit chưa bị từ chối mỗi Request.
- Ba Pending cũ thiếu người gửi/tiền chỉ được Manager từ chối có lý do thật, không duyệt hoặc tự bù dữ liệu.

### 7.2. Đối chiếu BR-01…BR-13

| Mã | Quy tắc đang áp dụng | Database/backend |
|---|---|---|
| BR-01 | Cọc mặc định 30%, làm tròn nghìn; phần còn lại trả homestay | PricingService; DepositRate snapshot và CHECK (0,1] |
| BR-02 | Hoa hồng theo hợp đồng, snapshot lúc đặt | Contracts.CommissionRate → Bookings → SettlementItems, CHECK (0,1) |
| BR-03 | Hai kỳ ngày 1–15 / 16–cuối tháng, xếp theo CheckOut | SettlementCalculator; cọc giữ lại trừ hoa hồng, cộng phí đổi theo tỷ lệ trên phần phí |
| BR-04 | >=7 ngày hoàn 100%, >=3 ngày hoàn 50%, dưới 3 ngày 0%; ưu tiên chính sách riêng | RefundService.QuoteCancellationAsync; CancellationPolicyId lưu trên đơn |
| BR-05 | Không trùng phòng/đêm, kể cả khóa ngày và chờ cọc | PK RoomNights; service ghi mọi đêm trong transaction |
| BR-06 | Chủ xác nhận trong 30 phút từ khi có cọc; quá hạn cảnh báo, không tự hủy | ConfirmDeadlineAt/ConfirmAlertSentAt, BookingTimeoutJob |
| BR-07 | FJ + yyMMdd + thứ tự duy nhất | BookingCodeGenerator.NextAsync, counter và UNIQUE Code |
| BR-08 | Giá từng đêm: lễ ưu tiên, cuối tuần thứ Sáu/thứ Bảy, còn lại ngày thường | PricingService.QuoteAsync; phiên bản EffectiveFrom gần nhất <= đêm đó, BookingNights snapshot |
| BR-09 | Đơn Completed mới được đánh giá một lần; chủ trả lời; đánh giá ẩn không vào điểm | ReviewService, ReviewRatings.Summaries; UNIQUE BookingId, CHECK 1–5/Reply; chặn trả lời chỉ khoảng trắng ở service |
| BR-10 | Mã đơn + đúng số tiền; QR tự đối chiếu 24/7, bill dự phòng | BankMatchingService, ReceiptReviewService; khoản bổ sung có CR{id}, không coi là cọc đầu |
| BR-11 | Fuji xử lý thay thế/bù; không tìm được thì hoàn cọc và voucher | OverbookingService: chuyển phòng / đơn thay thế + Compensation / hoàn 100% + voucher D14; Compensation được B5 chi riêng sau Completed |
| BR-12 | Duyệt chủ và từng Homestay riêng; giấy phép/PCCC gắn cơ sở, có hợp đồng và duyệt nội dung | OwnerRegistration, VerificationDocuments, Homestay.Status, Contracts; SLA 3 ngày làm việc từ SubmittedAt đủ hồ sơ |
| BR-13 | Ẩn liên hệ/địa chỉ chính xác trước Confirmed | Backend lọc dữ liệu trả về; nhắc trước nhận phòng chỉ gửi đúng điều kiện |

### 7.3. Các quyết định bổ sung ngày 16/10/2026

Áp dụng D15–D24 ở mục 1 tài liệu database: chủ xác nhận đối soát tùy chọn (Sent hoặc Confirmed → Paid), actor ghi AuditLog; không thêm cột actor/liên kết hồ sơ hoặc mốc đủ hồ sơ. SubmittedAt là mốc đủ hồ sơ. Giá/phòng/ảnh/mô tả sửa trực tiếp có audit; địa chỉ/pháp lý đưa cơ sở về PendingReview. Hợp đồng phải Active và EndDate >= CheckOut, chưa có hợp đồng tương lai. Tắt phòng chỉ chặn bán mới. Doanh số theo CreatedByEmployeeId, xử lý theo AssignedEmployeeId. Chủ từ chối lịch mới sau đổi không tự hoàn phí; Manager có thể cấp voucher.

AuditLog ghi trực tiếp trong service và transaction nghiệp vụ; không làm interceptor. Đây là yêu cầu cho backend tiếp theo, không khẳng định mọi thao tác đã có API/service. Không thêm CHECK Settlements.ConfirmedAt; Paid không bắt buộc chủ đã xác nhận.

### 7.4. Backend tiếp nhận đối tác và hợp đồng (B1)

Đã có service và test, chưa cung cấp giao diện/API tiếp nhận đối tác mới:

Sau review B1: có `ResendInviteAsync` cho WaitingOwnerActivation + Account PendingActivation chưa có mật khẩu; đổi security stamp để vô hiệu token cũ, không ghi token vào audit. Nhân viên chỉ sửa thông tin/ngân hàng và giấy tờ của hồ sơ EmployeeAssisted khi chủ chưa kích hoạt (chưa Account hoặc PendingActivation); sau kích hoạt chỉ chủ được sửa. Thông báo chủ dùng tiếng Việt, không chứa ReviewNote, tối đa 500 ký tự. Audit Insert của OwnerRegistrations/Homestays/Contracts/VerificationDocuments dùng Id thật, lưu entity rồi audit trong cùng transaction mặc định; unique index vẫn chặn trùng.

- `OwnerRegistrationService`: chủ tự đăng ký tạo Person/Owner/Account role Owner + Draft + Homestay Draft; không tự ghép SĐT có chủ/tài khoản khác. Nhân viên tạo thay không có mật khẩu; gửi mời sang WaitingOwnerActivation, chủ tự đặt mật khẩu bằng token mời + SĐT rồi trở về Draft. Chưa có OTP, cần nâng cấp SMS sau.
- Nộp mới đủ họ tên/SĐT/ngân hàng và ít nhất một cơ sở có giấy phép kinh doanh, PCCC, ảnh thực tế còn hạn → PendingReview; chỉ cơ sở đủ chuyển PendingReview. SubmittedAt ghi lúc nộp đủ, nộp lại sau NeedsRevision tính lại SLA. Chủ tự bấm nộp, nhân viên không nộp thay.
- PendingReview → UnderReview do Consultant/Manager nhận; UnderReview → NeedsRevision (Consultant/Manager, có ghi chú), Approved/Rejected (Manager/Administrator, khác người tạo thay; từ chối có lý do). Approve chỉ duyệt chủ, cập nhật security stamp, không tự Active homestay. Reject đưa cơ sở PendingReview của chủ về Draft.
- Giấy tờ mới gắn HomestayId; sửa/xóa qua B1 chỉ khi hồ sơ Draft/WaitingOwnerActivation/NeedsRevision và cơ sở Draft, hoặc hồ sơ NeedsRevision và cơ sở PendingReview; PendingReview/UnderReview của hồ sơ khóa sửa cả khi cơ sở còn Draft; Active/Suspended/Ended luôn bị chặn. ReceiptFileStore lưu riêng tư ngoài wwwroot, kiểm chữ ký ảnh và 5 MB. Giấy tờ cũ gắn hồ sơ và homestay Active không bị chuyển liên kết hay bắt bổ sung PropertyPhoto (B1.H). Quy tắc đủ giấy tờ không chặn đặt phòng/ký/gia hạn của cơ sở cũ.
- `ContractService`: Manager/Administrator tạo cho chủ Approved, homestay chưa Ended; hoa hồng 10–15%, StartDate không tương lai, EndDate > hôm nay, tối đa một Active. Gia hạn = tăng EndDate hợp đồng Active, giữ tỷ lệ. Đổi tỷ lệ = chấm dứt + tạo mới; đơn cũ giữ snapshot. Chấm dứt cần lý do, chặn đơn thuộc HoldsRoom trừ Completed và trả mã đơn. Completed vẫn giữ đêm lịch sử nhưng không chặn chấm dứt (đã chốt sau review B1).
- `ContractEligibility` dùng một biểu thức Active + StartDate <= hôm nay + EndDate >= CheckOut trong đặt phòng, báo giá/gửi/duyệt đổi, lọc tìm kiếm và xem trước voucher. Không có ngày trả thì lọc đến hôm nay.
- `BookingTimeoutJob` gọi hết hạn hợp đồng (EndDate < ngày VN), audit System và báo Manager. SLA hồ sơ = ba ngày làm việc sau SubmittedAt, giữ giờ VN, bỏ thứ Bảy/CN và Holidays; chỉ cảnh báo PendingReview/UnderReview, không NeedsRevision. Chống lặp bằng Notification.Url + CreatedAt sau SubmittedAt; nộp lại được cảnh báo mới.
- Audit trực tiếp trong service/transaction, JSON cũ/mới, AccountId thật; job AccountId NULL và ghi System. Không thêm schema hay interceptor.
- `database/tests/data_consistency.sql` đối chiếu 23 mục liên bảng bằng SELECT, in OK/LỆCH và tổng; có LỆCH thì `sqlcmd -b` thất bại, dừng báo lại. Không tự sửa dữ liệu.

### 7.5. Backend quản lý cơ sở, lịch và duyệt homestay (B2)

Đã có service + test, chưa làm giao diện/controller mới. Không thêm schema; mọi audit dùng Id thật, lưu entity rồi audit trong transaction mặc định.

- Quyền sửa dùng chung HomestayAccessService: chủ Active, không là nhân viên, cơ sở chưa Ended; Consultant chỉ Draft/PendingReview + hồ sơ tạo thay/chủ chưa kích hoạt theo B1; Manager/Administrator hỗ trợ cơ sở chưa Ended nhưng không vượt khóa hồ sơ. Khi có hồ sơ OpenStatuses, mọi actor chỉ sửa nếu hồ sơ Draft/WaitingOwnerActivation/NeedsRevision + homestay Draft, hoặc NeedsRevision + homestay PendingReview; PendingReview/UnderReview khóa cả nội dung/vị trí/ảnh/phòng/giá/chính sách/giấy tờ. Dùng chung EnsureDraftEditable với UpdateDraftAsync và EditableHomestayAsync B1; Accountant/chủ khác bị chặn.
- Nội dung/tên/liên hệ E.164/giờ nhận-trả/tiện nghi sửa thẳng, HTML qua HtmlSanitizer. Sửa địa chỉ/khu vực/tọa độ trên Active → PendingReview; trên Suspended lưu và giữ Suspended. Cả hai báo Manager các mã đơn Confirmed/Deposited sắp tới để gọi khách, không sửa đơn cũ.
- Loại phòng tạo cùng đủ ba giá hôm nay; danh mục/sức chứa hợp lệ, tên/mã phòng unique theo cơ sở/loại. Không xóa cứng. Active không được tắt phòng/loại cuối khiến không còn loại bật có phòng bật; thông báo dùng khóa ngày hoặc đề nghị Manager tạm ngưng. Draft/PendingReview/Suspended vẫn tắt được. Tắt loại/phòng chặn mọi lựa chọn mới ở Create/FreeRoomIds/đổi phòng Confirm/yêu cầu đổi kể cả kế toán/tìm kiếm, giữ đơn và đêm đã nhận; trả mã đơn sắp tới.
- Giá theo EffectiveFrom: chỉ thêm ngày >= hôm nay; cập nhật dòng cùng khóa chỉ nếu còn tương lai, không sửa/xóa giá <= hôm nay. PricingService chọn giá gần nhất không sau đêm ở; BookingNights cũ không đổi.
- Ảnh công khai lưu uploads/homestays/{id}/GUID, dùng chung nhận dạng ReceiptFileStore, <= 5 MB, loại phòng cùng cơ sở. Đổi bìa thả bìa cũ SaveChanges rồi bật bìa mới cùng transaction; Active không xóa bìa duy nhất. File mới dọn nếu DB lỗi, file xóa sau commit, không xóa ảnh demo/URL ngoài.
- Giấy tờ B2 dùng pipeline B1; Active/Suspended chỉ thêm pháp lý: Active về PendingReview, Suspended giữ nguyên; cả hai báo Manager, không xóa bản cũ. PropertyPhoto/Other trên Active không đổi trạng thái. Giữ liên kết lịch sử; không áp chuẩn ba giấy tờ mới trên tất cả cơ sở cũ.
- Chính sách riêng đổi bằng tắt cũ → SaveChanges → tạo mới/rules, đơn cũ giữ chính sách cũ. Bỏ riêng là tắt, quay về chung. Rules có mốc 0, không trùng ngày, ngày >= 0, % 0–100 không giảm khi hủy sớm hơn.
- Khóa ngày [StartDate, EndDate) từ hôm nay, Note <= 255, chủ hoặc Manager/Administrator. Trùng lịch báo mã đơn/BL{id}/CR{id} + ngày; PK RoomNights chặn đua, transaction rollback sạch. Lịch tối đa 62 ngày dùng một truy vấn RoomNights cho tất cả phòng, đủ bốn trạng thái.
- Mở khóa D3: StartDate >= hôm nay xóa cả block/đêm cascade; StartDate < hôm nay < EndDate cắt phần từ hôm nay, giữ đêm cũ; EndDate <= hôm nay báo “Khóa ngày đã kết thúc, không cần mở khóa.”, không audit. Không sửa CHECK EndDate > StartDate.
- Chủ Approved thêm cơ sở Draft, tự nộp cơ sở mới khi đủ giấy phép/PCCC/ảnh thực tế còn hạn gắn cơ sở, không tạo hồ sơ chủ mới. Cơ sở đã từng duyệt (ApprovedAt khác NULL) nộp lại chỉ cần giấy phép/PCCC, chấp nhận giấy tờ gắn hồ sơ Approved cũ cùng chủ, không bắt ảnh thực tế. Manager/Administrator duyệt nếu chủ Approved, hợp đồng hiệu lực, giấy phép/PCCC còn hạn (hồ sơ Approved cũ cùng chủ được nhận, không buộc ảnh thực tế lại), loại/phòng bật + đủ ba giá hôm nay, ảnh bìa và địa chỉ/liên hệ/mô tả. Thiếu trả danh sách, giữ trạng thái.
- CanTransition: Draft → PendingReview/Ended; PendingReview → Active/Draft/Ended; Active → PendingReview/Suspended/Ended; Suspended → Active (chỉ ResumeAsync)/Ended; Ended không chuyển. Từ chối/tạm ngưng/mở lại cần lý do audit, không ghép lý do vào thông báo. Ended chặn hợp đồng Active hoặc đơn HoldsRoom trừ Completed. Duyệt đặt ApprovedByEmployeeId/ApprovedAt, thông báo chủ.
- Danh sách PendingReview xếp theo audit chuyển trạng thái gần nhất (thiếu audit có mốc NULL, xếp trước); không thêm SLA cho homestay. Mọi thông báo qua PartnerServiceSupport.Notify <= 500 ký tự.
- Quote/Submit yêu cầu đổi mới chỉ nhận cơ sở Active; CanChangeAsync trả false khi cơ sở không Active. Manager Approve/Accountant Confirm của yêu cầu đã gửi được tiếp tục khi PendingReview qua tham số TargetAsync, vẫn kiểm phòng/loại bật và hợp đồng D20. Suspended/Ended chặn cả hai nhóm; Suspended chỉ mở lại qua ResumeAsync kiểm đủ điều kiện, không qua sửa vị trí/giấy tờ rồi Approve. Luồng hủy không đổi.
- data_consistency.sql giữ 14 mục B1 và thêm năm mục B2: ảnh bìa, đủ giá hôm nay, có loại/phòng bật, đêm khóa trong khoảng, ảnh đúng cơ sở của loại phòng. Chỉ SELECT; có LỆCH thì dừng, không sửa seed/dữ liệu.

### 7.6. Backend tìm phòng, quản lý đơn/khách và trùng phòng (B3)

Đã có service + test; không thêm view/controller quản trị mới, không thay schema. Controller khách hiện có chỉ gọi service thay các truy vấn lặp.

- AvailabilityService dùng chốt Active + ContractEligibility.Covering(hôm nay, CheckOut) + loại/phòng bật + không có RoomNights trong [nhận,trả). BookingService.FreeRoomIdsAsync và các trang tìm/chi tiết/nổi bật dùng chung. Phòng khóa và đêm giữ yêu cầu đổi đều chiếm lịch. Không có ngày chỉ kiểm hợp đồng đến hôm nay, giá “từ” là ngày thường hiện hành; có ngày thẻ, lọc MinPrice/MaxPrice và sắp xếp giá dùng trung bình mỗi đêm = RoundToThousand(tổng kỳ ở / số đêm) của loại rẻ nhất còn trống và đủ sức chứa. AvailabilityOffer.TotalPrice giữ tổng kỳ ở cho nhân viên và đề xuất trùng phòng. Filter khu vực/từ khóa/danh mục/đủ tất cả tiện nghi/giá min–max; sắp tên mặc định, giá tăng/giảm, điểm Review không ẩn. Nạp giá/Holidays một lần, tính thuần bằng PricingService.QuoteFromPrices; không gọi QuoteAsync theo từng loại. Giá thẻ và bộ lọc khớp nhãn `/đêm` của view cũ, không sửa view.
- StaffAvailabilityAsync cho Consultant/Manager/Administrator, tối đa 31 đêm, trả số trống/tổng phòng đang bán và tổng giá từng loại; một truy vấn RoomNights, không cache. Loại đã kín vẫn hiện 0 phòng trống.
- StaffBookingService tìm theo mã/người ở/tên khách/SĐT E.164, lọc trạng thái/cơ sở/ngày nhận/người phụ trách/chưa giao và phân trang; chi tiết có BookingNights, history, yêu cầu đổi, Payments, audit theo thời gian. Shortcut Contains chấp nhận quét, không thêm index. Manager/Administrator giao mọi đơn; Consultant chỉ tự nhận đơn chưa giao hoặc giao lại đơn mình phụ trách; người nhận Consultant/Manager đang hoạt động. UPDATE có điều kiện chặn cùng nhận; CreatedByEmployeeId giữ nguyên, AssignedEmployeeId là người xử lý (D23), audit + báo người nhận.
- Nhân viên xác nhận/đổi phòng lúc xác nhận, từ chối, CheckedIn/Completed/NoShow sau khi gọi chủ: ghi chú bắt buộc, gọi BookingService, history/audit actor Employee. Không tự hủy trực tiếp, không xử lý tiền; hủy theo yêu cầu hiện có. NoShow chỉ từ ngày sau CheckIn.
- CustomerService chỉ Consultant/Manager/Administrator: tìm tên/SĐT/email, xem đơn/voucher dùng được. Note <=1000: null giữ nguyên ghi chú cũ, chuỗi rỗng xóa ghi chú (lưu NULL); không đổi SĐT. Chỉ sửa tên/email khi khách chưa có Account. Consultant nhận DTO CCCD/mã sinh viên che, chỉ bốn ký tự cuối (giá trị ngắn che hết); Manager/Administrator xem đủ, không trả entity Profile thô.
- Báo trùng: chủ đúng cơ sở hoặc nhân viên, chỉ Confirmed trước CheckOut, chặn khi có Pending/AwaitingPayment. Audit và báo người phụ trách + Manager. Đề xuất đủ sức chứa/bán được, cùng homestay → cùng khu vực → nơi khác, kèm tổng mới/chênh lệch/điểm; không lộ liên hệ cơ sở mới trước xác nhận.

| Cách | Trạng thái và lịch | Dòng tiền |
|---|---|---|
| 1. Chuyển phòng cùng loại/cơ sở | Staff chuyển tại chỗ, giữ Confirmed/mã, history “Đổi phòng do trùng phòng” | Không đổi giá/cọc/voucher/hoa hồng, không tạo khoản tiền |
| 2. Đơn thay thế | Staff tạo N dùng chung CreateInTransactionAsync; cũ Cancelled, ReplacedByBookingId=N và nhả phòng; N Deposited chờ chủ mới xác nhận | N giữ cọc/tỷ lệ/voucher/giảm cũ, khách không nộp thêm cọc; giá/hoa hồng/hủy lấy mới, tổng mới >= cọc cũ. Cũ RefundAmount=0, không trả voucher; Compensation trên N=max(0,tổng mới−tổng cũ), ManualTransfer/Submitted, B5 chi riêng sau Completed, không cộng tổng đối soát/không trừ homestay cũ |
| 3. Không tìm được | Chỉ Manager/Administrator hủy và nhả phòng | RefundAmount=AmountDue, trả voucher 100% qua quy tắc còn hiệu lực hiện có, cấp voucher xin lỗi D14; khoản hoàn chờ kế toán, không tự tạo Payment Refund |

Cách 2 giữ khách/người ở/ngày/số khách/kênh/người tạo, giao người xử lý. Fuji bù phần tăng nên khách còn trả tổng cũ−cọc cũ; khi tổng mới giảm, tiền còn trả giảm theo. Chủ mới từ chối N thì luồng Rejected hiện có hoàn 100% AmountDue và trả voucher. Đơn có ReplacedByBookingId bị loại toàn bộ khỏi SettlementCalculator. Thông báo mã N, chủ mới chờ xác nhận; địa chỉ/SĐT/hướng dẫn chỉ mở sau Confirmed (BR-13). Mọi bước ghi audit trực tiếp, Id thật; transaction mặc định rollback cả đơn, counter, đêm, cọc/voucher/Compensation/history/audit/thông báo khi có lỗi, kể cả tranh phòng.

Hàm tách dùng chung: AvailabilityService.BookableHomestays/SellableRooms/NightsDuring; PricingService.QuoteFromPrices; BookingService.CreateInTransactionAsync/MoveRoomAsync/ApplyStatusCoreAsync/ApplyFujiCancellationAsync. data_consistency.sql có 23 mục: giữ 19 cũ + đơn được thay chưa hủy, N khác khách/ngày, Compensation không gắn N, AssignedEmployeeId sai vị trí; chỉ SELECT, có lệch thì dừng.

---

### 7.7. Backend đánh giá, nhắc lịch và lịch chủ (B4)

Chỉ service + test, chưa bổ sung giao diện; không thêm schema/index/migration.

- FR-06/BR-09: ReviewService nhận đánh giá Completed một lần qua tài khoản khách đúng đơn hoặc mã + SĐT ContactPhone/SĐT khách chuẩn E.164; HomestayId lấy từ đơn, Rating 1–5, Comment trim ≤ 1000, rỗng NULL. Lưu văn bản nguyên bản, bên hiển thị phải encode, không Html.Raw. Chủ đúng homestay trả lời/sửa, trim bắt buộc 1–1000 ký tự, cập nhật RepliedAt; không xóa. Manager/Administrator ẩn/hiện với lý do audit bắt buộc. Phân trang mới nhất trước; chỉ quản lý đọc dòng ẩn, ReviewRatings.Summaries dùng chung Availability để không tính điểm dòng ẩn. Unique bảo vệ hai lần gửi đồng thời. Chủ nhận thông báo đánh giá, khách nhận phản hồi và lời mời khi Completed.
- NFR-07/NFR-12: IMessageSender mặc định chỉ giả lập (chờ duyệt dự toán), không gọi dịch vụ ngoài thật. Ba bước gửi: (a) transaction ngắn kiểm chống lặp + tạo thông báo app và Pending nếu số nhận hợp lệ rồi commit; (b) sender NGOÀI transaction; (c) transaction ngắn lưu Sent/SentAt hoặc Failed/ErrorMessage ≤ 500. Zalo lỗi tạo Pending SMS trong cùng bước (c), commit rồi mới gửi SMS. Không giữ khóa đơn/chủ trong lúc chờ, không Serializable; nghiệp vụ phải commit trước SendAsync. Không lưu nội dung tin/nguyên lỗi nhà cung cấp, không tạo AuditLog lặp cho MessageLog; số nhận E.164. Sent với sender mặc định chỉ là giả lập thành công.
- FR-23: ReminderOptions validate startup: CustomerDaysBefore 1 (0–365), SendHour 9 (0–23), QuietFromHour 21 (1–23), bắt buộc SendHour < QuietFromHour. Khách Confirmed có CheckIn trong [hôm nay, hôm nay + N]; chỉ gửi [9, 21) giờ VN kể cả nhận hôm nay. 07:00 đợi 09:00; 23:30 đợi khung giờ hợp lệ tiếp theo. Tin ≤ 160 ký tự chỉ gồm mã đơn, tên homestay (rút nếu cần), ngày/giờ nhận, SĐT cơ sở, link chi tiết với Reminders:PublicBaseUrl (HTTP/HTTPS tuyệt đối ≤ 50 ký tự, không userinfo/query/fragment, mặc định http://localhost:5011; thay URL công khai khi triển khai). Địa chỉ/bản đồ/hướng dẫn chỉ ở trang chi tiết sau BR-13. CheckinReminder + BookingId bỏ qua Sent/Pending từ ConfirmedAt; tối đa 3 lượt Zalo→SMS mỗi lần xác nhận. Pending quá 10 phút → Failed, lỗi “Quá thời gian chờ kết quả gửi”, mới thử lại; lưu kết quả lỗi giữ Pending nên lượt sau không gửi lại ngay. Xác nhận lại sau đổi được nhắc lại. App khách tạo ngay Reserve, chống lặp bằng URL riêng của lời nhắc + CreatedAt >= ConfirmedAt; cả khách/chủ vẫn chỉ báo app trong cùng khung giờ, không báo giữa đêm.
- Nhắc chủ: gộp đơn Confirmed đón ngày mai theo homestay, cùng khung [SendHour, QuietFromHour); OwnerArrivals, BookingId NULL, gửi ContactPhone cơ sở, cùng SĐT (kể cả khác chủ) gộp đủ lịch một tin, app chỉ hiện lịch của đúng chủ. Thông báo app tạo ngay Reserve, độc lập Zalo/SMS, tách ≤ 500; Notification.Url chống lặp chủ/ngày. Homestay không có SĐT hợp lệ vẫn báo app, không tạo MessageLog. Metadata template/số nhận/ngày cùng Pending/Sent chống lặp ngoài, Failed có thể thử lại; chủ chưa Account không tạo Notification vô chủ. Transaction ngắn khóa cùng thứ tự đơn/chủ → MessageLog; sender ngoài transaction. BookingTimeoutJob giữ scope/try-catch riêng. Khi nối Zalo ZNS/SMS brandname thật cần đăng ký mẫu tin, thay IMessageSender sang mã mẫu + tham số và chống giao lặp phía nhà cung cấp khi kết quả gửi chưa xác định sau timeout.
- BR-13: BookingContactVisibility.CanView chỉ Confirmed/CheckedIn/Completed, dùng chung ShowAddress/ReminderService. Details lược địa chỉ/SĐT/tọa độ/hướng dẫn từ entity AsNoTracking trước khi trả view model. Đã rà view model công khai/Home/Homestays (chỉ khu vực), AvailabilityOffer và Overbooking alternatives (không liên hệ), thông báo Booking/đổi/hủy/thay thế (mã/trạng thái, không liên hệ trước xác nhận); bỏ ghép lý do vào thông báo từ chối cọc bổ sung. Trang Staff/Owner đọc theo quyền, không đổi UI.
- FR-15: thông báo chủ cho đơn cọc chờ xác nhận, nhân viên xác nhận/từ chối thay chủ, đổi áp dụng về Deposited cần xác nhận lại trong Booking:ConfirmMinutes (mặc định 30 phút), hủy do khách/nhân viên/trùng phòng, đơn thay thế mới và đánh giá mới. Dùng BookingService.NotifyAsync/NotifyChangeRequestResult, không chép logic ở từng caller; nội dung tiếng Việt ≤ 500 ký tự.
- FR-16: OwnerScheduleService đọc khoảng ngày gồm hai đầu ≤ 31 ngày; nhận = Confirmed/CheckIn, trả = CheckedIn/CheckOut, trả mã/người ở/SĐT/số khách/phòng/ghi chú. Chủ chỉ xem của mình; Manager/Administrator được hỗ trợ, nhân viên khác bị chặn. Đối soát chủ đọc qua SettlementService B5 (backend, chưa thêm UI).
- Audit trực tiếp bằng PartnerServiceSupport.Audit, Id thật, JSON cũ/mới, actor cho thay đổi nghiệp vụ. MessageLog đã là nhật ký gửi tin nên không thêm audit mỗi lần thử; không lưu nội dung tin. data_consistency.sql 26 mục giữ nguyên, chỉ SELECT, có lệch thì dừng báo lại.

### 7.8. Backend đối soát và chi khoản bù (B5)

- SettlementService dùng duy nhất SettlementCalculator.ItemFor/Build cho công thức tiền (mục 7.3 tài liệu database); FujiNetRevenue trả CommissionAmount − DiscountAmount snapshot để báo cáo B6. Kỳ theo CheckOut, gồm Cancelled giữ cọc/NoShow, loại đơn đã được thay và đơn đã nằm ở bảng khác. Không tạo bảng rỗng, vẫn lập cho cơ sở Ended.
- Kỳ hợp lệ khi End + GraceDays < hôm nay VN; Settlements:GraceDays mặc định 2 (0–30), LookbackPeriods 4 (1–48), GenerateEveryMinutes 60 (5–1440), DataAnnotations + ValidateOnStart. Job chỉ quét đúng số kỳ đã hết grace gần nhất, có thể lập tay kỳ cũ bởi Accountant/Manager/Administrator. Job lập kỳ chỉ chạy khi đã đủ GenerateEveryMinutes tính từ lần thành công trước; mốc giữ trong instance và chạy lại ngay sau khi tiến trình khởi động lại. Lần lỗi không cập nhật mốc, được thử lại ở phút sau. Đọc/Build/so sánh Draft trước khi mở transaction: không đổi thì không khóa hay ghi; chỉ khi cần tạo/sửa mới khóa homestay, đọc và tính lại dưới khóa. Bảng không Draft bỏ qua không khóa.
- Draft tính lại từ đơn hiện tại trong transaction mặc định, xóa/lưu dòng cũ trước thêm dòng mới để không vướng unique BookingId. Sent/Confirmed/Paid không tính lại. Nháp cũ trở rỗng giữ lịch sử nhưng chặn gửi; không xóa bảng.
- Đơn chưa chốt = Confirmed/CheckedIn/Deposited/PaymentSubmitted/AwaitingPayment có CheckOut trong kỳ. Draft trả mã đơn; Send chặn khi còn mã chưa chốt, rồi kiểm lại snapshot và chặn rỗng trước khi gửi.

| Từ | Tới | Quyền |
|---|---|---|
| Draft | Sent | Accountant, SentAt |
| Sent | Confirmed | Chủ đúng cơ sở, tùy chọn, ConfirmedAt |
| Sent | Paid / Draft | Accountant, trả hoặc mở lại |
| Confirmed | Paid / Draft | Accountant, trả hoặc mở lại |
| Paid | Không có | Bất biến |

- Chủ xác nhận không bắt buộc (D15), không thêm CHECK ConfirmedAt. Mở lại cần lý do, xóa SentAt/ConfirmedAt và tính lại Draft, audit giữ lịch sử, báo chủ. Chủ phản hồi 1–500 ký tự cho bảng không Draft, audit/báo kế toán, không đổi trạng thái. Paid ghi PaidAt/PaidByEmployeeId, cần mã BankRef 1–100 nếu PayoutAmount > 0, luôn cần đủ ba trường ngân hàng chủ. D16 actor gửi/xác nhận/trả nằm trong AuditLog, không thêm cột.
- Kế toán/Manager/Administrator đọc/lập tay, chỉ Accountant gửi/mở lại/trả; chủ chỉ đọc bảng không Draft của mình, xác nhận/phản hồi; Consultant bị chặn. Chi tiết có dòng snapshot/mã đơn, đơn chưa chốt nếu Draft, lịch sử audit và khoản bù chỉ hiển thị. Thông báo chủ/kế toán tiếng Việt <= 500, không ghép lý do dài.
- CompensationService: Payment Compensation Submitted chỉ được chi khi đơn thay thế Completed, chưa được thay tiếp. Accountant chỉ ghi nhận khi chủ homestay của đơn thay thế đủ BankName/BankAccountNo/BankAccountName (dùng chung kiểm tra với trả đối soát), ghi BankRef -> Approved, ReviewedBy/At, audit/báo chủ. Dùng chung AccountingService.RecordTransferInTransactionAsync với RecordRefundAsync; helper kiểm mã, chốt trạng thái/người/thời gian và audit bằng Id thật, cùng transaction. Không gọi ngân hàng thật.
- Khoản bù chi riêng, KHÔNG cộng đối soát vì CHECK Payout = Retained − Commission và không thêm cột. Không trừ homestay cũ (Q3). N kết thúc Rejected/Cancelled/Expired/NoShow hoặc bị thay tiếp -> job từ chối khoản bù Submitted, giữ dòng/lý do/ReviewedAt, audit System. Accountant được từ chối tay trước Completed với lý do 1–500.
- BookingTimeoutJob thêm hai tác vụ riêng lập kỳ (mặc định mỗi 60 phút sau lần thành công) và từ chối khoản bù treo (mỗi phút), mỗi scope/try-catch riêng. Khóa homestay/đơn trong transaction mặc định, không Serializable; unique hiện có là chốt cuối. Không thêm bảng/cột/index/migration, chưa thêm view/controller/giao diện.
- data_consistency.sql giữ đủ 26 mục cũ, thêm 7 mục liên quan đối soát/Compensation: tổng 33, chỉ SELECT trên DeployTest/ConstraintTest/v5, có lệch thì dừng báo lại.

## 8. Danh sách REST API

| Method | Endpoint | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/homestays?area=&checkIn=&checkOut=&guests=&minPrice=&maxPrice=&amenities=&sort=&page=` | Tìm kiếm, lọc, phân trang | Công khai |
| GET | `/api/rooms/{id}/availability?from=&to=` | Danh sách ngày đã kín của phòng (cho lịch) | Công khai |
| GET | `/api/rooms/{id}/quote?checkIn=&checkOut=` | Báo giá: tổng tiền, cọc, giá từng đêm | Công khai |
| POST | `/api/bookings` | Tạo yêu cầu đặt phòng | Công khai / Staff |
| GET | `/api/bookings/{code}?phone=` | Tra cứu đơn | Công khai (cần đúng SĐT) |
| POST | `/api/bookings/{id}/confirm` | Xác nhận đơn | Owner |
| POST | `/api/bookings/{id}/reject` | Từ chối đơn | Owner |
| POST | `/api/bookings/{id}/cancel` | Hủy đơn, trả về số tiền hoàn | Khách (mã + SĐT) / Staff |
| POST | `/api/rooms/{id}/blocks` | Khóa ngày | Owner |
| DELETE | `/api/rooms/{id}/blocks/{date}` | Mở khóa ngày | Owner |
| GET | `/api/notifications/unread-count` | Số thông báo chưa đọc (polling) | Đã đăng nhập |

API viết bằng ASP.NET Core (`[ApiController]`, `[Route("api/[controller]")]`, `[HttpGet]`/`[HttpPost]`/`[HttpDelete]`), trả về JSON qua `ActionResult<T>`: `Ok()`, `CreatedAtAction()`, `NotFound()`, `Conflict()`; lỗi validation được `[ApiController]` tự trả 400 (`ValidationProblemDetails`). Mã trạng thái: 200, 201, 400, 401/403, 404, 409 (trùng phòng). Toàn bộ API hiển thị trên Swagger UI (`/swagger`).

---

## 9. Sitemap

```
Trang chủ
├── Tìm kiếm homestay (lọc + phân trang)
│   └── Chi tiết homestay
│       └── Chi tiết phòng (lịch trống, báo giá)
│           └── Đặt phòng: B1 Chọn ngày → B2 Thông tin khách → B3 Xác nhận → Thành công (mã đơn)
├── Tra cứu đơn (mã + SĐT) → Chi tiết đơn → Hủy đơn / Đánh giá
├── Đăng nhập (Owner, Staff, Admin)
└── Đổi ngôn ngữ VI / EN

Khu vực Chủ homestay (/Owner)
├── Tổng quan (đơn chờ xác nhận, lịch nhận/trả hôm nay)
├── Homestay của tôi → Phòng → Ảnh, Giá
├── Lịch phòng (khóa / mở ngày)
├── Đơn đặt phòng (xác nhận / từ chối / nhận phòng / trả phòng)
└── Báo cáo đối soát

Khu vực Nhân viên (/Staff)
├── Tra cứu phòng trống toàn hệ thống
├── Tạo đơn thay khách
├── Quản lý đơn (lọc, phân trang, lịch sử, giao lại)
├── Cảnh báo đơn quá hạn xác nhận
└── Thống kê đơn của tôi

Khu vực Quản trị (/Admin)
├── Dashboard (biểu đồ đơn, doanh thu, hoa hồng)
├── Tài khoản & phân quyền
├── Danh mục: Khu vực, Loại phòng, Tiện nghi, Ngày lễ
├── Homestay đối tác (tỷ lệ hoa hồng, kích hoạt)
├── Xác nhận cọc / Hoàn cọc
├── Đối soát theo kỳ
└── Báo cáo
```

---

## 10. Kế hoạch triển khai theo giai đoạn

> Mỗi giai đoạn tương ứng 1 sprint (≈ 1 tuần). Điều chỉnh theo lịch học kỳ, đảm bảo xong trước **2 buổi thuyết trình cuối kỳ**.

| GĐ | Tên | Công việc | Đầu ra | Mục báo cáo |
|---|---|---|---|---|
| **0** | Khởi động | Chốt đề tài; cài .NET 10 SDK, Visual Studio 2022 / VS Code + C# Dev Kit / Rider, SQL Server (LocalDB trên Windows, Docker trên macOS), `dotnet-ef`; tạo repo GitHub, bảng Kanban; thống nhất quy ước ([mục 12](#12-quy-ước-làm-việc-nhóm)) | Repo + backlog | — |
| **1** | Phân tích | Tóm tắt file PTTKYC: mô tả hệ thống, nghiệp vụ, biểu mẫu khảo sát; xác định chức năng 4 lớp người dùng; vẽ **use-case** tổng quát + phân rã | Use-case (draw.io) | 5.1, 5.2 |
| **2** | Thiết kế | **ERD**, mô tả bảng và ràng buộc; **sitemap**; **wireframe** các màn hình chính (tìm kiếm, chi tiết, đặt phòng, lịch phòng, quản lý đơn, dashboard) | ERD, sitemap, wireframe | 5.2 |
| **3** | Nền tảng | Tạo solution bằng `dotnet new mvc` + `dotnet new xunit`; cấu hình `Program.cs`; entity, `ApplicationDbContext`, `dotnet ef migrations add Init` / `dotnet ef database update`, seed trong `SeedData` (35 homestay mẫu, tài khoản mỗi role); Identity + Customer/Owner/Employee và chức vụ; `_Layout` Bootstrap responsive; cấu hình i18n + cookie ngôn ngữ; partial phân trang dùng chung; CRUD danh mục (Admin) | Chạy được, đăng nhập theo role | 5.3 |
| **4** | Chủ homestay | CRUD homestay, phòng; upload nhiều ảnh; editor mô tả/chính sách; giá 3 mức; trang lịch phòng khóa/mở ngày bằng AJAX | Module Owner | 5.3 |
| **5** | Khách: tìm kiếm | Trang tìm kiếm (filter + paging + sort, lưu bộ lọc vào cookie); chi tiết homestay/phòng; API availability + quote; lịch trống và báo giá AJAX | Module tìm kiếm | 5.3 |
| **6** | Đặt phòng | Đặt phòng 3 bước (session); `BookingService` + `RoomNights`; sinh mã đơn; tra cứu đơn; Owner xác nhận/từ chối; lịch sử đơn; thông báo trong hệ thống | Luồng đặt phòng end-to-end | 5.3 |
| **7** | Nhân viên + Admin | Staff: tạo đơn thay khách, quản lý đơn, giao lại, cảnh báo 30 phút (job); Accounting: bill/hoàn tiền/đối soát; Manager: duyệt nghiệp vụ; job hết hạn, đánh giá, dashboard | Đủ chức năng | 5.3 |
| **8** | Kiểm thử | Unit test xUnit (`dotnet test`); chạy bảng test case; test responsive (điện thoại/máy tính), test bảo mật cơ bản, test i18n; sửa lỗi | Bảng test case + kết quả | 5.4 |
| **9** | Đóng gói | Xuất script `.sql`: schema bằng `dotnet ef migrations script --idempotent -o database/HomestayBooking.sql`, dữ liệu mẫu bằng *Generate Scripts → Schema and data* của SSMS/Azure Data Studio; viết `README` hướng dẫn cài đặt và cấu hình; hoàn thiện báo cáo (kết luận, phân công, TLTK, bài học, hướng phát triển); làm slide, tập thuyết trình | File `.zip` nộp | 5.5, 5.6 |

**Mốc kiểm tra:**

- **Hết GĐ 2:** có đủ hình cho chương 5.2. Không bắt đầu code khi ERD chưa chốt.
- **Hết GĐ 6:** demo được luồng đặt phòng hoàn chỉnh (mốc quan trọng nhất).
- **Hết GĐ 8:** đóng băng tính năng (feature freeze), chỉ sửa lỗi và viết báo cáo.

---

## 11. Phân công công việc

| Thành viên | Vai trò | Phụ trách chức năng | Phụ trách báo cáo |
|---|---|---|---|
| **Phạm Ngọc Phú** | Trưởng nhóm, tích hợp | Nền tảng (solution, Identity, phân quyền, DbContext), đặt phòng + `BookingService` + `RoomNights`, REST API, bảo mật | 5.1, tổng hợp và biên tập báo cáo, slide |
| **Lã Quốc Thái** | Thành viên | Khu vực Chủ homestay: CRUD homestay/phòng, upload ảnh, editor, giá 3 mức, lịch khóa ngày | Use-case, wireframe; 5.3 phần Owner |
| **Nguyễn Quang Nghĩa** | Thành viên | Trang khách: layout, tìm kiếm (filter/paging/sort), chi tiết, AJAX báo giá, tra cứu đơn, i18n, cookie | Sitemap; 5.3 phần Khách |
| **Trần Kỳ Vĩ** | Thành viên, phụ trách kiểm thử | Khu vực Nhân viên + Admin: quản lý đơn, cảnh báo, cọc/hoàn cọc, đánh giá, đối soát, dashboard; background job | ERD + mô tả CSDL; 5.4 kiểm thử; script `.sql` |

Tất cả thành viên: viết unit test cho phần mình làm, review PR của người khác, chụp màn hình chức năng của mình cho báo cáo.

---

## 12. Quy ước làm việc nhóm

**Git:**

```
main      ← chỉ merge bản ổn định (cuối mỗi giai đoạn)
develop   ← nhánh tích hợp
feature/<tên-chức-năng>   ví dụ: feature/owner-room-crud, feature/search-paging
fix/<mô-tả-lỗi>
```

- Không commit thẳng vào `main` hoặc `develop`; mọi thay đổi đi qua Pull Request và cần ≥ 1 người review.
- Commit message: `feat: thêm lọc theo tiện nghi`, `fix: sai giá đêm lễ`, `docs: cập nhật ERD`, `test: thêm test hoàn cọc`.
- Migration EF Core: **chỉ một người chạy `dotnet ef migrations add` tại một thời điểm** (báo trước trên nhóm chat) vì file `ApplicationDbContextModelSnapshot.cs` rất dễ xung đột khi merge.
- Dùng `.gitignore` tạo bởi `dotnet new gitignore`; không đưa `bin/`, `obj/`, `.vs/`, `wwwroot/uploads/` lên repo.
- Chuỗi kết nối riêng của từng máy (LocalDB hoặc Docker) để trong `dotnet user-secrets`; `appsettings.json` trên repo chỉ chứa chuỗi mẫu.

**Code:**

- Đặt tên tiếng Anh cho class, biến, bảng; giao diện hiển thị qua file `.resx`.
- ViewModel riêng cho từng form, không bind trực tiếp Entity vào View.
- Mọi danh sách trên giao diện đều có phân trang.

**Họp:** Planning đầu tuần và demo cuối tuần (online được); cập nhật Kanban hằng ngày.

---

## 13. Kế hoạch kiểm thử

### 13.1. Unit test (xUnit)

| Nhóm | Ca kiểm thử tiêu biểu |
|---|---|
| `PricingService` | 2 đêm ngày thường; đêm thứ Sáu tính giá cuối tuần; đêm trùng ngày lễ tính giá lễ; cọc = 30% làm tròn |
| `RefundService` | Hủy trước 10 ngày → 100%; trước 5 ngày → 50%; trước 1 ngày → 0%; biên đúng 7 ngày và đúng 3 ngày |
| `BookingService` | Completed → Cancelled phải lỗi; hai người cùng đặt phòng cuối thì chỉ một đơn giữ được đêm; chủ xác nhận đơn đã cọc, không tự từ chối các đơn khác |
| `BookingCodeGenerator` | Đúng định dạng; số thứ tự tăng dần trong ngày, quay về 001 khi sang ngày mới |
| `SettlementCalculator` / backend đối soát | Đơn kết thúc có tiền giữ lại/phí đổi vào kỳ theo CheckOut; hoa hồng snapshot, phí đổi chia theo tỷ lệ trên phí; chủ xác nhận tùy chọn |

### 13.2. Test case thủ công (mẫu cho mục 5.4)

| Mã | Mục tiêu | Dữ liệu vào / ràng buộc | Đầu ra dự kiến | Đầu ra thực tế | Kết quả | Ghi chú sửa lỗi |
|---|---|---|---|---|---|---|
| TC01 | Đặt phòng hợp lệ | Phòng trống 20–22/11, 2 khách, SĐT hợp lệ | Tạo AwaitingPayment, giữ hai đêm và hiện mã `FJ…`/QR cọc | | | |
| TC02 | Chặn ngày trả trước ngày nhận | CheckIn 22/11, CheckOut 20/11 | Báo lỗi validation, không gửi form | | | |
| TC03 | Chặn SĐT sai định dạng | SĐT `12345` | Thông báo "Số điện thoại không hợp lệ" | | | |
| TC04 | Chống trùng phòng | Hai khách cùng đặt phòng cuối, cùng khoảng đêm | Một đơn giữ phòng thành công; người còn lại được báo hết phòng | | | |
| TC05 | Phân quyền | Owner A truy cập URL đơn của Owner B | 403 / không tìm thấy | | | |
| TC06 | Tra cứu đơn | Đúng mã, sai SĐT | Không hiển thị thông tin đơn | | | |
| TC07 | Upload ảnh | File `.exe` đổi đuôi `.jpg`, file 10 MB | Từ chối, báo lỗi | | | |
| TC08 | XSS qua editor | Mô tả chứa `<script>alert(1)</script>` | Script bị loại bỏ khi lưu | | | |
| TC09 | Phân trang + lọc | Lọc Tây Hồ, giá ≤ 1 triệu, trang 2 | Đúng kết quả, giữ bộ lọc khi chuyển trang | | | |
| TC10 | Đổi ngôn ngữ | Chọn English, tải lại trang | Giao diện tiếng Anh, cookie được lưu | | | |
| TC11 | Responsive | Màn hình 375px | Không cuộn ngang, menu thu gọn | | | |
| TC12 | Hủy và hoàn cọc | Đơn Deposited, hủy trước 5 ngày, chính sách chung | Chốt hoàn 50% tiền cọc thực chuyển, nhả phòng; kế toán ghi chuyển hoàn riêng | | | |

Danh sách đầy đủ do Vĩ tổng hợp, khoảng 30–40 test case, mỗi module ít nhất 5.

---

## 14. Bố cục báo cáo và sản phẩm nộp

### 14.1. Bố cục báo cáo (15–35 trang)

| Mục | Nội dung | Nguồn |
|---|---|---|
| Bìa | Tên trường, môn học, tên nhóm, lớp, giảng viên, năm | |
| 5.1 Tổng quan đề tài | Mô tả hệ thống, nghiệp vụ; khảo sát, biểu mẫu; phân tích chức năng 4 lớp người dùng | Rút gọn từ file PTTKYC (≈ 5–6 trang) |
| 5.2 Phân tích và thiết kế | Use-case; ERD + mô tả bảng, ràng buộc; sitemap; wireframe | GĐ 1–2 |
| 5.3 Thiết kế hệ thống | Ảnh chụp từng chức năng + giải thích, nêu rõ chỗ dùng paging/AJAX/session/cookie/API/upload/editor/i18n | GĐ 3–7 |
| 5.4 Kiểm thử | Mục tiêu, bảng test case, lỗi phát hiện và cách sửa | GĐ 8 |
| 5.5 | Kết luận, bảng phân công, tài liệu tham khảo | GĐ 9 |
| 5.6 | Kết quả so với mục tiêu, bài học rút ra, hướng phát triển (Zalo/SMS, cổng thanh toán, đồng bộ OTA, app di động) | GĐ 9 |

**Định dạng:** Times New Roman 13, giãn dòng 1.1, lề trên 2 cm, dưới 2 cm, trái 1.5 cm, phải 1.5 cm, gáy 2 cm; **không có header/footer**.

### 14.2. Checklist nộp bài

- [ ] Báo cáo `.docx`/`.pdf` đúng bố cục và định dạng
- [ ] Toàn bộ mã nguồn (đã xóa `bin/`, `obj/`, `.vs/`; chạy được bằng `dotnet restore` + `dotnet run`)
- [ ] `database/HomestayBooking.sql`: script tạo CSDL + dữ liệu mẫu, đã chạy thử trên máy sạch
- [ ] `README.md`: yêu cầu môi trường (.NET 10 SDK, SQL Server), cách chạy script SQL hoặc `dotnet ef database update`, chỗ sửa chuỗi kết nối, tài khoản mẫu cho từng role
- [ ] File nén duy nhất đặt tên theo quy tắc: `QLHomestay_Nhom<số>_<Lớp>.zip`
- [ ] Slide thuyết trình + kịch bản demo (luồng đặt phòng end-to-end + các mục kỹ thuật)

---

## 15. Rủi ro và cách xử lý

| Rủi ro | Mức độ | Cách xử lý |
|---|---|---|
| Phạm vi quá lớn so với thời gian | Cao | Ưu tiên luồng chính (GĐ 3–6); chức năng ở GĐ 7 làm theo thứ tự: cọc → quản lý đơn → cảnh báo → đánh giá → đối soát → dashboard |
| Xung đột migration / merge | Trung bình | Một người tạo migration tại một thời điểm; merge `develop` thường xuyên |
| Thành viên chậm tiến độ | Trung bình | Kanban cập nhật hằng ngày; trưởng nhóm điều chỉnh phân công ở buổi planning |
| Môi trường khác nhau giữa Windows và macOS | Trung bình | .NET 8 chạy được trên cả hai; máy macOS dùng SQL Server qua Docker; thống nhất phiên bản SDK bằng `global.json`; cài đặt ngay ở GĐ 0 |
| Giảng viên yêu cầu đúng ASP.NET MVC 5 | Thấp | Xác nhận với giảng viên ở GĐ 0; ASP.NET Core MVC vẫn là mô hình MVC của ASP.NET nên mọi yêu cầu ở mục 3.3 đều đáp ứng được |
| Mất dữ liệu demo trước buổi thuyết trình | Thấp | Seed data + script `.sql`; tập demo trên máy sẽ dùng để trình bày |
| Báo cáo dồn về cuối | Cao | Mỗi giai đoạn hoàn thành phần báo cáo tương ứng (cột "Mục báo cáo" ở [mục 10](#10-kế-hoạch-triển-khai-theo-giai-đoạn)) |

### 7.9. Backend báo cáo, quản trị tài khoản và dữ liệu mẫu (B6)

- ReportService trả DTO đã tính sẵn, ngày nhập [từ, đến] bao gồm hai đầu theo giờ VN, tối đa 366 ngày, lọc homestay/khu vực. Không nạp toàn bộ đơn: thống kê tạo đơn, kênh/trạng thái, hiệu suất và homestay dùng GroupBy trong CSDL; chỉ nạp các đơn có CheckOut trong khoảng để dùng SettlementCalculator.ItemFor/FujiNetRevenue. Kết quả tiền là **tạm tính**, có thể khác snapshot bảng đã gửi.
- Cơ sở thời gian: số đơn/tỷ lệ chốt/công việc Assigned dùng nhóm CreatedAt trong khoảng. Doanh số Completed của công ty và nhân viên theo CheckOut (D7), người tạo là CreatedByEmployeeId, người xử lý là AssignedEmployeeId (D23). Tỷ lệ chốt = ConfirmedAt khác NULL / tổng đơn mình tạo (chốt 17/10/2026), không đo theo lượt hỏi. Đã xử lý xong = Completed/Cancelled/Rejected/Expired/NoShow; còn lại là đang phụ trách. Không đổi người tạo khi giao lại.
- Tỷ lệ khách hủy: mẫu số đơn tạo trong khoảng có DepositedAt, bỏ ReplacedByBookingId khác NULL; tử số Cancelled. Tỷ lệ chủ từ chối báo riêng = Rejected / tổng đơn tạo trong khoảng; Expired báo số lượng riêng. Mẫu số 0 trả 0. Khoản bù đã chi theo ReviewedAt của Compensation Approved, tách khỏi tiền đối soát.
- Homestay: ReviewRatings.Summaries lọc Review.CreatedAt trong khoảng và bỏ IsHidden; thời gian xác nhận theo nhóm DepositedAt trong khoảng, chỉ lấy đơn có ConfirmedAt, tính trung bình và tỷ lệ <=30 phút. Trùng phòng theo AuditLog Step OverbookingReported/CreatedAt; số đơn thay theo CancelledAt và ReplacedByBookingId, nhóm tháng giờ VN. Không nhận diện sự kiện bằng chữ trong ghi chú.
- X6: Manager/Administrator xem toàn bộ; Consultant chỉ EmployeesAsync của mình (chỉ định người khác bị chặn); Accountant chỉ FinancialAsync. Chủ/khách bị chặn, service kiểm lại Employee + Account Active, không chỉ role. Chưa thêm giao diện báo cáo.
- AccountAdminService: Administrator tạo Employee + Person + Account, mật khẩu qua UserManager, role Employee, AccountActivityLogger AccountCreated/RoleAssigned. Chưa bắt đổi mật khẩu lần đầu (shortcut, chờ luồng kích hoạt nhân viên). Đổi chức vụ/trạng thái cập nhật stamp; ngừng việc -> Disabled, chuyển lại Active -> Active. Chỉ đổi chức vụ không tự mở khóa tài khoản Locked.
- Khóa/mở bất kỳ tài khoản Active <-> Locked bắt buộc lý do, cập nhật stamp + StatusChanged; không mở nhân viên ngừng việc bằng đường tắt. Reset mật khẩu chỉ cho nhân viên, Identity kiểm chính sách + PasswordReset; không log mật khẩu, hash, stamp hay token. Không tự khóa/hạ chức vụ/cho nghỉ; luôn còn Administrator Active có Account Active. Manager chỉ đọc danh sách, không sửa.
- Audit ghi trực tiếp, Id thật, OldValues/NewValues, cùng transaction mặc định với Identity/role/nhật ký. Dùng khóa các dòng Administrator hiện có để kiểm lại trước khi sửa, không Serializable, không schema mới. CatalogDeletionService dùng chung action xóa cũ Areas/RoomCategories/Amenities/Holidays: chặn danh mục đang dùng, kể cả tiện nghi có quan hệ cascade; dịch FK khi tranh chấp sang tiếng Việt. Ngày lễ không có FK, đơn cũ giữ snapshot BookingNights; không sửa view.
- Seed CSDL mới giữ guard chỉ thêm vào bảng rỗng: hợp đồng tới 2028-05-31; 9 ngày lễ 2028 theo lịch đã chốt, Giỗ Tổ để Admin bổ sung; mỗi homestay có PropertyPhoto sample-document.png. Dữ liệu cũ dùng seed_topup_2028.sql idempotent, chỉ hợp đồng HD-2026-* Active còn ngắn hạn, ngày lễ thiếu, ảnh mẫu thiếu; không di chuyển giấy tờ cũ. Script có guard tên DB, XACT_ABORT, transaction và audit System.
- Thử top-up CHỈ DeployTest: lần 1 gia hạn 35 hợp đồng, thêm 9 ngày lễ/35 ảnh, thêm 79 AuditLogs; chỉ 4 bảng này đổi. Lần 2 0/0/0, COUNT_BIG + CHECKSUM_AGG(BINARY_CHECKSUM(*)) của đủ 45 bảng giống hệt trước/sau. **Chưa chạy top-up trên v5**; để cùng đợt duyệt triển khai migration 9, có backup trước.
- System.Security.Cryptography.Pkcs được tham chiếu trực tiếp 10.0.13 ở Web; Tests nhận qua project reference. Không nâng EF Core/SqlClient. `dotnet list package --vulnerable --include-transitive` sạch theo nguồn NuGet hiện tại; xem [advisory chính thức](https://github.com/dotnet/runtime/security/advisories/GHSA-846f-f764-76mx).
- data_consistency.sql giữ 33 mục cũ, thêm nhân viên Suspended/Resigned nhưng Account Active và kiểm Administrator khi EXISTS Employees: tổng 35, chỉ SELECT, không áp kiểm thiếu Administrator cho database rỗng như ConstraintTest. Không so EndDate với ngày lễ cuối; giữ mục 13 hợp đồng Active quá hạn và D20. Không tự sửa dữ liệu nếu có LỆCH.

Những phần chưa đủ đã ghi ngay trong bảng 3.3: FR-03 đặt không đăng nhập; FR-07 tạo khách mới theo SĐT cho đơn đặt hộ; FR-08 đo tải; FR-04/23 kênh thật; FR-21 bootstrap Administrator Production; FR-26 truy vấn hồ sơ thẩm định. Đây không phải tuyên bố backend mọi FR đã hoàn tất.

### 7.10. Backend bổ sung cho giao diện (B8)

Lấp các gap Bước 0 kiểm kê giao diện ghi ở `docs/ui/backend-gaps.md` (G-01…G-14). Không thêm schema/migration, không sửa view. 1500 → 1520 test (1 test hiệu năng vẫn skip như cũ).

- Quyết định 17/10: FR-27 chỉ Quản lý/Quản trị viên duyệt nội dung homestay (giữ kiểm quyền hiện tại, màn duyệt ở khu Admin); màn hợp đồng FR-19 ở khu Admin, URL thông báo hết hạn `/Admin/Contracts/{id}`; Tư vấn xem phòng trống FR-08 bằng bảng `StaffAvailabilityAsync`, lịch tháng `RoomBlockService.CalendarAsync` giữ quyền Chủ + Quản lý/Quản trị.
- G-08 (lỗi): `OverbookingService` gửi khách link `/Bookings/Details/{mã đơn}` (trước dùng Id nên 404); đổi phòng gửi chủ `/Owner/Bookings/Details/{id}`.
- Thông báo (FR-11/15): `NotificationService.ListAsync/LatestAsync/UnreadCountAsync/MarkReadAsync/MarkAllReadAsync`, luôn lọc theo userId; đánh dấu thông báo của người khác trả false.
- FR-13/27: `HomestayContentService.OwnerHomestaysAsync(actor)` và `EditorAsync(id, actor)` trả DTO form sửa (giá hiện hành mỗi DayType + giá tương lai theo EffectiveFrom, ảnh, tiện nghi, chính sách đang áp dụng, giấy tờ không kèm FileUrl); `HomestayReviewService.PendingDetailsAsync` dùng lại DTO đó cho trang duyệt, kèm FirstReview (chưa từng duyệt) và chủ.
- FR-19: `ContractService.ListAsync(ContractQuery)` (lọc trạng thái, tìm số HĐ/homestay/chủ, sắp hết hạn trong N ngày) và `DetailsAsync` (lịch sử tạo/gia hạn/chấm dứt đọc từ AuditLog); chỉ Quản lý/Quản trị.
- FR-09: `StaffBookingService.AssigneesAsync` (Tư vấn/Quản lý đang làm việc, đúng điều kiện `AssignAsync`). Sidebar: `BookingChangeRequestService.PendingCountAsync`.
- FR-16: `OwnerScheduleService.ReadAsync(actor, …)` tự suy chủ từ tài khoản.
- FR-01/02: `HomestaySearchViewModel.Categories/Amenities` (chỉ loại phòng/tiện nghi đang có ở homestay Active); `HomestayCard.Rating/ReviewCount` lấy từ `ReviewRatings` đã nạp một lần; `AvailabilityService.BlockedDatesAsync` trả đêm hết phòng (≤ 92 đêm) cho bộ chọn ngày.
- FR-06: `BookingDetailsViewModel.HasReview`. FR-21: `EmployeeAccountInput.Department`/`AccountSummary.Department` (≤ 100 ký tự, ghi audit).


### 7.11. Backend bổ sung cho giao diện (B9)

- FR-13: `HomestayContentService.EditorAsync` trả toàn bộ tiện nghi/danh mục loại phòng đang tồn tại, không lọc cơ sở Active; cờ Selected theo liên kết thật. Danh mục không có cờ Active nên dùng cùng điều kiện tồn tại của UpdateAsync/CreateTypeAsync/UpdateTypeAsync.
- FR-15/16: `OwnerScheduleService.OwnerBookingSummaryAsync` kiểm tài khoản Owner Active/không Employee, đếm Deposited, quá hạn ConfirmDeadlineAt và Confirmed nhận hôm nay (VN), chỉ cơ sở của chủ.
- FR-04/11/15/26: mọi nguồn NotificationService.Add thiếu URL được nối theo người nhận (Owner Id số, khách Code, Staff màn đơn, Accounting màn bill); DecideAsync thông báo chủ `/Partner/Status`. Không backfill thông báo cũ, không đoán từ nội dung.
- FR-08/09: StaffBookingService nạp tên cơ sở/phòng/người phụ trách và actor lịch sử, `HomestaysAsync` trả tên mọi trạng thái; `FreeRoomsAsync` trả mã phòng. `StaffNightAvailabilityAsync` trả ma trận từng đêm ≤31, bốn query dữ liệu + query quyền cố định; giữ điều kiện bán/hợp đồng, giá mỗi đêm và RoomNights, không cache.
- FR-18: Accountant được `ConfirmOnBehalfAsync` từ Sent với lý do 1–500, audit “Kế toán xác nhận thay chủ” và báo chủ. ConfirmAsync của chủ không đổi. RecordPaidAsync nhận số tiền đúng PayoutAmount và thời điểm UTC thực tế ≤Now, ≥SentAt và ≥ConfirmedAt nếu có. Không chi từng phần, không đổi schema; số tiền/ngày trong audit. Chỉ Accountant ghi, Manager/Administrator vẫn chỉ đọc/lập.
- `CompensationService.RejectableAsync` cho Accountant đọc Compensation Submitted có đơn chưa Completed, đúng điều kiện RejectAsync hiện hành.
- FR-17/18: Settlement đọc có Homestay/Owner/Person; `AccountingService.ReceiptsAsync/RefundsAsync/TransactionsAsync/TransactionAsync/AdditionalDepositsAsync` nạp SettlementId từ SettlementItem thật, kiểm Actor cho Accountant/Manager/Administrator, không suy theo ngày.
- FR-27: PendingDetailsAsync trả lý do và so sánh trường audit hiện có, giấy tờ legacy còn hạn của hồ sơ Approved cùng chủ; không tạo snapshot lịch sử giả. FR-21: AccountQuery.Position lọc tại DB và DetailsAsync kiểm Management. FR-06: ListAsync nạp tên hiển thị người viết.
- Kiểm chứng: B9OwnerTests, B9StaffTests, B9FinanceTests, B9AdminTests, PartnerReviewTests và AvailabilityPerformanceTests. Kết quả đo/kiểm UI xem `docs/ui/b9-backend-report.md`.
