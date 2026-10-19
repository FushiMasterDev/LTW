# Pre-launch QA — Fuji Homestay

Ngày kiểm: **20/10/2026, giờ Việt Nam**. Thực hiện theo toàn bộ `docs/prompts/prelaunch-qa.md`, trong phiên không tương tác.

**Kết luận: NO-GO cho ra mắt công khai.** Các luồng đặt phòng, tiền cọc, đổi/hủy, vận hành và đối tác đã chạy được trên bản publish. Tuy nhiên, kiểm upload mở rộng vẫn thất bại so với mục 5.4: ảnh hỏng có header PNG, tên `.exe`, tên chứa `../` và MIME không khớp đều được nhận. Cần xử lý và chạy lại TC07 trước khi chốt Go. Những giới hạn kiểm chứng khác được ghi riêng bên dưới.

## 1. Môi trường, phạm vi và an toàn dữ liệu

| Thành phần | Đã dùng |
|---|---|
| Nhánh | `feature/foundation`, không đổi nhánh |
| Mã gốc | `49c6f50b5fec153501d7b9ba9de212d52d774b69` |
| Mã ứng dụng sau sửa | `76ebd00`, cùng các commit liệt kê ở mục 8 |
| Máy / SDK | Darwin x86_64; .NET SDK **10.0.400** |
| SQL Server | Docker `sql-homestay`, SQL Server **16.0.4230.2**, image `mcr.microsoft.com/mssql/server:2022-latest` |
| Bản chạy | `dotnet publish HomestayBooking.Web -c Release -o /private/tmp/fuji-prelaunch/publish`; chạy DLL trong thư mục publish |
| Development | HTTP **5014**, `HomestayBooking_PreLaunch` |
| Production | HTTPS **5015**, `HomestayBooking_PreLaunchProd`; HTTP **5017** chỉ dùng kiểm chuyển hướng |
| Job hạn ngắn | Development **5016**, cùng DB QA Development; `Booking__HoldMinutes=1`, `Booking__ConfirmMinutes=2` |
| Trình duyệt | Chrome/Chromium **155.0.8059.82**, WebKit **26.6**, Firefox **156.0**; `playwright-cli` **0.1.22** |

- Hai tên DB QA đều chưa tồn tại khi bắt đầu. Tạo mới bằng `CREATE DATABASE`, chạy `database/deploy.sql` và `verify.sql` trong container. Mỗi DB có **128 dòng OK**, 45 bảng và 9 migration. Không đổi schema/migration.
- Mật khẩu SQL lấy trực tiếp từ môi trường container vào biến shell; không ghi giá trị vào báo cáo, source hoặc commit. Mỗi app dùng `ConnectionStrings__Default` trỏ rõ DB QA.
- Không chạy app QA vào `HomestayBooking_v5` hoặc database nghiệp vụ có sẵn. Các test SQL chỉ dùng `HomestayBooking_ConstraintTest`, đã kiểm **0 dòng nghiệp vụ** trước khi chạy; dữ liệu thử nằm trong transaction rồi rollback.
- Không push, không merge. Không dừng app 5011. Kiểm cuối vẫn thấy app này lắng nghe bằng PID **31028**.
- Khoảng 00:36, Docker Desktop mất socket; container sau đó được quan sát ở trạng thái thoát **137**, `OOMKilled=false`. Đã mở lại Docker Desktop và **start container hiện có**, không dùng stop/rm/restart container. Các bước UI bị gián đoạn được chạy lại. Đây là sự cố môi trường, chưa xác định nguyên nhân thoát.
- Ảnh, script, log, TRX và browser state ở **`/private/tmp/fuji-prelaunch/`** hoặc `.playwright-cli/`; không commit. Tên file bằng chứng trong các bảng bên dưới được hiểu tương đối với thư mục này.

Đã đọc README, các phần FR/5.4/7/8/13 của `QUY_TRINH_BTL.md`, `database/README.md` và ba tài liệu UI bắt buộc. Áp dụng `playwright-cli`, `verification-before-completion`, `web-design-guidelines`, `full-output-enforcement`. Bản hướng dẫn UI được lấy từ [nguồn chính thức](https://raw.githubusercontent.com/vercel-labs/web-interface-guidelines/main/command.md).

## 2. Tổng hợp mục 1–6 của prompt

| Mục | Kết quả | Đầu ra và bằng chứng |
|---|---|---|
| 1. Dựng máy chủ mới | Đạt các lượt đã xác minh | Hai lần deploy/verify đều 128 OK; chạy từ publish; Production không có demo. Build/test cuối được ghi ở mục 9. `*-deploy.log`, `*-verify.log`, `prod-auth.json`, `production-checks.json` |
| 2. Luồng nghiệp vụ | Đạt phần lớn; một nhánh chưa kiểm được qua UI | Đã chạy khách, chủ, voucher, đổi/hủy, kế toán, nhân viên, thẩm định, hợp đồng, duyệt nội dung, tài khoản, danh mục và lịch. Nhánh mời/kích hoạt đối tác cần phân biệt hai quy trình, xem mục 10 |
| 3. Đồng thời và biên | Đạt các ca đã chạy sau sửa | TC04: 1/8 giữ được phòng cuối; gửi lại cùng form trả cùng đơn; hết hạn thật nhả đêm; cảnh báo thật; validation ngày/khách/SĐT/độ dài. `concurrency.json`, `submission-http-fixed.json`, `boundaries.json` |
| 4. Bảo mật | **Lỗi** | Phân quyền, chống CSRF, XSS, khóa đăng nhập và webhook đạt các ca đã chạy. Upload chưa đạt kiểm mở rộng; một số header chưa có. `security-matrix.json`, `security-extra.json`, `upload-deep.json` |
| 5. Thiết bị / trình duyệt | Đạt trong mô phỏng, có giới hạn | Ba engine chạy được; 90 cấu hình viewport/sáng-tối không phát hiện tràn ngang; luồng đặt/hủy thực sự chạy trên WebKit và Firefox. Chưa kiểm bàn phím ảo và thiết bị vật lý |
| 6. Hiệu năng | Đạt benchmark cô lập sau tối ưu | Mốc SQL 150 cơ sở / 1.500 phòng / 326.700 đêm; không thay ngưỡng 5 giây. Lượt chạy chung ban đầu đã thất bại và được giữ trong log, xem mục 7 |

## 3. Dựng mới và Production

Trước bootstrap, DB Production có **0 tài khoản, 0 nhân viên, 0 đơn**, 7 khu vực và 3 role. Đã làm đúng B7: tạo quản trị đầu tiên **QTQA001** bằng các biến `Bootstrap__Administrator__*`, mật khẩu ngẫu nhiên riêng. Sau đó bỏ cấu hình bootstrap khỏi lần chạy tiếp theo. DB Production cuối có **1 tài khoản / 1 nhân viên / 0 homestay / 0 đơn**; đây là quản trị QA, không phải tài khoản demo seed.

Mật khẩu quản trị QA được giữ trong file riêng quyền 600 `/private/tmp/fuji-prelaunch/prod-admin-password`, không đưa giá trị vào log/báo cáo/commit.

| Kiểm Production | Thực tế |
|---|---|
| `/swagger`, `/Dev/UiKit`, `/Dev/SimulateBank` | **404**, kể cả sau khi đăng nhập quản trị QA |
| HTTP → HTTPS | **307** từ 5017 sang `https://localhost:5015/` |
| HSTS | `max-age=2592000` khi gửi Host ngoài localhost; localhost bị loại trừ theo mặc định framework |
| Cookie đăng nhập | Quan sát thực: `Secure`, `HttpOnly`, `SameSite=Lax` |
| Cookie antiforgery | Quan sát thực: `Secure`, `HttpOnly`, `SameSite=Strict` |
| Cookie session | Cấu hình xác nhận `Secure=Always`, `HttpOnly`, `SameSite=Lax`; chưa quan sát phát sinh cookie session trên DB Production chỉ có danh mục và quản trị |
| Trang lỗi | 404 thân thiện và view Error đọc được; lỗi API validation không có stack/SQL. Hồi quy pipeline POST lỗi thật được bổ sung ở `ProductionErrorHandlingTests` |
| Header đang có | `X-Frame-Options: SAMEORIGIN` trên trang có antiforgery; HSTS trên HTTPS với Host phù hợp |
| Header chưa có trên phản hồi đã đo | `Content-Security-Policy`, `X-Content-Type-Options`, `Referrer-Policy` chung, `Permissions-Policy` |

Lần chạy Production ban đầu chỉ có HTTP và chưa đặt cổng HTTPS trả 500 do antiforgery yêu cầu SSL. Sau khi cấu hình HTTPS/HTTPS port, trang trả 200. Điều này là yêu cầu cấu hình triển khai: không dùng bản Production qua HTTP trực tiếp. Không hạ mức bảo vệ cookie để làm HTTP chạy được.

## 4. Các luồng nghiệp vụ đã dùng qua giao diện

Các thao tác ở bảng này đi qua bấm nút, điền form, upload và modal bằng Playwright; không gọi service trực tiếp. Các ghi nhận chuyển tiền đều là sổ thử/Simulator, không chuyển tiền ngân hàng thật.

| Vai trò / luồng | Đầu ra thực tế | Bằng chứng |
|---|---|---|
| Khách chưa đăng nhập 375px | Tìm Hoàn Kiếm + ngày + giá → Phố Cổ Corner House → `FJ261020-001`; 400.000đ, cọc 120.000đ, QR tải được | `01-search.png`, `02-detail.png`, `03-qr-loaded.png` |
| QR tự khớp | Simulator đúng mã/số tiền trả `Matched`, đơn sang `Deposited` | `04-bank-match.png` |
| Tra cứu | Luồng English tạo `FJ261020-025`, tra mã + SĐT đúng trở về đúng URL chi tiết; sai SĐT/API không lộ đơn | `english-flow.json`, `security-matrix.json` |
| Chủ nhận thông báo, xác nhận | `FJ261020-001` được xác nhận; trước đó không có địa chỉ, sau đó thấy 12 Hàng Bạc và hướng dẫn nhận phòng | `06-confirmed.png`, `guest-confirmed.yml`, `scan-fuji-owner.json` |
| Nhận/trả phòng | `Confirmed → CheckedIn → Completed` qua nút của chủ | `07-checkin.png`, `08-completed.png` |
| Đánh giá và trả lời | Khách gửi 5/5; chủ trả lời “Cảm ơn khách QA 🌸”; cả hai hiện công khai, chuỗi độc hại hiển thị như văn bản | `09-review.png`, `10-owner-reply.png`, `editor-xss.json` |
| Khách có voucher | Quản lý cấp voucher 50.000đ; `FJ261020-003` có tổng 800.000đ, giảm **16.000đ** = 20% hoa hồng 80.000đ; cọc còn **224.000đ**, số dư voucher 34.000đ | `11-voucher-booking.png`, script `customer-book.js` |
| Đơn của tôi | Mở đủ `upcoming`, `past`, `cancelled`; chỉ dữ liệu của khách | `customer-book.js`, `scan-fuji-customer.json` |
| Đổi ngày không thêm tiền | `FJ261020-007`: 28–29/10 → 27–28/10, tổng 400.000đ, phí 0, giữ cọc 120.000đ; khách gửi, quản lý duyệt, `CR1002=Applied` | `cancellation-flows.json` |
| Đơn dùng voucher xin đổi | UI báo phải liên hệ nhân viên; không tự mở luồng đổi trái quy tắc hiện có | `change-quote-current.yml`, `18-valid-change-quote.png` |
| Hủy trước 7 ngày | `FJ261020-007`: hoàn **120.000/120.000đ**, nhả đêm | `cancel-28.png`, `cancellation-flows.json` |
| Hủy trước 5 ngày | `FJ261020-008`: hoàn **60.000/120.000đ**, nhả đêm; kế toán ghi hoàn `QA-REFUND-008` | `cancel-25.png`, `accountant-pay-refund.json` |
| Hủy trước 2 ngày | `FJ261020-009`: hoàn **0đ**, nhả đêm | `cancel-22.png` |
| Chuyển thiếu / muộn / không rõ mã | Tạo `Underpaid`, `LatePayment`, `Unmatched`; kế toán khớp khoản không rõ mã vào `FJ261020-022`, hoàn khoản muộn và khoản thiếu | `manual-tx-create.json`, `manual-tx-process.json` |
| Bill | Khách upload bill cho `FJ261020-015/016`; KT duyệt một đơn sang Deposited, từ chối đơn còn lại và khách thấy lý do | `bill-flows.json`, `bill-0.png`, `bill-1.png` |
| Đối soát | Lập/kiểm kỳ gần nhất có 3 bảng dữ liệu; bảng #29 gửi → xác nhận thay có lý do → tiền âm bị chặn → mở lại → gửi lại → ghi đủ **306.000đ**, sang Paid | `settlement-wrong.yml`, `19-settlement-final.png`, `29-settlement-paid.png`, `manual-tx-process.json` |
| Mở lại sau Paid | UI không cung cấp thao tác này; Paid bất biến theo quy tắc đã chốt | `accountant-pay-refund.json`, code `SettlementService.CanTransition` |
| Nhân viên đặt hộ | NV001 tạo khách mới qua SĐT `0977035014`, kênh Phone; `FJ261020-004`, tổng 500.000đ, cọc 150.000đ | `12-staff-created.png` |
| Nhận / giao đơn | NV001 giao cho NV002; NV002 đăng nhập xem được đơn; NV001 nhận đơn chưa giao #67 | `claim-keyboard.json`, script `staff-assign.js` |
| Phòng trống toàn hệ thống | Bảng số phòng từng đêm, giá và làm mới chạy được | `16-availability.png`, `scan-fuji-staff.json` |
| Nhân viên gửi hủy, quản lý duyệt | NV001 gửi hủy #67 → quản lý duyệt → yêu cầu Applied, đơn Cancelled | `staff-cancel-request.json`, `manager-approve-final.json` |
| Đổi cần cọc thêm | QL duyệt CR4 cho `FJ261019-012`; QR `CR4 FJ261019-012` nhận **360.000đ**; KT xác nhận → Applied, đơn Deposited, cọc 600.000đ | `37-additional-confirmed.png`, `additional-confirm.json` |
| Trùng phòng: cùng loại | Chuyển `FJ261020-006` từ 101 sang 102, giữ mã/tổng/cọc/trạng thái | `overbooking-move.json` |
| Trùng phòng: đổi cơ sở | `FJ261017-002` → `FJ261020-019`; cọc cũ 720.000đ, tổng mới 2.880.000đ, khoản bù 480.000đ, đơn mới chờ xác nhận | `31-overbooking-replacement.png`, `overbooking-replace.json` |
| Trùng phòng: hoàn tiền | QL hủy `FJ261019-005`, chốt hoàn 2.400.000đ và voucher xin lỗi | `overbooking-refund.json` |
| Chi khoản bù | Tạo `FJ261020-020` nhận hôm nay → đơn thay thế `FJ261020-021` → xác nhận/nhận/trả → KT ghi chi **240.000đ** | `35-compensation-paid.png`, `compensation-full.json` |
| Đối tác tự đăng ký | Hồ sơ #7 / homestay #36: đăng ký → 3 giấy tờ → gửi → NV yêu cầu bổ sung → chủ bổ sung/gửi lại → QL duyệt | `17-partner-draft.png`, `22-partner-submitted.png`, `23-partner-revision.png`, `partner-approve-contract.json` |
| Hợp đồng và lên tìm kiếm | HĐ `QA-PRELAUNCH-36`, hoa hồng 12%, đến 31/12/2027; chủ tạo phòng QA101, ba giá 500k/600k/800k, ảnh bìa; QL duyệt nội dung, tìm thấy cơ sở | `27-partner-ready.png`, `28-homestay-review.png`, `30-partner-live.png` |
| Báo cáo | Đủ 5 tab overview/bookings/finance/employees/homestays, khoảng 01/06–30/11/2026, có dữ liệu, sáng/tối | `report-tabs.json`, `report-*.png`, `scan-fuji-manager.json` |
| Quản trị tài khoản | Tạo QA001 → đăng nhập mật khẩu tạm → đổi mật khẩu; khóa/mở khóa; reset và đăng nhập lại; logout xóa cookie; 5 lần sai chặn mật khẩu đúng | `13-employee-password-changed.png`, `21-lockout.png`, `admin-lifecycle.json` |
| Danh mục | Thêm/sửa/xóa Areas/RoomCategories/Amenities; SQL cuối không còn ba mục QA. Xóa Hoàn Kiếm đang dùng bị chặn | `catalogs.json`, kiểm SQL inventory |
| Lịch phòng AJAX | POST 201 / DELETE 204, URL không đổi; khách gửi form vào đêm đã khóa bị báo hết phòng; khóa đêm có đơn trả **409** và mã đơn | `32-calendar-block.png`, `32-guest-blocked.png`, `33-calendar-unlock.png`, `calendar-ajax.json`, `final-smoke.json` |

Chưa chạy nhánh **mời/kích hoạt bằng UI** trên một hồ sơ EmployeeAssisted mới: cổng `/Partner` tạo tài khoản hoạt động ngay, còn lời mời chỉ dành cho hồ sơ do nhân viên tạo hộ trước kích hoạt. Hiện không có màn tạo mới EmployeeAssisted trong controller Staff. Không gọi service trực tiếp để thay cho bằng chứng người dùng. Các test tự động `PartnerWorkflowRegressionTests` có kiểm token/kích hoạt; chúng không thay thế kiểm E2E qua UI.

## 5. Đồng thời, dữ liệu biên và bảo mật

### Đồng thời và job thật

- **TC04:** 8 HTTP POST đồng thời, mỗi client có cookie/token riêng, một phòng cuối, 2 đêm 01–03/02/2027. Đúng **1 đơn thành công, 7 thông báo hết phòng**, không có RoomNight trùng. Đây là kiểm HTTP được phép riêng ở mục 3.
- Trước sửa gửi lặp: cùng form tạo `FJ261020-010` và `011`. Sau sửa: 2 lượt nối tiếp + 6 lượt đồng thời đều trả `FJ261020-023`; SQL chỉ có **1 đơn / 1 đêm**. Đặt hộ gửi lại cũng trả cùng `/Staff/Bookings/Details/1019`.
- Idempotency lưu SubmissionId trong AuditLog, cùng transaction giữ đêm; SQL khóa hàng khách để serialize giữa các app instance. Form riêng vẫn được đặt hai phòng hợp lệ. Không đổi schema. Phải giữ lịch sử audit để bảo toàn khả năng nhận ra form cũ.
- Bản hạn ngắn tạo `005/006`: `005` tự Expired, còn **0 đêm**; `006` đã cọc sinh `ConfirmAlertSentAt=2026-10-19 17:35:09 UTC`, không tự hủy. Sau đó đơn này được dùng kiểm xác nhận/đổi/hủy.
- Tab cũ xác nhận lại bị thông báo không thể chuyển Confirmed → Confirmed; F5 không lặp chuyển trạng thái. Modal Esc hủy được. `two-tab-confirm.json`, `keyboard-modal.json`.

### Biên dữ liệu

`boundaries.json` ghi nhận: ngày quá khứ, trả trước nhận, quá 30 đêm, vượt sức chứa, SĐT `12345`, tên/SĐT trống, tên 101 ký tự, ghi chú 501 ký tự đều không tạo đơn và không trả 500. SĐT có khoảng trắng và `+84` được chuẩn hóa, tạo được đơn. Emoji/dấu Việt lưu và hiển thị được. Tiền âm ở đối soát bị chặn; form Simulator đặt mức tối thiểu 1.000đ. Bộ lọc giữ `MaxPrice` khi chuyển trang 2; trang ngoài phạm vi trả trang hợp lệ, không 500.

### Quyền và CSRF

| Người dùng | Admin tạo nhân viên | Staff đơn | Owner lịch | Accounting giao dịch | Dev giả lập |
|---|---|---|---|---|---|
| Chưa đăng nhập | Login | Login | Login | Login | Login |
| Customer | 403 | 403 | 403 | 403 | 403 |
| Owner | 403 | 403 | 200 của mình | 403 | 403 |
| NV001 | 403 | 200 | 403 | 403 | 403 |
| KT001 | 403 | 403 | 403 | 200 | 403 |
| QL001 | 403 | 200 | 403 | 200 đọc | 200 |
| AD001 | 200 | 200 | 403 | 200 đọc | 200 |

Quyền quản lý/đọc giao nhau được đối chiếu FR, không coi mọi URL khác area đều phải bị chặn. Owner A đọc đơn, homestay, lịch và đối soát của B đều **404**. API confirm/block/unblock của B cũng **404 với token CSRF hợp lệ**. Khách khác không đọc được đơn QA; mã đúng/SĐT sai không lộ thông tin.

Đã gửi **49 form POST** lấy từ trang thật mà bỏ token: đều **400**; thêm POST/DELETE API chủ thiếu token cũng 400. Rà filter toàn cục MVC và các attribute API ghi. Webhook là ngoại lệ có chủ đích, dùng API key.

### XSS, đăng nhập, webhook, bí mật

- Ghi chú/nhận xét chứa script, thẻ img/onerror và javascript: không thực thi. Editor server loại script/event handler/javascript URL; đã kiểm HTML lưu rồi khôi phục mô tả mẫu. Nhận xét QA vẫn giữ văn bản encode để xem lại.
- Tài khoản thử bị chặn sau 5 lần sai; đăng xuất xóa cookie Identity. `returnUrl=https://evil.invalid/` trả redirect **`/`**. PasswordOptions yêu cầu tối thiểu 8 ký tự và độ phức tạp Identity; luồng đổi mật khẩu qua UI chạy được.
- Webhook JSON đúng định dạng, thiếu/sai API key → **401**. Cùng transaction ID gửi hai lần → lần hai `duplicate=true`, không thêm dòng tiền lần hai. Ngày sai định dạng → **400 ProblemDetails** sau sửa.
- `git grep` tìm 296 file ứng viên chứa các từ khóa password/key/secret/token; quét thêm mẫu khóa API, AWS access key và private key không thấy bí mật độ tin cậy cao. Mật khẩu demo công khai và placeholder cấu hình được phân biệt với credential vận hành. Không coi phép quét này là chứng minh tuyệt đối không thể có bí mật dạng khác.

### Upload — chưa đạt

- MZ giả `.jpg`, ảnh không có chữ ký hợp lệ bị từ chối. File 6 MB và 10 MB không được lưu. File hợp lệ được đổi tên GUID trong thư mục riêng tư.
- Kiểm sâu bằng multipart có token hợp lệ tại `/Owner/Homestays/Document/1`, type Other:

| File | Thực tế |
|---|---|
| `qa-broken.png`, chỉ 12 byte đầu PNG | **Được nhận**, lưu `.png` dù ảnh không giải mã được |
| `../qa-path.png`, PNG hợp lệ | **Được nhận**, tên gốc được rút về `qa-path.png` |
| `qa-disguised.exe`, Content-Type octet-stream nhưng byte PNG | **Được nhận**, đổi sang GUID `.png` |
| `qa-mismatch.jpg`, Content-Type JPEG nhưng byte PNG | **Được nhận**, đổi sang GUID `.png` |

Nguyên nhân: `ReceiptFileStore.ValidateImageAsync` chỉ kiểm size và magic bytes, chưa đối chiếu extension/MIME và chưa xác minh ảnh đầy đủ. GUID tránh dùng tên người gửi làm đường dẫn lưu, nhưng checklist yêu cầu từ chối các đầu vào này vẫn không đạt. Bằng chứng: `upload-deep.json`, `upload-server-results.json`; các dòng giấy tờ thử được giữ trong DB QA. Chưa quan sát thực thi mã từ những file này.

## 6. Thiết bị, ngôn ngữ, bàn phím

- **90 cấu hình**: 7 vai trò trên Chromium và luồng khách trên WebKit/Firefox × 5 độ rộng **375/390/412/768/1024** × sáng/tối. Các trang được quét không có tràn ngang hay pageerror. Đây là tập trang trong `scan-fuji-*.json`, không phải mọi route × mọi tổ hợp.
- WebKit/Firefox đều thực sự tạo rồi hủy đơn (`012/013`), không chỉ mở trang. Context mobile cho iPhone SE 375×667, iPhone 14 390×844, iPad 768×1024 và 1024×768 mở được form/lịch. WebKit báo `maxTouchPoints=0` dù đã đặt hasTouch; Android 412×915 Chromium báo 1. Chưa coi đây là kiểm Safari trên iPhone vật lý.
- Calendar 375px đo được ô tương tác **44,72×72px**, không tràn; nút chính đặt phòng đọc/bấm được. Một số nút đóng alert cao **32px**, chưa đạt mục tiêu 44px cho mọi vùng bấm.
- Luồng khách English đi từ tìm → chi tiết → đặt → QR → tra cứu; mỗi vai trò nội bộ có ít nhất một màn EN. Không thấy chữ điều khiển tiếng Việt trong luồng khách; tên người/cơ sở, nội dung thông báo/audit đã lưu vẫn là dữ liệu Việt theo quyết định UI trước đó. Tiền giữ dạng VND `vi-VN`, ngày theo quy ước giao diện đã chốt.
- Tắt JavaScript vẫn đọc được trang và đã tạo `FJ261020-014` bằng form native/Enter, không tràn ngang. Reduced motion ở trang chủ: nội dung h1 hiện, **0 animation đang chạy** tại thời điểm đo.
- Header/form có focus; modal vào focus, Tab qua nút đóng/Hủy/Xác nhận, Esc đóng. Một mẫu Tab chuyển activeElement về BODY trước khi quay lại nút modal; chưa kết luận đầy đủ về focus trap trên mọi tổ hợp browser/assistive technology.
- **Không kiểm được:** bàn phím ảo thật che nút, VoiceOver/TalkBack, thiết bị vật lý và mọi route nội bộ ở đủ 3 browser. Đây là giới hạn kiểm chứng, không được tính là pass.

Bằng chứng: `device-webkit.json`, `home-and-android.json`, `final-browser.json`, `25-no-js-booked.png`, `38-english-qr.png`, `40-final-calendar.png`, các ảnh `scan-*` và `device-*`.

## 7. Hiệu năng

### Performance API — thời gian cục bộ, mili giây

Các số UI sau được lấy trong lượt quét trước bản tối ưu FR-08; bản publish cuối đã smoke các trang chính. DCL/load tính từ navigation start, TTFB từ request start. Không suy rộng thành SLA Internet hoặc tải đồng thời thực tế.

| Trang, Chromium | TTFB | DCL | Load |
|---|---:|---:|---:|
| Trang chủ | 113 | 301 | 307 |
| Tìm có lọc | 97 | 131 | 189 |
| Chi tiết | 74 | 112 | 171 |
| Đặt phòng | 14 | 101 | 115 |
| Đơn của tôi | 67 | 98 | 104 |
| Owner dashboard | 111 | 203 | 204 |
| Owner calendar | 135 | 208 | 211 |
| Staff dashboard | 116 | 195 | 197 |
| Staff availability | 1.195 | 1.270 | 1.272 |
| Accounting dashboard | 226 | 298 | 299 |
| Admin dashboard | 155 | 211 | 211 |
| Báo cáo | 263 | 421 | 422 |

Chậm nhất trong tập đa engine: chi tiết WebKit **1.626 / 2.254 / 2.255ms**. Chromium chậm nhất là phòng trống nhân viên. Các số và version engine đầy đủ trong `scan-fuji-*.json`.

### NFR-02 / NFR-05

- Dữ liệu benchmark: **150 homestay, 1.500 phòng, 108.900 block, 326.700 đêm bận**, 31 đêm; transaction luôn rollback trên ConstraintTest rỗng.
- Lượt chạy chung trước sửa: FR-08 **13.167ms**, test thất bại; matrix 996ms, search 2.408ms. Giữ bằng chứng tại `test-final.log` và `test-final/final.trx`.
- Tối ưu: dùng lại scope phòng bán được trong truy vấn đêm, gom giá theo RoomType, không nạp ảnh/tiện nghi khi caller chỉ cần DTO phòng trống/phương án thay thế. Test giá 31 đêm kiểm riêng các loại và phiên bản EffectiveFrom.
- Benchmark SQL được đánh dấu không chạy cùng collection khác để số đo không tranh tài nguyên với hàng trăm fixture SQLite/InMemory. **Ngưỡng 5.000ms/3.000ms không đổi.** Đây là đo cô lập, không phải chứng minh tải vận hành thực tế.
- Lượt toàn bộ sau tối ưu/cô lập, 1.740 test: matrix **407ms**, FR-08 **1.285ms**, search **645ms**; tất cả đạt. `test-final-isolated/final.trx`.
- Lượt xác nhận cuối, **1.741 test**: matrix **309ms**, FR-08 **1.334ms**, search **673ms**; tất cả đạt. `test-final-confirmed/final.trx`.

### Request và dung lượng trang chủ

Context mới, tại mốc load: **35 request**, khoảng **1,20 MiB encoded body**, gồm HTML 29.128B, CSS 8 request / 368.632B, JS 14 / 258.858B, ảnh 11 request đã tải / 605.682B và manifest 980B. DOM có 18 ảnh; tất cả có width/height, 17 ảnh dùng lazy, hero không lazy. Số này chưa bao gồm các ảnh lazy tải thêm khi cuộn; không phải tổng mọi tài nguyên có thể tải. `home-and-android.json`.

## 8. Lỗi đã sửa

| Mức | Nguyên nhân / thay đổi | Commit | Hồi quy và xác minh |
|---|---|---|---|
| Trung bình | Publish thiếu PNG giấy tờ/bill mẫu; khai báo content để mang file vào output/publish | `379411a` | `SeedAssetPackagingTests`: 2 fail trước sửa → 2 pass; cmp file publish; sau chạy lại có file giấy tờ/bill |
| Trung bình | Webhook ParseExact ném exception khi ngày sai; đổi sang TryParseExact, trả ProblemDetails 400 | `6c76271` | `BankWebhookValidationTests`: 3 fail + 1 pass trước sửa → 4 pass; Production API 400 không lộ stack |
| Cao | Form tạo đơn không có định danh gửi; replay giữ phòng thứ hai | `83422f2` | 3 test SQLite; hidden form ID; nhóm 30 test pass; HTTP thật 2+6 lượt cùng đơn; đặt hộ cũng cùng đơn |
| Trung bình | Truy vấn dated dùng chung dữ liệu ảnh/tiện nghi không cần thiết và quét giá/tập ID lặp | `f22de19` | 75 test tìm kiếm/giá/tồn/trùng phòng pass, gồm SQL performance; test 31 đêm/EffectiveFrom |
| Thấp, test infrastructure | Benchmark tranh tài nguyên với collection khác | `e167871` | Lượt full 1.740/1.740 pass với RUN_PERF_TESTS=1; giữ log lượt thất bại trước đó |
| Trung bình | Error action bị CSRF khi middleware re-execute POST; chỉ bỏ antiforgery ở action lỗi chỉ đọc | `76ebd00` | `ProductionErrorHandlingTests`: trước sửa 400 thay vì 500; sau sửa 500 HTML thân thiện, không có exception sentinel |

Không thêm thư viện ứng dụng, không migration/schema, không đổi nghiệp vụ hủy/đối soát/voucher. Chỉ cài thêm browser engine vào cache máy cho QA.

## 9. Build và test

- Lượt nền: **1728 pass / 1 skip / 0 fail**; skip là benchmark opt-in.
- Lượt full có benchmark trước tối ưu: **1738 pass / 1 fail**, lỗi FR-08 13,167 giây được điều tra và giữ log.
- Lượt sau tối ưu và cô lập SQL: **1740 pass / 0 skip / 0 fail**.
- Hồi quy Error handler bổ sung sau lượt 1740: **1/1 pass**, đã có lượt đỏ trước sửa.
- Lượt cuối cho mã ứng dụng `76ebd00`: `dotnet build -c Release` **0 cảnh báo / 0 lỗi**; `RUN_PERF_TESTS=1 dotnet test HomestayBooking.sln -c Release --no-build` **1741 pass / 0 skip / 0 fail**, thời gian test **3 phút 46 giây**. Bằng chứng: `build-final.log`, `test-final-confirmed.log`, `test-final-confirmed/final.trx`.
- Đã publish lại sau lượt build cuối và smoke DLL mới: trang chủ/tìm/chi tiết/đặt trả 200, form có SubmissionId; chặn đêm có đơn trả 409; Production quản trị đăng nhập được, ba route Swagger/Dev đều 404. `publish-final.log`, `final-smoke.json`, `prod-auth.json`. Trang `/Home/StatusCode/500` gọi trực tiếp trả 200 là smoke giao diện lỗi; bằng chứng HTTP 500 khi exception thật nằm ở test pipeline, không coi URL này là phép gây lỗi server.

Test đầu trong sandbox có lỗi TLS macOS `bad certificate format` và một lượt fixture timeout khởi tạo host; đã chạy lại bằng quyền hệ thống phù hợp. Các lỗi môi trường đó không được tính là ứng dụng pass. Benchmark tách collection cũng không thay thế một thử tải production riêng.

### Đối chiếu TC01–TC12, mục 13.2

Ngày trong ca mẫu được thay bằng ngày tương lai còn phòng tại thời điểm chạy; các điều kiện nghiệp vụ được giữ. Ca chỉ kiểm một phần được ghi rõ, không suy từ toàn bộ test xanh thành E2E đạt.

| Ca | Kết quả | Đầu ra thực tế và bằng chứng |
|---|---|---|
| TC01 — Đặt hợp lệ | Đạt | Form tạo AwaitingPayment, mã FJ và QR/cọc đúng; luồng `001` sau đó khớp cọc. Ca hai đêm `002` giữ đúng hai đêm trước khi Expired; `03-qr-loaded.png`, `concurrency.json` |
| TC02 — Trả trước nhận | Đạt phía server | Ngày đảo bị validation, không tạo đơn, không 500. Không khẳng định mọi browser đều chặn gửi từ client; `boundaries.json` |
| TC03 — SĐT sai | Đạt | `12345` bị từ chối, không tạo đơn; khoảng trắng/+84 hợp lệ được chuẩn hóa; `boundaries.json` |
| TC04 — Phòng cuối đồng thời | Đạt | 8 client/token độc lập: 1 thành công, 7 hết phòng, không trùng đêm; `concurrency.json` |
| TC05 — Quyền chủ | Đạt | Chủ A mở đơn/cơ sở/lịch/đối soát B nhận 404; API ghi với token hợp lệ cũng 404; `security-matrix.json`, `security-extra.json` |
| TC06 — Tra cứu sai SĐT | Đạt | Mã đúng, SĐT sai không trả thông tin đơn; `security-matrix.json` |
| TC07 — Upload | **Lỗi ở kiểm mở rộng** | MZ đổi đuôi JPG và file 10 MB bị từ chối; ảnh 12 byte/header PNG được nhận, cùng các sai lệch tên/MIME. Không đánh dấu cả nhóm upload đạt; `upload-deep.json` |
| TC08 — XSS editor | Đạt các payload đã thử | Script/onerror/javascript bị loại khi lưu editor; nhận xét encode không chạy; mô tả mẫu đã khôi phục; `editor-xss.json` |
| TC09 — Lọc/phân trang | Đạt phần đã thử | Click trang 2 giữ MaxPrice=1000000; URL AreaId=3 + giá + page=2 trả 200, page=99999 không 500. Chưa xác minh lại tập kết quả đúng bộ mẫu Tây Hồ bằng click phân trang; `final-browser.json`, `boundaries.json`, `final-smoke.json` |
| TC10 — English | Đạt phần đã thử | Chọn EN, các navigation tiếp theo vẫn EN; khách đi hết tìm/đặt/QR/tra cứu, mỗi vai trò một màn EN. Nội dung dữ liệu đã lưu và cách hiển thị VND giữ quyết định cũ; `english-flow.json`, `38-english-qr.png`, `scan-fuji-*.json` |
| TC11 — Responsive 375 | Đạt mô phỏng, giới hạn thiết bị | Không tràn ngang, menu thu gọn, đặt phòng/lịch dùng được; chưa kiểm keyboard ảo trên máy thật và còn nút đóng 32px; `device-webkit.json`, `final-browser.json` |
| TC12 — Hủy trước 5 ngày | Đạt | `FJ261020-008` chốt hoàn 60.000/120.000đ, nhả phòng; KT ghi chuyển hoàn riêng `QA-REFUND-008`; `cancellation-flows.json`, `accountant-pay-refund.json` |

## 10. Lỗi còn lại và việc cần quyết định

| Mức | Cách tái hiện / tác động | Tình trạng |
|---|---|---|
| **Trung bình — chặn Go** | Gửi file có header PNG nhưng không phải ảnh hoàn chỉnh; hoặc PNG tên `.exe` / MIME JPEG / filename có `../` qua form giấy tờ. Server nhận và ghi metadata. Xem `ReceiptFileStore.cs:32` | Cần validation extension/MIME/signature và tính hợp lệ của ảnh, kiểm đủ JPG/PNG/WEBP; TC07 chưa đạt |
| Trung bình | Partner/Status và Owner giấy tờ quảng cáo PDF, trong khi pipeline chỉ nhận JPG/PNG/WEBP | Cần thống nhất hợp đồng upload. Không tự mở hỗ trợ PDF trong đợt QA |
| Thấp | Các nút đóng alert 32px, dưới mục tiêu touch 44px | Nút chính/lịch đã đạt; còn thiếu ở nút đóng chung |
| Thấp | Thiếu CSP/nosniff/referrer policy chung/permissions policy trên phản hồi đo | Cần cấu hình app/proxy phù hợp trước triển khai; không có tuyên bố đã harden đầy đủ |
| Giới hạn kiểm chứng | Chưa thử keyboard ảo/AT/thiết bị thật; WebKit touch báo 0; chưa kiểm focus trap mọi tổ hợp | Không đánh dấu pass |

### Cần người dùng quyết định

1. Chốt bản vá upload hoàn chỉnh và cách giải mã/xác minh ảnh. Phiên QA giữ giới hạn không thêm thư viện; chưa triển khai một bộ giải mã ảnh mới hoặc mở thêm định dạng. Cần chạy lại TC07 sau bản vá.
2. Chốt acceptance đối tác: **tự đăng ký đã có tài khoản** và **nhân viên tạo hộ rồi mời kích hoạt** là hai nhánh khác nhau. Không có bước mời sau khi tự đăng ký/được duyệt. Nếu cần chạy nhánh assisted hoàn toàn qua UI, cần bổ sung màn tạo hồ sơ assisted; không thay bằng gọi service trong báo cáo này.
3. Giữ tiền/ngày EN và nội dung thông báo/audit Việt theo quyết định UI hiện có, hay mở một công việc dịch dữ liệu hệ thống đã lưu. Phiên này giữ quyết định cũ.
4. Khi triển khai thật, cấu hình TLS/proxy, khóa SePay thật, ngân hàng và kênh chuyển lời mời. SMS/Zalo thật vẫn là phần rút gọn đã chấp thuận; QA không gửi tin bên ngoài.

**Điều kiện chuyển sang Go:** sửa và kiểm lại upload, chốt hai nhánh onboarding/định dạng giấy tờ, xác minh cấu hình Production thực tế; chạy lại các ca liên quan và giữ build/test/consistency xanh. Các kết quả tự động xanh hiện tại không bao phủ lỗi upload đã được chứng minh bằng HTTP.

## 11. Dữ liệu giữ lại và kết thúc

**Giữ nguyên hai database để xem lại:**

- `HomestayBooking_PreLaunch`: 22 tài khoản, 6 nhân viên, 7 chủ, 36 homestay, 129 phòng, **85 đơn**, 67 payment, 55 bank transaction ở snapshot cuối. Gồm 60 đơn seed và **25 đơn QA `FJ261020-001`…`025`**. Các đơn chờ cọc có thể tự Expired khi người dùng bật lại app.
- `HomestayBooking_PreLaunchProd`: 1 quản trị QA bootstrap, không có homestay/đơn demo.

Các dữ liệu nổi bật: QA001; chủ `0977065014`, hồ sơ #7, cơ sở #36/phòng QA101/hợp đồng `QA-PRELAUNCH-36`; voucher cấp từ `FJ261019-012`; các yêu cầu đổi/hủy, bill, hoàn tiền và khoản bù có tham chiếu `QA-*`; 4 giấy tờ upload âm tính đã được nhận, lưu GUID để xem lại lỗi. Ba danh mục QA thêm thử đã được xóa qua giao diện. Mô tả homestay #1 đã khôi phục sau XSS; nhận xét encode vẫn giữ.

`database/tests/data_consistency.sql` chạy khi app QA đã dừng: **35/35 OK trên cả hai database**, không sửa dữ liệu để làm script xanh. `HomestayBooking_ConstraintTest` không để lại dòng nghiệp vụ sau test. Chi tiết ở `data-inventory.txt`, `*-consistency-final.log` và TRX SQL.

Các app 5014/5015/5016/5017 và browser do phiên QA mở được dừng bằng PID/session cụ thể. Không dùng pkill/killall, không dừng/xóa SQL container, không dừng app 5011. Không push/merge. Bằng chứng chỉ có ở thư mục tạm trên máy này.

## 12. Sửa sau báo cáo và kiểm lại (20/10/2026)

Người dùng chốt: **giấy tờ đối tác nhận PDF** (bill và ảnh homestay vẫn chỉ nhận ảnh); **màn nhân viên tạo hồ sơ đối tác hộ để sau** (ra mắt với luồng tự đăng ký `/Partner`, đã chạy trọn ở mục 4).

| Việc | Sửa | Kiểm |
|---|---|---|
| Upload nhận ảnh hỏng, đuôi/MIME sai, tên `../` (chặn Go) | `ReceiptFileStore`: tên file trần (không thư mục, `..`, ký tự điều khiển); đuôi và Content-Type khai báo phải khớp nội dung (`application/octet-stream`/trống thì chỉ dựa vào nội dung); kiểm cấu trúc: PNG đủ khối IHDR→IEND đúng CRC, JPEG có SOF kích thước > 0 + SOS + EOI, WEBP RIFF khớp độ dài + khối VP8/VP8L/VP8X; PDF (chỉ giấy tờ) `%PDF-` + đối tượng + `%%EOF`. Ghi đúng phần byte đã kiểm, không đọc lại request | `UploadValidationTests` (16 ca) + test cũ đổi sang file thật nhỏ (`TestFiles`); HTTP thật trên `HomestayBooking_PreLaunch`, bảng dưới |
| Giao diện quảng cáo PDF nhưng không nhận | Lưu giấy tờ gọi `SaveAsync(..., allowPdf: true)`; `ContentTypeOf(.pdf) = application/pdf`; tải về dạng attachment | Tải lại PDF vừa nộp: 200, `application/pdf`, `attachment` |
| Nút đóng 32px | Dưới 576px `.btn-close` 44×44px, biểu tượng giữ cỡ | UI Kit 375px: 44×44; 1280px giữ nguyên |
| Thiếu header bảo mật | Middleware mặc định `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy`, CSP `frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'`; trang tự đặt chặt hơn (mật khẩu tạm `no-referrer`) giữ nguyên; antiforgery không đặt SAMEORIGIN nữa | `SecurityHeadersTests`; curl trang chủ |

**TC07 kiểm lại bằng multipart thật tới `/Owner/Homestays/Document/1` (chủ `0912000001`):**

| File | Mong đợi | Thực tế | Kết quả |
|---|---|---|---|
| `qa-broken.png` | Từ chối | Từ chối – File ảnh bị hỏng hoặc không đầy đủ. | Đạt |
| `../qa-path.png` | Từ chối | Từ chối – Tên file không hợp lệ. | Đạt |
| `qa-disguised.exe` | Từ chối | Từ chối – Đuôi file không khớp với nội dung file. | Đạt |
| `qa-mismatch.jpg` | Từ chối | Từ chối – Đuôi file không khớp với nội dung file. | Đạt |
| `qa-large.png` | Từ chối | Từ chối – File phải có dung lượng từ 1 byte đến 5MB. | Đạt |
| `qa-valid.png` | Nhận | Nhận | Đạt |
| `qa-valid.pdf` | Nhận | Nhận | Đạt |
| `qa-broken.pdf` | Từ chối | Từ chối – File PDF bị hỏng hoặc không đầy đủ. | Đạt |

Kết quả kiểm tra sau sửa: `dotnet build -c Release` 0 cảnh báo/0 lỗi; `dotnet test` **1759 pass, 1 skip** (test hiệu năng chỉ chạy khi `RUN_PERF_TESTS=1`).

**Chưa làm (ghi lại):** CSP đầy đủ cho `script-src`/`style-src` cần nonce cho script chống nháy theme inline trong `_HeadAssets`; màn tạo hồ sơ đối tác hộ.

**Kết luận cập nhật: GO có điều kiện.** Lỗi chặn duy nhất (upload) đã sửa và kiểm lại đạt. Điều kiện trước khi mở cho khách thật: cấu hình TLS/proxy và chuỗi kết nối Production, khóa SePay và tài khoản ngân hàng thật, cấp quản trị viên đầu tiên theo quy trình B7, và thử một lượt trên điện thoại thật.
