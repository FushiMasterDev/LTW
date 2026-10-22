# Báo cáo Web API desktop cho chủ homestay — 22/10/2026

Nhánh làm việc: `feature/desktop-api-owner`, giữ nguyên nhánh, không commit/push/merge. Phạm vi: 42 endpoint mới dưới `/api/desktop/owner`, dùng lại nghiệp vụ khu `/Owner`; 43 endpoint desktop nhân viên và 10 endpoint API cũ giữ hợp đồng. Bảng đủ 42 endpoint, request, quyền, lỗi và curl mẫu nằm ở [mục 8](../api-va-backend.md#8-api-cho-ứng-dụng-desktop); README mục 5 đã bổ sung nhóm chủ.

## Endpoint và service

| Nhóm | Số endpoint | Controller mới | Service |
|---|---:|---|---|
| Đăng nhập SĐT, me | 2 | DesktopOwnerAuthController | AccountLookupService.FindForPhoneLoginAsync, SignInManager, claims factory, ApprovedOwner |
| Tổng quan, lịch nhận/trả, đơn và lịch phòng/khóa ngày | 12 | DesktopOwnerWorkspaceController | OwnerDashboardService, OwnerBookingQueryService, OwnerScheduleService, BookingService, RoomBlockService |
| Danh sách/editor/khu vực, nội dung/vị trí/tiện nghi, loại phòng/phòng/giá, ảnh, chính sách, giấy tờ | 21 | DesktopOwnerHomestaysController | HomestayContentService, AvailabilityService, RoomService, HomestayImageService, HomestayPolicyService, HomestayDocumentService |
| Đánh giá/trả lời, đối soát/chi tiết/xác nhận/sai lệch, file giấy tờ | 7 | DesktopOwnerFinanceController | ReviewService, SettlementService, OwnerRegistrationQueryService |

Không thêm tạo/xóa homestay, xóa loại phòng/phòng hoặc ghi danh mục chung vì web Owner không cung cấp các quyền đó. Các thao tác của web đều có API tương ứng, gồm check-in, complete, no-show, thay phiên bản chính sách, chọn bìa, bật/tắt bán và giấy tờ lịch sử. API không mở thêm thao tác kế toán gửi/mở lại/chi trả đối soát cho chủ.

## Bảo mật và hợp đồng

- `DesktopApiController` giữ thuộc tính HTTP dùng chung: ApiController, Bearer-only, IgnoreAntiforgeryToken, DesktopExceptionFilter và schema lỗi. `DesktopController` vẫn có policy Employee; base mới `DesktopOwnerController` dùng ApprovedOwner. Cookie Identity vẫn là scheme mặc định của web/API cũ.
- Login chủ dùng SĐT chuẩn hóa, CheckPasswordSignInAsync với lockoutOnFailure, đếm AccessFailed khi IsNotAllowed như AccountController. Chỉ Owner đã duyệt, tài khoản Active và không có hồ sơ/vai trò nhân viên mới nhận token. Mọi thất bại thông tin/điều kiện tài khoản trả cùng 401 như web. Dùng chung DesktopLogin, khóa/issuer/audience/TokenMinutes; không tạo cookie.
- OnTokenValidated kiểm trạng thái/stamp/lockout, dựng lại claim từ factory từng request. Nhánh token nhân viên vẫn kiểm Employee đang làm việc; nhánh token chủ kiểm Owner đã duyệt và hồ sơ chủ hợp lệ. Giữ nhóm vai trò của token gốc để không tái sử dụng token nhân viên thành token chủ sau khi đổi vai trò. Hủy duyệt/gỡ vai trò/đổi stamp/khóa có hiệu lực ngay.
- OwnerResourceService kiểm phạm vi bằng OwnerHomestaysAsync trước nghiệp vụ: home/type/room/image/document/review/settlement/block; kiểm cả quan hệ cha–con trong URL. Đơn lọc đúng PersonId từ claim và DepositedAt khác null như web. Đối soát nháp không hiện. Tài nguyên của chủ khác/mất bản ghi đều 404; service nghiệp vụ vẫn tự kiểm quyền lại.
- DTO riêng ở DesktopOwnerDtos.cs, không trả entity hoặc đường dẫn lưu file thật. Enum request JSON dùng converter trên DTO; enum response là chuỗi. Không đổi JsonOptions/CORS. Ngày yyyy-MM-dd, timestamp chuẩn UTC, TimeOnly HH:mm:ss. Danh sách độc lập dùng DesktopPage; mảng con editor/chi tiết/lưới lịch là dữ liệu của tài liệu.
- Upload multipart gọi nguyên HomestayImageService/HomestayDocumentService và ReceiptFileStore: kiểm tên, MIME, chữ ký/cấu trúc và 1 byte–5MB; ảnh nhận JPG/PNG/WEBP, giấy tờ thêm PDF. File riêng tư chỉ qua OpenDocumentAsync sau chốt sở hữu; không phục vụ từ wwwroot. Thay giấy tờ pháp lý/vị trí vẫn chuyển chờ duyệt và gửi thông báo như web.
- Bổ sung khóa lỗi không tìm thấy cho phòng/khóa/ảnh/giấy tờ/đánh giá/đối soát/file thiếu vào DesktopExceptionFilter. Trùng lịch khóa trả 409 ProblemDetails có conflicts; concurrency/unique giữ 409. Filter chỉ ở nhóm desktop.
- Swagger Development giữ 10 endpoint cũ, 43 endpoint nhân viên và thêm 42 endpoint chủ với tag Desktop Owner. Hai login không gắn Bearer security requirement. Mọi action mới có XML summary/response và ProducesResponseType; các lỗi chung kế thừa từ base.

## Logic chuyển khỏi MVC và giữ hành vi web

| File web | Logic dùng chung mới | Hành vi giữ lại |
|---|---|---|
| Areas/Owner/Controllers/BookingsController.cs | Services/OwnerBookingQueryService.cs: OwnBookings, tìm theo trạng thái/tên/mã/SĐT, thứ tự ưu tiên, phân trang, chi tiết/nights/histories/phòng trống/change history, lấy đơn cho thao tác | Chỉ đơn đã cọc của mình; arrivals vẫn redirect Schedule; ViewData, view model, TempData, localization, xử lý concurrency và redirect cũ. Constructor cũ vẫn gọi được nhờ tham số service tùy chọn, không sửa test nghiệp vụ cũ |
| Areas/Owner/Controllers/DashboardController.cs | Services/OwnerDashboardService.cs: hồ sơ gần nhất, homestay/số phòng, summary của chủ đã duyệt | Web vẫn cho chủ chưa duyệt xem trạng thái hồ sơ, không có số đếm/shortcut quản lý; chủ không có hồ sơ vẫn Forbid; view model không đổi |

Các controller MVC Owner khác, view/CSS/JS, AccountController, AppClaimsPrincipalFactory, AppSignInManager, policy web và controller API cookie cũ không sửa. BookingService/RoomService/ReviewService/SettlementService và các service upload hiện có không sửa nghiệp vụ. Các kiểm tra ngày, chuyển trạng thái, hoàn cọc, giữ phòng bán cuối cùng, snapshot giá/chính sách/đối soát, audit, thông báo và transaction chạy bằng service gốc.

## File thay đổi

- Thêm `Controllers/Api/Desktop/Owner/{DesktopOwnerController,DesktopOwnerAuthController,DesktopOwnerWorkspaceController,DesktopOwnerHomestaysController,DesktopOwnerFinanceController}.cs`.
- Thêm `DesktopApi/DesktopOwnerDtos.cs`, `Services/{OwnerBookingQueryService,OwnerDashboardService,OwnerResourceService}.cs`.
- Sửa hai controller MVC nói trên, `Controllers/Api/Desktop/DesktopController.cs`, `DesktopApi/{DesktopApiRegistration,DesktopExceptionFilter,DesktopSwaggerFilter}.cs`, `Program.cs` (đăng ký ba service và mô tả Swagger).
- Thêm `HomestayBooking.Tests/Integration/DesktopOwnerApiTests.cs` và `DesktopOwnerRelationalTests.cs`; cập nhật kiểm tra Swagger trong DesktopApiConfigurationTests để kiểm độc lập 10 cũ + 43 nhân viên + 42 chủ. Các assertion/test đợt 1 khác giữ nguyên.
- Cập nhật `README.md`, `docs/api-va-backend.md`, báo cáo này. Không sửa/stage `docs/bao-cao/` hoặc prompt người dùng.

## Kiểm thử và kết quả

Mốc trước sửa: lệnh lọc desktop/owner và các service liên quan chạy **531 pass, 0 fail, 0 skip**, khoảng 1 phút 11 giây.

Bộ test mới có **31 ca**: 30 ca DesktopOwnerApiTests và một ca DesktopOwnerRelationalTests. Bao phủ login đúng/sai/khách/nhân viên/chưa duyệt/khóa/không Active, đếm sai mật khẩu, limiter/Production thiếu khóa, claim/issuer/audience/thời hạn, cookie-only 401, token chéo vai trò 403, thu hồi trạng thái/stamp/lockout/duyệt/vai trò; đọc/lọc/phân trang; IDOR từng nhóm và ID con sai cha; từng thao tác ghi có thành công và lỗi; upload hợp lệ/sai loại/MIME/quá cỡ/thiếu file; file riêng tư chủ khác/anonymous/mất file; không lộ đối soát nháp. Kiểm cả trạng thái DB, audit, số đêm khóa, snapshot và nội dung file sau thao tác.

Ca SQLite trong bộ nhớ kiểm truy vấn mới thực thi trên provider quan hệ, transaction khóa/mở ngày, xung đột đêm, cascade xóa đêm, ExecuteUpdate khóa homestay khi xác nhận đối soát và truy vấn quyền giấy tờ. Không mock service nghiệp vụ.

Các lượt đầu phát hiện fixture mới chọn homestay seed không có ảnh, fixture đối soát thiếu SentAt theo CHECK constraint SQLite và helper test cố đọc trang HTML 404 của web thành JSON. Đã sửa dữ liệu/cách kiểm tra trong test mới; không thay seed, schema hoặc hành vi web để cho qua.

Môi trường theo dõi file của test host bị kẹt trong WebApplication.CreateBuilder, dẫn đến HostFactoryResolver timeout. Chạy lại với các biến môi trường chỉ cho tiến trình test:

```bash
DOTNET_USE_POLLING_FILE_WATCHER=1 \
DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false \
DOTNET_PROCESSOR_COUNT=4 \
dotnet test HomestayBooking.sln -c Release --no-build --no-restore \
  --filter 'FullyQualifiedName~DesktopApi|FullyQualifiedName~DesktopOwner|FullyQualifiedName~Owner|FullyQualifiedName~HomestayEditor|FullyQualifiedName~HomestayContent|FullyQualifiedName~RoomBlock|FullyQualifiedName~RoomManagement|FullyQualifiedName~ReviewService|FullyQualifiedName~SettlementService' \
  --results-directory /private/tmp/desktop-owner-tests \
  --logger 'trx;LogFileName=owner-final.trx' --logger 'console;verbosity=minimal'
```

Biến môi trường không thay cấu hình ứng dụng trong repo; mã chẩn đoán tạm đã gỡ.

Kết quả chốt:

| Kiểm tra | Kết quả |
|---|---|
| `dotnet build HomestayBooking.sln -c Release` | Thành công, 0 warning, 0 error; restore up-to-date |
| Lệnh lọc ở trên, `owner-final.trx` | **562 pass, 0 fail, 0 skip**, khoảng 1 phút 6 giây |
| Phân tách từ TRX | **531 test cũ pass + 31 test mới pass** |
| Swagger | **10 API cũ + 43 endpoint nhân viên + 42 endpoint chủ**, đúng tag/security |
| `git diff --check` và kiểm whitespace các file mới | Không có lỗi |

Lượt build cuối thoát 0 sau khoảng 22,62 giây; restore có dòng chẩn đoán macOS `CSSM_ModuleLoad(): CSSMERR_DL_MDS_ERROR`, nhưng compiler vẫn báo 0 warning, 0 error.

Kết quả TRX ở `/private/tmp/desktop-owner-tests/owner-final.trx`. Lượt chạy riêng ca IDOR sau sửa fixture cũng pass (`owner-idor.trx`). Không sửa test đợt 1 để cho qua; chỉ cập nhật phạm vi đếm Swagger theo yêu cầu, vẫn kiểm riêng đủ 43 endpoint nhân viên và 10 API cũ.

## Giới hạn và điểm chưa chắc

- Theo giới hạn prompt, không chạy toàn bộ suite có SQL Server, không tạo/sửa database SQL Server, không dùng Docker, không chạy app/curl/crawl trên database thật. Toàn bộ test trong phiên dùng InMemory/SQLite trong bộ nhớ và file upload tạm riêng. Cần chạy toàn bộ test, curl và crawl trên Mac ngoài sandbox ở lượt tiếp theo.
- Chưa kiểm tranh chấp đồng thời thật trên SQL Server. Ca trùng lịch quan hệ được thực thi trên SQLite; các transaction/locking nghiệp vụ giữ nguyên service hiện có.
- Editor trả toàn bộ dữ liệu con như màn web; không thêm phân trang riêng cho mỗi bảng con. Không thêm refresh token, retry tự động, quy trình duyệt mới hoặc dữ liệu ngoài quyền web Owner.
- Không có yêu cầu nghiệp vụ cần quyết định thêm. Không commit, không đổi nhánh và không có thao tác triển khai trong phiên này.
