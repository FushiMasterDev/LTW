# Prompt: API desktop cho đối soát, hoàn tiền và khoản bù (phía kế toán)

Repo `/Users/phamngocphu/Downloads/LTW`, nhánh **`feature/desktop-api-settlements`** (tạo từ `main` = `e30439f`). Ba đợt API desktop trước đã merge: nhân viên, chủ homestay, kế toán giao dịch ngân hàng và cọc bổ sung.

Đọc trước:

- `docs/prompts/desktop-api.md`, `docs/prompts/desktop-api-owner.md`: mọi ràng buộc của các đợt trước vẫn áp dụng.
- `docs/api/desktop-api-theo-vai-tro.md`, `docs/api-va-backend.md` mục 8, `docs/api/winforms-giao-dien.md` mục 2.3, 4.9 và 4.10.
- `HomestayBooking.Web/DesktopApi/*`, `HomestayBooking.Web/Controllers/Api/Desktop/*`, chủ yếu `DesktopController.cs`, `DesktopBankTransactionsController.cs`, `DesktopAdditionalDepositsController.cs`, `DesktopReceiptsController.cs` và `Owner/DesktopOwnerFinanceController.cs` (API đối soát phía chủ, có sẵn DTO `OwnerSettlement*`).
- Web cần bám theo: `Areas/Accounting/Controllers/AccountingController.cs`, `RefundsController.cs`, `CompensationsController.cs`, `SettlementsController.cs`, `DashboardController.cs`.
- Service: `Services/AccountingService*.cs`, `Services/CompensationService.cs`, `Services/SettlementService.cs`, `Services/SettlementCalculator.cs`.
- Test: `HomestayBooking.Tests/Integration/DesktopApiTests.cs`, `DesktopOwnerApiTests.cs`, `DesktopSqliteHost.cs`, `DesktopStandaloneHostTests.cs`.

## Mục tiêu

Thêm REST API dưới `/api/desktop/...` để WinForms của kế toán làm được **ba trang Kế toán trên web**: Hoàn tiền (`/Accounting/Refunds`), Khoản bù (`/Accounting/Compensations`) và Đối soát (`/Accounting/Settlements`). Thêm cả số việc đang chờ (`LoadCountsAsync`) để làm huy hiệu trên menu.

API chỉ là lớp vỏ mỏng gọi lại **service có sẵn**, không viết lại nghiệp vụ. Logic đang nằm trong controller MVC thì chuyển vào service hoặc helper dùng chung để web và desktop cùng gọi, giữ nguyên hành vi web. Ít nhất có:

- `SettlementsController.LatestPeriod()`: kỳ gần nhất đã qua thời gian chờ.
- `SettlementsController.HistoryItem()`: đọc AuditLog thành bước và ghi chú.
- Phần đếm của `AccountingController.LoadCountsAsync`.

Không sửa `HomestayBooking.Desktop` (WinForms). Giao diện làm sau.

**Yêu cầu số 1: không ảnh hưởng web.** Mọi trang MVC, endpoint `/api/...` cũ và `/api/desktop/...` hiện có giữ nguyên đường dẫn, request/response, xác thực, antiforgery và trang lỗi. Toàn bộ test hiện có phải còn pass.

## Phân quyền

Bám đúng web, cả quyền của controller lẫn kiểm tra trong service:

| Nhóm | Đọc | Ghi |
|---|---|---|
| Hoàn tiền | `AppPolicies.Accounting` (Kế toán, Quản lý, Quản trị) | Như web: policy `Accounting`, service `HandlerAsync` tự chặn người không được xử lý |
| Khoản bù | `Accounting`; riêng danh sách "có thể từ chối" (`rejectable`) chỉ Kế toán | **Chỉ Kế toán** (web trả Forbid → desktop trả 403) |
| Đối soát | `Accounting` | **Chỉ Kế toán**: lập bảng, gửi, xác nhận thay chủ, mở lại, ghi nhận đã trả |
| Số việc chờ | `Accounting` | |

- Chỉ nhận Bearer token của nhân viên. Token chủ gọi vào → 403, không token → 401. Dùng base `DesktopController` có sẵn, nhóm này ở tag Swagger riêng (ví dụ "Desktop Accounting").
- Không dùng `CurrentEmployeeIdAsync` để bỏ qua kiểm tra của service. Service nào nhận `Actor` thì truyền `Actor`.

## Endpoint đề xuất

Tên có thể đổi nếu có lý do, ghi lý do vào báo cáo.

| Phương thức | Đường dẫn | Gọi |
|---|---|---|
| GET | `/api/desktop/refunds?done=false&q=&page=` | `AccountingService.RefundsAsync`. `done=false`: chờ hoàn, cũ nhất trước; `done=true`: đã hoàn, mới nhất trước |
| POST | `/api/desktop/refunds/{bookingId}/record` `{ "bankRef": "..." }` | `RecordRefundAsync`; trả số tiền đã ghi nhận |
| GET | `/api/desktop/compensations?rejectable=false&page=` | `CompensationService.PendingAsync` / `RejectableAsync`, phân trang bằng `DesktopPage.Slice` |
| POST | `/api/desktop/compensations/{paymentId}/paid` `{ "bankRef": "..." }` | `RecordPaidAsync` |
| POST | `/api/desktop/compensations/{paymentId}/reject` `{ "reason": "..." }` | `RejectAsync` |
| GET | `/api/desktop/settlements?year=&month=&periodNo=&homestayId=&status=&page=` | `SettlementService.ListAsync`. Kiểm tham số như web: năm, tháng, kỳ phải đủ cả ba hoặc bỏ cả ba |
| GET | `/api/desktop/settlements/latest-period` | Kỳ gần nhất có thể lập (`year`, `month`, `periodNo`, ngày bắt đầu/kết thúc) để WinForms đặt mặc định |
| GET | `/api/desktop/settlements/{id}` | `DetailsAsync`: bảng, từng đơn, khoản bù, lịch sử (bước đã dịch và ghi chú như web) |
| POST | `/api/desktop/settlements/generate` `{ "year", "month", "periodNo", "homestayId": null }` | `GenerateAsync` (một homestay) hoặc `GeneratePeriodAsync` (cả kỳ). Trả số bảng có dữ liệu, id bảng (nếu một homestay) và **danh sách mã đơn chưa chốt** |
| POST | `/api/desktop/settlements/generate-recent` | `GeneratePeriodAsync(LatestPeriod())`, trả như trên |
| POST | `/api/desktop/settlements/{id}/send` | `SendAsync` |
| POST | `/api/desktop/settlements/{id}/confirm-on-behalf` `{ "reason" }` | `ConfirmOnBehalfAsync` |
| POST | `/api/desktop/settlements/{id}/reopen` `{ "reason" }` | `ReopenAsync` |
| POST | `/api/desktop/settlements/{id}/record-paid` `{ "bankRef", "amount", "paidAt" }` | `RecordPaidAsync(id, bankRef, amount, paidAt, actor)` |
| GET | `/api/desktop/accounting/counts` | Số việc chờ như thanh tab web (bill, giao dịch, cọc bổ sung, hoàn tiền, khoản bù, đối soát…) |

Lưu ý:

- `paidAt` là **thời điểm UTC** theo quy ước desktop (ISO 8601 có `Z` hoặc offset). Không có offset → 400. Không dùng `VietnamTime.ToUtc` cho giá trị đã là UTC. `amount` là số JSON, không phải chuỗi.
- Ghi thành công trả DTO của bản ghi sau khi đổi (ví dụ `{ id, status }` của bảng đối soát), không trả 204 trống, để WinForms cập nhật dòng mà không cần tải lại.
- Thao tác không hợp lệ theo trạng thái (gửi bảng đã gửi, ghi nhận trả khi chưa xác nhận…) dùng đúng `BookingException` của service → 400 qua `DesktopExceptionFilter`. Câu "Không tìm thấy…" mới thì thêm vào danh sách 404 của filter.
- Kiểm tra validation (thiếu `bankRef`, lý do quá 500 ký tự…) giữ ở service như web. Không thêm luật mới chỉ cho desktop.

## Quy ước (giữ như các đợt trước)

- DTO riêng cho desktop trong `DesktopApi/`. Enum dạng chữ chỉ trong DTO desktop, không đổi `JsonOptions` toàn cục.
- Ngày `yyyy-MM-dd`, timestamp UTC (`DesktopMapping.Utc`).
- Phân trang `DesktopPage`. Lỗi ProblemDetails qua `DesktopExceptionFilter`.
- XML doc và `ProducesResponseType` đầy đủ để Swagger của DesktopHost hiện đúng. Swagger của web vẫn ẩn nhóm `/api/desktop`.
- Nếu DTO bảng đối soát giống `OwnerSettlementDto`/`OwnerSettlementDetailsDto` thì dùng lại mapping. Không để lộ cho kế toán thông tin chỉ chủ được thấy, và ngược lại.

## Dữ liệu demo trên DesktopHost

Kiểm dữ liệu seed SQLite (`SeedDevelopmentDataAsync`) có đủ để thử trên WinForms không: ít nhất một đơn **chờ hoàn tiền**, một **khoản bù chờ chi**, một bảng đối soát ở mỗi trạng thái `Draft`/`Sent`/`Confirmed`/`Paid`, và một kỳ có thể lập bảng mới. Thiếu thì thêm tối thiểu, đi qua service hoặc đúng luật dữ liệu. Không làm trượt test đang đếm dữ liệu seed; nếu phải sửa số đếm trong test thì ghi rõ trong báo cáo.

## Test

Thêm test tích hợp theo kiểu `DesktopApiTests` (host SQLite qua `DesktopSqliteHost` hoặc cách các test desktop hiện dùng):

- **Ma trận quyền** cho từng endpoint: Tư vấn → 403, Kế toán → đúng, Quản lý/Quản trị đọc được và ghi → 403 ở nhóm chỉ Kế toán, token chủ → 403, không token → 401.
- Mỗi thao tác ghi có ít nhất **một ca thành công và một ca lỗi** (sai trạng thái, thiếu `bankRef`/lý do, id không tồn tại → 404).
- Vòng đời đối soát đầy đủ qua API: lập → gửi → (chủ xác nhận qua `/api/desktop/owner/settlements/{id}/confirm` hoặc kế toán xác nhận thay) → ghi nhận đã trả; mở lại từ trạng thái được phép.
- `record-paid` với `paidAt` không có offset → 400; có `Z` → lưu đúng UTC.
- `generate` trả đúng danh sách mã đơn chưa chốt.
- Gọi lại cùng thao tác ghi lần hai (ví dụ ghi nhận hoàn tiền hai lần) không tạo bản ghi đôi.
- Test Swagger cập nhật số endpoint.

Test hiện có giữ nguyên, không sửa để cho qua.

## Tài liệu

- `docs/api/desktop-api-theo-vai-tro.md`: bảng quyền mục 2 và mục 4 (Kế toán) thêm các nhóm mới, có ví dụ JSON. Mục 5 và 6 ghi Quản lý, Quản trị chỉ đọc.
- `docs/api-va-backend.md` mục 8: bảng endpoint, quyền, lỗi, ví dụ curl.
- `docs/api/winforms-giao-dien.md`: mục 2.3 bỏ ba dòng Hoàn tiền, Khoản bù, Đối soát; mục 4.9 và 4.10 ghi API đã có (danh sách endpoint), trạng thái form vẫn là "Rỗng, chờ nối API".
- Báo cáo `docs/api/desktop-api-settlements-report.md`: endpoint, file sửa, logic nào đã chuyển từ controller vào service, thay đổi seed, test và kết quả, điểm chưa chắc.

## Giới hạn môi trường

Sandbox không ghi được `.git`, và SqlClient trên macOS trong sandbox lỗi TLS: **không commit, không dùng Docker, không tạo hoặc sửa database SQL Server**.

Chạy `dotnet build HomestayBooking.sln -c Release` và các test liên quan (`--filter` theo lớp test desktop và service bạn sửa). Toàn bộ test, thử curl trên DesktopHost và crawl web sẽ chạy trên Mac sau.

Không đụng `docs/bao-cao/`, `HomestayBooking.Desktop/`, `deploy/`. Không thêm dòng Co-Authored-By, không ghi tên công cụ AI trong file.
