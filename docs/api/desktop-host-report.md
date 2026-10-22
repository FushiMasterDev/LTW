# Báo cáo host desktop SQLite độc lập

Ngày kiểm tra: 22/10/2026. Nhánh `feature/desktop-api-owner`. Không commit; giữ thay đổi API chủ đã có trước đợt này, không sửa `docs/bao-cao/`, không chạy Docker/SQL Server.

## Cấu trúc và vòng đời

`HomestayBooking.DesktopHost` là executable ASP.NET Core .NET 10, tham chiếu Web qua ProjectReference. Không sao chép controller, DTO, nghiệp vụ, Identity hoặc JWT. `AddHomestayCore` chứa nguyên khối đăng ký từ Web, giữ thứ tự và giá trị; Web tiếp tục chọn SQL Server và job cũ, host chọn SQLite và job nội bộ riêng.

Application part của Web đi qua `DesktopControllerFeatureProvider`, chỉ giữ namespace `HomestayBooking.Web.Controllers.Api.Desktop` và namespace con. Host chỉ MapControllers, không conventional routes, MVC areas, static assets, static files, session, webhook, health check hay HTTPS redirect. Swagger chỉ nhóm desktop và dùng lại DesktopSwaggerFilter. Mọi route không khớp trả 404 ProblemDetails. Exception trong action dùng DesktopExceptionFilter của Web; filter riêng nhận diện unique constraint SQLite (1555/2067) thành cùng 409 ProblemDetails; lỗi pipeline trả 500 chung, không lộ exception.

Mặc định `http://localhost:5080`, Development. URL, môi trường, database path và các options nghiệp vụ/JWT có thể cấu hình. Cấu hình được đọc lúc khởi động; tắt file watcher của cấu hình mặc định để chạy ổn định trong sandbox và không cần theo dõi thay đổi file khi offline. Không dùng SQL Server connection string, user-secrets hay middleware của Web.

`DesktopDbContext` kế thừa ApplicationDbContext, chỉ đổi convention decimal → double. Test SQLite cũ cũng dùng convention này qua lớp SqliteTestDbContext. Tạo schema bằng EnsureCreated, bật foreign keys, timeout khóa mặc định 30 giây. Marker schema version 1 trong bảng `__DesktopHost` cùng file database quyết định seed một lần. Không giả lập lịch sử migration SQL Server. Dataset và marker seed commit cùng transaction; seed nội bộ tái sử dụng transaction ngoài. Restart không tạo lại dữ liệu đã xóa.

Seed gọi lại toàn bộ dữ liệu Development của Web, kể cả roles/Identity, danh mục, nhân sự, chủ/khách, 35 homestay, phòng/giá/hợp đồng, 60 đơn, bill, yêu cầu đổi, đánh giá, khóa phòng, giấy tờ và vận hành/đối soát. Ảnh mẫu được đóng gói để nhánh seed ảnh chạy đủ. Hai PNG Data/SampleFiles đi theo ProjectReference. File receipt/document ở App_Data/receipts; ảnh upload dưới App_Data/public/uploads, không được xuất bản qua static middleware. URL ảnh trong DTO vẫn giống Web nên client cần ảnh thay thế; bill/giấy tờ đọc được qua endpoint desktop có kiểm quyền.

Đường dẫn mặc định dựa trên AppContext.BaseDirectory; Path.Combine/GetFullPath dùng được trên Windows. `--contentRoot` hỗ trợ thư mục runtime riêng (test dùng thư mục tạm). `--reset` xóa/tạo lại database đã chọn, giữ upload/keys. Hướng dẫn chạy/publish/reset/sao lưu/LAN ở [README host](../../HomestayBooking.DesktopHost/README.md).

## File Web sửa trong đợt host

| File | Lý do và ảnh hưởng |
|---|---|
| Program.cs | Thay đúng khối đăng ký bằng AddHomestayCore, vẫn UseSqlServer. Đối chiếu với bản đầu phiên cho thấy toàn bộ phần còn lại giữ nguyên, gồm middleware, options cookie, MVC, Swagger và bootstrap. |
| Services/HomestayCoreRegistration.cs (mới) | Chuyển nguyên đăng ký DbContext/Identity/policy/JWT/options/service; tham số chọn provider và bật job cũ, mặc định bật như Web. Giữ cả ba service API chủ chưa commit. |
| Data/SeedData.cs | Tách phần nạp dữ liệu khỏi quản lý schema, thêm SeedDevelopmentDataAsync cho host trong scope/transaction của bên gọi. InitializeAsync của Web giữ kiểm migration và chỉ demo ở Development. |
| Data/SeedData.Images.cs | Guard môi trường nằm tại SeedAsync; helper ảnh vẫn kiểm IWebHostEnvironment và sự tồn tại asset. Web ngoài Development vẫn không gọi nhánh demo. |
| Data/SeedData.Operations.cs | Chỉ mở transaction riêng nếu chưa có transaction; cần để seed host và marker atomic. Web không có transaction ngoài nên hành vi cũ giữ nguyên. |

Các file Web khác đang modified/untracked trong git status là công việc API chủ có sẵn, không chỉnh trong đợt này. Ngoài Web: thêm project host vào solution; test reference host, SqliteTestDbContext dùng lại DesktopDbContext, thêm DesktopStandaloneHostTests; thêm ignore App_Data của host; cập nhật README chính và mục 8 tài liệu backend.

## Khác biệt SQLite so với SQL Server

| Khía cạnh | Hành vi/giới hạn |
|---|---|
| Tiền, tỷ lệ | decimal chuyển sang REAL/double để CHECK, sort, aggregate theo số thay vì TEXT. Có sai số nhị phân, khác SQL Server decimal chính xác; các phép cộng nhiều dòng hoặc CHECK đẳng thức có thể nhạy với sai số. DTO vẫn decimal và nghiệp vụ làm tròn dùng service cũ. Chỉ thích hợp demo/offline nếu chưa đánh giá nghiệp vụ tiền thật. |
| Đồng thời | SQLite cho một writer, khóa theo database; SQL Server có khóa dòng và UPDLOCK/HOLDLOCK riêng. Nhánh SQL Server lock trong BookingService không chạy trên SQLite. Transaction, unique RoomNights, kiểm status/concurrency token vẫn có. Tải ghi lớn có thể timeout/busy; chưa benchmark nhiều client/process cùng lúc. |
| Collation/tìm kiếm | Không áp collation SQL Server lên SQLite. Equality/unique mặc định BINARY phân biệt hoa thường, không bỏ dấu; LIKE/Contains tùy phép dịch SQLite, không đảm bảo giống collation SQL Server. Login nhân viên chuẩn hóa qua service, SĐT chuẩn hóa E.164 và claim Identity dùng chung. |
| Kiểu/constraint | Dùng model EF hiện tại, gồm FK, CHECK và partial unique indexes. SQLite không có enforcement độ dài/precision giống SQL Server; validation API/service vẫn chạy. Unique constraint được nhận diện bằng mã SQLite trong filter riêng của host, không sửa xử lý SQL Server của Web. |
| Thời gian/báo cáo | DateOnly/TimeOnly và timestamp SQLite khác kiểu cột SQL Server; DTO giữ ngày yyyy-MM-dd, timestamp UTC Z. ReportService đã có nhánh tránh DateDiffSecond trên SQLite, được gọi qua cả 5 endpoint báo cáo. |
| Nâng schema | EnsureCreated, marker version 1; không chạy 9 migration SQL Server. Chưa có migration SQLite cho nâng cấp model tương lai. Sao lưu trước khi chuyển phiên bản; reset chỉ dùng khi chấp nhận mất dữ liệu. |
| File database | Một database logic nhưng SQLite có thể dùng thêm -wal/-shm khi đang mở. Không chép riêng file db khi đang ghi; sao lưu sau khi dừng host và kèm App_Data. |

## Job nền

DesktopMaintenanceJob chạy ngay khi host start và mỗi phút, từng tác vụ có scope/try-catch riêng, gọi lại service Web:

- Hết hạn giữ chỗ và giải phóng RoomNights.
- Hết hạn yêu cầu đổi chưa nộp cọc bổ sung.
- Cảnh báo quá hạn xác nhận, bill chậm duyệt, hồ sơ chủ quá hạn bằng notification nội bộ.
- Hết hạn voucher và hợp đồng.

Không đăng ký BookingTimeoutJob cũ ở host. Bỏ gửi nhắc khách/chủ, tự lập đối soát và từ chối khoản bù treo; không kết nối ngân hàng, gửi SMS/email hay gọi dịch vụ ngoài. Đối soát mẫu vẫn đọc/xác nhận qua API chủ. Web vẫn chạy đầy đủ BookingTimeoutJob như trước.

## Kiểm chứng

Lệnh chạy toàn bộ suite không cần SQL Server (hai lớp bị loại đúng theo giới hạn môi trường):

```bash
DOTNET_PROCESSOR_COUNT=4 DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false DOTNET_USE_POLLING_FILE_WATCHER=1 \
  dotnet test HomestayBooking.sln --disable-build-servers -m:1 \
  -p:UseSharedCompilation=false -p:NuGetAudit=false \
  --filter 'FullyQualifiedName!~ReportServiceSqlServerTests&FullyQualifiedName!~AvailabilityPerformanceTests' \
  --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=desktop-host-final.trx' \
  --results-directory /tmp/desktop-host-results
```

| Kiểm tra | Kết quả thực tế |
|---|---|
| Toàn bộ suite ở trên | **1.899 pass, 0 fail, 0 skip**, thời gian thực thi test 4 phút 59 giây; bao gồm 1.897 ca cũ và 2 ca host mới. |
| DesktopStandaloneHostTests chạy riêng sau sửa fixture | **2 pass, 0 fail, 0 skip**, 39 giây. |
| `dotnet build HomestayBooking.sln -c Release` (thêm tùy chọn tắt build server/shared compilation và NuGet audit) | Thành công, **0 warning, 0 error**, exit 0. |
| Publish self-contained `win-x64` | Thành công, exit 0; thư mục `/tmp/fuji-desktop-win-x64`, khoảng 141 MB. Executable và `e_sqlite3.dll` được kiểm tra là PE32+ x86-64 cho Windows; có Data/SampleFiles và toàn bộ ảnh demo. |
| Chạy thật bằng `dotnet run --project HomestayBooking.DesktopHost --no-build --no-restore -- --urls http://127.0.0.1:5084 --DesktopHost:DatabasePath /tmp/fuji-desktop-smoke/final.db` | Host Kestrel hoạt động với SQLite file tạm, không cần biến môi trường sửa file watcher. |
| curl host thật | Login NV001, QL001, KT001, chủ 0912000001; me/areas/receipts/dashboard tương ứng đều 200. Token chéo nhóm trả 403. `/`, `/Account/Login`, `/api/homestays` trả 404 JSON ProblemDetails. |
| Swagger host thật | 200, đúng **43 thao tác nhân viên + 42 thao tác chủ**, chỉ đường dẫn `/api/desktop/...`. |
| Chạy riêng Production không SigningKey | Swagger 200, login 503 ProblemDetails, không bị lỗi khởi động. |
| Web Program so với bản chưa commit đầu phiên | Đối chiếu bằng script: chỉ thay khối đăng ký bằng extension; toàn bộ phần còn lại giữ nguyên. Nội dung/thứ tự đăng ký trong extension cũng được đối chiếu. |
| `git diff --check`, kiểm tra whitespace file mới | Không có lỗi. |

Hai ca host chạy Kestrel thật với SQLite file và storage trong thư mục tạm, không thay provider bằng InMemory, không fake authentication. Bao phủ đăng nhập NV001/QL001/KT001/AD001 và chủ; không có Set-Cookie; đủ 85 Swagger operations; route ngoài desktop/static/private path 404; anonymous 401, token chéo nhóm/chức vụ 403; đọc danh mục/đơn/khách/phòng trống, cả năm báo cáo; quản lý thêm khu vực/xóa ngày lễ; nhân viên tạo đơn và gửi lại submissionId; kế toán đọc file/duyệt bill; chủ đọc workspace, xác nhận đơn, khóa/xung đột/mở phòng và upload ảnh. Bước chuẩn bị bill gọi ReceiptFileStore và ReceiptReviewService thật vì desktop không có endpoint khách nộp bill; phần duyệt đi qua HTTP như WinForms.

Restart kiểm đơn vẫn Confirmed, khu vực/đêm khóa/file vẫn còn, số đơn không tăng, ngày lễ seed đã xóa không bị tạo lại. `--reset` trả về đúng dataset gốc. Khóa cấu hình giữ token qua restart, khóa ngẫu nhiên làm token cũ nhận 401. Hai login dùng chung rate limit IP, vượt giới hạn nhận 429/Retry-After. Ca job tự tạo đơn qua API, đặt HoldExpiresAt quá hạn, gọi một lượt toàn bộ tác vụ desktop, kiểm Expired và RoomNights được giải phóng, không có log Error của job.

Lượt đầu bị treo tại file watcher của WebApplication.CreateBuilder trong sandbox; host đã tắt reload cấu hình mặc định, suite Web chạy với các biến môi trường chỉ cho tiến trình test như báo cáo đợt trước. Lượt bổ sung job phát hiện fixture giả định seed còn đơn AwaitingPayment, trong khi job startup đã hết hạn chúng; sửa fixture tự tạo đơn mới, không sửa nghiệp vụ hoặc test cũ để cho qua.

NuGet HTTPS qua .NET trong sandbox ban đầu lỗi chứng chỉ khi tải runtime pack Windows. Đã tải ba nupkg chính thức phiên bản 10.0.11 từ `https://api.nuget.org/v3-flatcontainer/` bằng curl kiểm TLS, dùng feed/cache trong `/tmp` và cache package có sẵn làm fallback rồi publish lại thành công. Không đổi NuGet config/chứng chỉ của máy và không lưu package/khóa thật vào repo. Restore có dòng chẩn đoán macOS CSSM, nhưng build chốt vẫn 0 warning/error. Runtime Windows đi theo SDK đã cài; EF Core SQLite là 10.0.12 như bộ test.

Bằng chứng của phiên: `/tmp/desktop-host-results/desktop-host-final.trx`, `/tmp/desktop-host-final-tests.log`, `/tmp/desktop-host-focused-tests.log`, `/tmp/desktop-host-final-build.log`, `/tmp/desktop-host-final-publish.log`, `/tmp/desktop-host-final-run.log`. Host thử đã được dừng sau kiểm tra.

## Điểm chưa chắc

Không có máy Windows trong phiên này: cross-publish và kiểm định định dạng PE/native DLL không thay thế việc chạy executable trực tiếp trên Windows. Chưa đánh giá tải đồng thời lớn, nâng cấp dữ liệu qua nhiều phiên bản schema hoặc độ chính xác số tiền rất lớn. Hai lớp SQL Server thật bị loại theo yêu cầu không đụng SQL Server; không tuyên bố đã chạy chúng. Ảnh public không có đường tải qua host vì yêu cầu chỉ phục vụ API desktop và không static files.
