# Database — Fuji Homestay (HomestayBooking)

Dựng database SQL Server cho ứng dụng trên một máy hoặc server mới.

| File | Vai trò |
|---|---|
| `schema.sql` | Toàn bộ schema (bảng, khóa, CHECK, index) **sinh tự động từ EF Core migrations**, không sửa tay |
| `deploy.sql` | Chạy `schema.sql` với kiểm tra database đích và `XACT_ABORT` |
| `verify.sql` | Kiểm tra sau khi dựng: số bảng/khóa/index, migration đã áp, các ràng buộc chính. Chỉ đọc |
| `tests/data_consistency.sql` | Đối chiếu 14 quy tắc dữ liệu liên bảng, chỉ đọc; dùng được trên mọi database đã deploy, có LỆCH thì mã thoát khác 0 |
| `tests/constraints.sql` | Thử ràng buộc trên database **mới dựng, chưa seed** (mục 4) |
| `tests/hardening_seeded.sql` | Thử các ràng buộc của migration `DatabaseHardening` trên database **đã seed** (mục 4) |
| `tests/change_request_seeded.sql` | Thử 65 ca workflow trên database **đã seed** (mục 4), luôn ROLLBACK |
| `tests/status_consistency_seeded.sql` | Thử các ràng buộc của migration `StatusConsistency` trên database **đã seed** (mục 4) |

Nguồn chuẩn của schema là `HomestayBooking.Web/Data/Migrations` và `ApplicationDbContext`.
Thiết kế chi tiết: `fuji_homestay_database_v1.md`.

Migration `AccountingGuards` thêm 2 index trên `Payments`: `IX_Payments_BookingId` và `IX_Payments_OneRefundPerBooking` (UNIQUE một khoản hoàn mỗi đơn). Các CHECK tiền hoàn/voucher/đối soát thuộc migration `ReviewFixes`.

## 1. Yêu cầu môi trường

- **SQL Server 2022** (đã kiểm tra trên 16.0.4230.2 Developer, compatibility level 160, collation `SQL_Latin1_General_CP1_CI_AS`).
  Phiên bản khác chưa được kiểm tra.
- **sqlcmd** (`mssql-tools18`, go-sqlcmd) hoặc **SSMS** với *Query → SQLCMD Mode* bật, vì `deploy.sql` dùng `:r` và `:on error exit`.
- Để seed dữ liệu mẫu hoặc sinh lại `schema.sql`: **.NET SDK 10** và `dotnet-ef` 10.0.12
  (`dotnet tool install --global dotnet-ef --version 10.0.12`).
- Tài khoản SQL có quyền tạo database và tạo bảng.

Ví dụ SQL Server bằng Docker:

```bash
docker run -d --name sql-homestay -p 1433:1433 \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<mat-khau-manh>' \
  -v sql-homestay-data:/var/opt/mssql mcr.microsoft.com/mssql/server:2022-latest
```

**Không ghi mật khẩu vào file trong repo.** Các lệnh dưới đây đọc mật khẩu từ biến môi trường `SQLCMDPASSWORD`:

```bash
read -rs SQLCMDPASSWORD && export SQLCMDPASSWORD
```

## 2. Tạo database mới

`schema.sql` không tạo database. Tạo trước một database rỗng (đổi tên tùy ý):

```bash
sqlcmd -S localhost,1433 -U sa -C -Q "CREATE DATABASE [HomestayBooking]"
```

## 3. Chạy script

Đứng trong thư mục `database/` (vì `:r schema.sql` dùng đường dẫn tương đối):

```bash
cd database
sqlcmd -S localhost,1433 -U sa -C -d HomestayBooking -v TargetDb=HomestayBooking -i deploy.sql
```

- `-d` và `-v TargetDb` **phải là cùng một tên**. Nếu khác nhau, hoặc đích là database hệ thống, hoặc database đã có bảng không do EF tạo, script dừng trước khi tạo bất cứ thứ gì.
- `-C`: tin chứng chỉ tự ký (SQL Server trong Docker / máy cá nhân).
- **SET options:** schema có index có điều kiện (`WHERE`), SQL Server chỉ cho tạo khi `QUOTED_IDENTIFIER`, `ANSI_NULLS`, `ANSI_PADDING`, `ANSI_WARNINGS`, `ARITHABORT`, `CONCAT_NULL_YIELDS_NULL` bật và `NUMERIC_ROUNDABORT` tắt.
  `sqlcmd` mặc định **tắt** `QUOTED_IDENTIFIER` (nếu chạy thẳng `schema.sql` sẽ lỗi `Msg 1934`). `deploy.sql` tự đặt các option này ở một batch riêng trước `schema.sql` (có hiệu lực cho cả phiên, kể cả `EXEC(N'...')`) và dừng nếu chưa đúng.
  Nếu chạy `schema.sql` trực tiếp, không qua `deploy.sql`: dùng `sqlcmd -I` hoặc SSMS (mặc định đã bật).
- Mỗi migration chạy trong một transaction. Gặp lỗi thì migration đó được hủy toàn bộ và script dừng.
- Script **idempotent**: migration nào đã có trong `__EFMigrationsHistory` thì bỏ qua. Chạy lại trên database đã dựng không đổi gì; chạy trên database cũ hơn thì chỉ áp phần còn thiếu.

SQL Server trong Docker, không cài sqlcmd trên máy:

```bash
docker exec sql-homestay mkdir -p /tmp/hb
docker cp database/. sql-homestay:/tmp/hb/
docker exec -i -w /tmp/hb -e SQLCMDPASSWORD sql-homestay /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -C -d HomestayBooking -v TargetDb=HomestayBooking -i deploy.sql
```

SSMS: mở `deploy.sql`, bật *SQLCMD Mode*, sửa dòng `:r schema.sql` thành đường dẫn đầy đủ, thêm `:setvar TargetDb HomestayBooking` ở đầu, chọn đúng database rồi chạy.

Cách khác, không dùng file SQL: `dotnet ef database update --configuration Release --project HomestayBooking.Web --connection "<chuỗi kết nối>"` áp cùng các migration đó.

### Tự áp migration khi khởi động

`Database:AutoMigrate` quyết định cách ứng dụng kiểm tra schema trước khi seed:

- Cấu hình chung (`appsettings.json`): `false`. Ứng dụng chỉ đọc danh sách migration chưa áp dụng. Nếu còn migration, ứng dụng ghi log lỗi với tên từng migration và dừng khởi động; hãy triển khai bằng `deploy.sql` trước rồi khởi động lại.
- Development (`appsettings.Development.json`): cũng `false`. Profile `http` chạy Development nhưng có thể trỏ tới database đang dùng, nên không tự áp migration theo môi trường.
- Database mới cần dựng bằng `deploy.sql` ở mục 3 hoặc `dotnet ef database update --configuration Release --project HomestayBooking.Web` với cấu hình kết nối đã kiểm tra đúng đích.
- Nếu chủ động cho phép ứng dụng áp migration, chỉ bật cho phiên chạy đó, ví dụ `Database__AutoMigrate=true ASPNETCORE_ENVIRONMENT=Development dotnet run --project HomestayBooking.Web`. Không đổi mặc định để những lần chạy sau vẫn tắt tự migrate.
- Cờ này chỉ áp dụng với database quan hệ. Nhánh InMemory dùng trong test vẫn gọi `EnsureCreatedAsync`.

Khi triển khai, đặt `Database__AutoMigrate=false`, áp schema bằng quy trình bên dưới rồi khởi động ứng dụng. Cờ này chỉ chặn tự migration; việc seed dữ liệu được quyết định riêng theo môi trường.

## 4. Xác minh

```bash
sqlcmd -S localhost,1433 -U sa -C -d HomestayBooking -i verify.sql
```

Kết quả đúng: mọi dòng ở bảng đầu là `OK`, có 9 migration (cuối là `20261015200521_EnumAndReviewChecks`), mọi ràng buộc ở bảng cuối là `OK`.

| Hạng mục | Kỳ vọng |
|---|---|
| Bảng (gồm `__EFMigrationsHistory`) | 45 |
| Cột | 382 |
| Khóa chính / khóa ngoại / CHECK | 45 / 76 / 60 |
| Default (`DEFAULT` ở database) | 4 (`Bookings.DiscountAmount`, `Bookings.ChangeFeeAmount`, `SettlementItems.DiscountAmount`, `HomestayImages.SortOrder`) |
| Khóa ngoại `ON DELETE CASCADE` | 13 (các bảng con thuần túy; còn lại NO ACTION) |
| Index (không tính khóa chính) / có điều kiện | 93 / 12 |

### Đối chiếu dữ liệu nghiệp vụ (chỉ đọc)

`tests/data_consistency.sql` chỉ SELECT và báo lỗi nếu có dữ liệu lệch; không mở transaction,
không ghi database, không kiểm hoặc chuyển liên kết giấy tờ cũ. Dùng được trên v5,
DeployTest, ConstraintTest hoặc database đã deploy khác, không có guard theo tên.

```bash
sqlcmd -S localhost,1433 -U sa -C -b -d HomestayBooking_DeployTest -i tests/data_consistency.sql
```

Mỗi mục in tên tiếng Việt, số dòng lệch và `OK`/`LỆCH`, cuối có tổng 14 mục.
Mã thoát 0 khi tất cả OK; có LỆCH thì THROW để `sqlcmd -b` thoát khác 0.
Tổng là tổng lượt vi phạm; một bản ghi có thể lệch nhiều mục. Script dùng đúng trạng thái
`HoldsRoom()` (kể cả Completed), ngày Việt Nam cho hợp đồng hết hạn. Nếu đổi enum hoặc
quy tắc giữ phòng phải cập nhật script. Nên chạy khi ứng dụng dừng để hai lần đọc (in kết
quả và kiểm tra báo lỗi) ổn định. Có LỆCH thì báo lại, không tự sửa dữ liệu.

### Thử ràng buộc (tùy chọn, trước khi seed)

`tests/constraints.sql` chỉ chạy trên `HomestayBooking_ConstraintTest` (database rỗng). Ba file `tests/hardening_seeded.sql`, `tests/status_consistency_seeded.sql` và `tests/change_request_seeded.sql` chạy trên `HomestayBooking_DeployTest` (database đã seed).

`tests/constraints.sql` thử 87 ca trên SQL Server thật:
- 54 ca dữ liệu sai phải bị chặn đúng tên ràng buộc (trùng phòng cùng đêm, mỗi đơn một khoản cọc/hoàn, CHECK tiền đơn/voucher/đối soát, hợp đồng hiệu lực, ảnh bìa, FK NO ACTION; 13 whitelist enum, trả lời đánh giá, năm đối soát và quyết định hồ sơ);
- 33 ca hợp lệ phải chạy được (kể cả xóa khóa ngày thì các đêm khóa tự xóa).

```bash
cd database
sqlcmd -S localhost,1433 -U sa -C -d HomestayBooking_ConstraintTest -i tests/constraints.sql
```

- Chỉ chạy trên database tên `HomestayBooking_ConstraintTest`, đã dựng bằng `deploy.sql` và **chưa có dòng dữ liệu nào** (trước khi seed). Sai một trong các điều kiện đó thì script dừng trước khi ghi. Muốn dùng tên khác thì sửa dòng kiểm tra `DB_NAME()` ở đầu file.
- Mọi dữ liệu thử nằm trong một transaction và luôn bị ROLLBACK, kể cả khi một ca trượt hoặc có lỗi. Dòng cuối in `open_tran = 0`, `rows_left = 0`.
- Kết quả đúng: `Kết quả: 87 đạt, 0 trượt`, mã thoát 0. Có ca trượt hoặc lỗi thì mã thoát khác 0.
- ROLLBACK không trả lại bộ đếm IDENTITY, nên dữ liệu seed sau đó sẽ có Id không bắt đầu từ 1. Không ảnh hưởng ứng dụng.

### Thử ràng buộc của `DatabaseHardening` (database đã seed)

`tests/hardening_seeded.sql` thử 28 ca cho 7 CHECK của migration `DatabaseHardening` (tỷ lệ cọc, hoa hồng đơn và mục đối soát, bill tay phải có ảnh, đối soát đã gửi phải có `SentAt`, loại/trạng thái yêu cầu đổi/hủy):

```bash
cd database
sqlcmd -S localhost,1433 -U sa -C -d HomestayBooking_DeployTest -i tests/hardening_seeded.sql
```

- Chỉ chạy trên `HomestayBooking_DeployTest` đã áp `DatabaseHardening`. Không đọc hay sửa dữ liệu seed: tự tạo dữ liệu nền riêng (SĐT `+84999…`, mã đơn `HT-…`, khu vực `HardeningTest`) và dừng nếu các khóa đó đã tồn tại.
- Mọi dữ liệu thử nằm trong một transaction và luôn ROLLBACK; script so số dòng toàn database trước/sau và báo lỗi nếu lệch. Kết quả đúng: `28 đạt, 0 trượt`, `open_tran = 0`, `test_rows_left = 0`, mã thoát 0.

### Thử ràng buộc của `StatusConsistency` (database đã seed)

`tests/status_consistency_seeded.sql` thử 42 ca cho 4 CHECK của migration `StatusConsistency`: `ConfirmedAt` của đơn theo trạng thái (hủy được cả trước lẫn sau xác nhận), người gửi yêu cầu đổi/hủy (`Customer`/`Employee`), trường bắt buộc theo loại yêu cầu, và quyết định theo trạng thái yêu cầu. Giữ đủ 42 ca, cập nhật kỳ vọng theo `ChangeRequestWorkflow`: Applied thay Approved, người quyết bắt buộc, trường thừa phải dùng loại kết hợp; các ca Request có savepoint riêng để không va unique yêu cầu mở:

```bash
cd database
sqlcmd -S localhost,1433 -U sa -C -d HomestayBooking_DeployTest -i tests/status_consistency_seeded.sql
```

- Cùng điều kiện an toàn như `hardening_seeded.sql`: chỉ chạy trên `HomestayBooking_DeployTest` đã áp `StatusConsistency`, dùng khóa thử riêng (SĐT `+84998…`, mã đơn `SC-…`, khu vực `StatusConsistencyTest`), luôn ROLLBACK. Kết quả đúng: `42 đạt, 0 trượt`, `open_tran = 0`, `test_rows_left = 0`, mã thoát 0.
- Collation `SQL_Latin1_General_CP1_CI_AS` không phân biệt hoa thường nên SQL Server nhận `'customer'` (EF vẫn đọc ra `Customer`); test C# chạy trên SQLite thì chặn giá trị này.

### Thử `ChangeRequestWorkflow` (database đã seed)

`tests/change_request_seeded.sql` có **65 ca** cho 12 CHECK trong thiết kế (5 sửa, 7 thêm), 2 unique filtered và ba chủ của RoomNights. Chỉ chạy trên `HomestayBooking_DeployTest` đã áp migration mới; dùng SĐT `+84997…`, mã `WF-…`, khu vực `ChangeRequestWorkflowTest` và tài khoản `wf-test-…` riêng. Luôn ROLLBACK, so tổng số dòng trước/sau; mong đợi `65 đạt, 0 trượt`, `open_tran = 0`, `test_rows_left = 0`, exit 0.

```bash
cd database
sqlcmd -S localhost,1433 -U sa -C -b -d HomestayBooking_DeployTest -i tests/change_request_seeded.sql
```

- Enum sai đồng thời có thể vi phạm Fields/PaymentDeadline. Các ca Type/Status trong script và test SQLite dùng đúng biểu thức lấy từ metadata ở bảng thử riêng để kiểm đúng tên CHECK; các ca Fields/Deadline và dữ liệu hợp lệ vẫn kiểm trên schema đầy đủ. Không tắt CHECK.
- SQLite báo unique bằng tên cột; test C# đối chiếu thêm tên/index unique trong model và mã lỗi 2067. SQL Server kiểm trực tiếp tên index trong thông báo lỗi.
- Chuỗi chỉ có khoảng trắng: SQL Server chặn `<> ''`, SQLite cho qua; service phải kiểm chuỗi trắng. Không thêm hàm TRIM hoặc sửa CHECK Receipt.
- Trước/sau **mỗi bước ghi** trên database thử, chụp `COUNT_BIG(*)` và `CHECKSUM_AGG(BINARY_CHECKSUM(*))` của từng bảng. So cả hai, không chỉ tổng số dòng; các script test không để lại thay đổi dữ liệu, nhưng bộ đếm IDENTITY có thể tăng sau ROLLBACK.

Migration `ChangeRequestWorkflow` thêm 14 cột, 5 FK NO ACTION, 6 index (gồm 2 unique filtered); giữ nguyên index cọc ban đầu và index BookingChangeRequests.BookingId không lọc. Không tạo bảng. Up thêm cột/FK trước, gỡ CHECK cũ rồi đổi **Approved → Applied**, sau đó đặt CHECK/index mới. Ba Pending cũ giữ nguyên (người gửi/tiền NULL); khi có service/giao diện, Manager chỉ được từ chối chúng với lý do thật, không được duyệt, vẫn chiếm unique mở tới khi từ chối. Actor Owner của dòng lịch sử đóng cũng giữ nguyên.

Down đổi **Applied → Approved** và khôi phục schema/CHECK cũ. Down dừng trước khi sửa nếu đã sử dụng các cột/trạng thái/giữ phòng/cọc thêm mới để tránh mất dữ liệu không biểu diễn được ở phiên bản cũ; phải xử lý và duyệt dữ liệu chuyển tiếp trước khi hạ phiên bản. Không dùng Down như cách reset database.

Seed Development mới dùng QL001 duyệt; đủ người gửi/cột tiền, DecisionNote và RejectionReason cùng có lý do khi từ chối. Mẫu Applied không cần thêm cọc, sửa lịch/BookingNights/RoomNights ngay trên mã đơn cũ, về Deposited và xóa ConfirmedAt, có history/audit/thông báo; không ghi lại các yêu cầu đã có trong database.

## 5. Dữ liệu mẫu (seed)

`schema.sql` **chỉ có schema, không có dữ liệu**. Sau khi kiểm tra schema, `SeedData.InitializeAsync` chia dữ liệu theo `IWebHostEnvironment`:

| Phạm vi | Dữ liệu được seed |
|---|---|
| Mọi môi trường | Role; khu vực; danh mục loại phòng; tiện nghi; ngày lễ; chính sách hủy chung BR-04 |
| Chỉ Development | Nhân viên, chủ homestay, khách và tài khoản mẫu; homestay/phòng/giá/hợp đồng mẫu; đơn, thanh toán, đối soát, khóa ngày, giấy tờ, yêu cầu đổi/hủy, đánh giá, voucher và ảnh demo |

Production, Staging, Testing hoặc thiếu thông tin môi trường đều không tạo dữ liệu demo. Việc đổi môi trường không xóa tài khoản hoặc dữ liệu đã có.

- Mỗi lần khởi động, ứng dụng kiểm tra schema theo `Database:AutoMigrate` rồi seed vào database trong chuỗi kết nối `ConnectionStrings:Default`. Khi cờ tắt mà còn migration chưa áp dụng, ứng dụng dừng trước khi seed. Với database đã dựng bằng `deploy.sql`, không còn migration chờ chạy.
- Seed chạy lại không nhân đôi dữ liệu: từng nhánh kiểm tra bản ghi hoặc bảng trước khi thêm.
- Ngày của đơn mẫu tính theo **ngày chạy seed**, nên luôn có đơn sắp tới, đang ở và đã qua. Vì thế repo không kèm file dữ liệu SQL tĩnh (sẽ cũ sau vài tuần).
- Tài khoản demo và mật khẩu mặc định: xem `HomestayBooking.Web/Data/SeedData.cs` (`DefaultPassword`). Chỉ tạo ở Development.
- `Database:AutoMigrate=false` chỉ chặn tự migration; dữ liệu nền vẫn seed và Development vẫn seed demo. Không dùng cờ này như công tắc tắt toàn bộ seed.

**Việc cần làm sau:** thiết kế quy trình cấp tài khoản quản trị đầu tiên ở Production bằng thông tin bí mật riêng/cấu hình cấp phát an toàn. Không dùng `DefaultPassword` hoặc tự tạo tài khoản quản trị demo ở Production. Thay đổi này chưa cung cấp quy trình cấp tài khoản quản trị thật.

Chạy ứng dụng trỏ vào database vừa dựng. **Luôn đặt chuỗi kết nối bằng biến môi trường** (ưu tiên cao hơn `appsettings.json` và user-secrets), để không vô tình migrate/seed vào database khác:

```bash
export ConnectionStrings__Default="Server=localhost,1433;Database=HomestayBooking;User Id=sa;Password=$SQLCMDPASSWORD;TrustServerCertificate=True"
ASPNETCORE_ENVIRONMENT=Development dotnet run --project HomestayBooking.Web
```

Máy phát triển có thể lưu chuỗi kết nối bằng user-secrets thay cho biến môi trường (không lưu vào Git):

```bash
dotnet user-secrets set "ConnectionStrings:Default" "<chuỗi kết nối>" --project HomestayBooking.Web
```

`appsettings.json` chỉ chứa chuỗi mẫu với mật khẩu giả `<dat-trong-user-secrets>`.

## 6. Khi có migration mới

Sinh lại `schema.sql` từ migrations, không sửa tay:

```bash
cd HomestayBooking.Web
dotnet ef migrations has-pending-model-changes --configuration Release      # phải báo không có thay đổi
dotnet ef migrations script --configuration Release --idempotent -o ../database/schema.sql
```

Sau đó cập nhật số kỳ vọng trong `verify.sql` và bảng ở mục 4.

Nếu một bản ứng dụng đang chạy từ `bin/Debug` (ví dụ `dotnet run --no-build`), build bằng `--configuration Release` để không ghi đè file nó đang dùng.

## 7. Cập nhật database đang có dữ liệu thật

Thứ tự an toàn (ví dụ áp `DatabaseHardening` lên database đang dùng):

1. **Thử trước trên database thử nghiệm** có dữ liệu giống thật: `deploy.sql` → `verify.sql` → `tests/hardening_seeded.sql`, `tests/status_consistency_seeded.sql` và `tests/change_request_seeded.sql` → các truy vấn kiểm tra nhất quán.
2. **Kiểm tra dữ liệu thật không vi phạm** các CHECK mới bằng truy vấn SELECT (SQL Server từ chối thêm CHECK nếu có dòng vi phạm, và cả migration được hủy).
3. **Backup** database thật (`BACKUP DATABASE … WITH COPY_ONLY, CHECKSUM` rồi `RESTORE VERIFYONLY`).
4. **Áp bằng `deploy.sql`** với `-d` và `-v TargetDb` là tên database thật, rồi chạy `verify.sql`.

Khi cập nhật database thật, giữ `Database:AutoMigrate=false`: ứng dụng sẽ dừng nếu schema còn thiếu migration. Nếu bật cờ này, ứng dụng có migration mới sẽ tự áp chúng khi khởi động; chỉ bật sau khi đã quyết định áp migration và hoàn tất quy trình thử nghiệm, kiểm tra dữ liệu, backup.

### Đã triển khai `ChangeRequestWorkflow` lên v5 — 16/10/2026

- Đã áp `20261015092139_ChangeRequestWorkflow` lên `HomestayBooking_v5` bằng `deploy.sql` ngày **16/10/2026**, khi app đã tắt. Deploy chạy một lần, mã thoát `0`; `verify.sql` có **96 dòng OK**, mã thoát `0`.
- Schema sau triển khai: **8 migration, 45 bảng, 382 cột, 76 FK, 44 CHECK** đều bật và được tin cậy; **93 index ngoài khóa chính**, trong đó 12 index có điều kiện.
- Backup trước triển khai: `HomestayBooking_v5_20261016_pre-ChangeRequestWorkflow.bak`, dùng `COPY_ONLY, CHECKSUM`; đã qua `RESTORE VERIFYONLY WITH CHECKSUM`. File lưu tại `~/Backups/HomestayBooking/`, kèm file `.sha256`; SHA-256 khớp bản trong container: `9fe5e8a769229b19b2cddb010cd6d2cb2dcb292fabe90dc564c5ae5bd6fc87bc`.
- Đã restore thử sang database mới **`HomestayBooking_v5_RestoreCheck3`**, dùng MDF/LDF riêng, không `REPLACE`. `DBCC CHECKDB WITH NO_INFOMSGS` không báo lỗi; bản restore có 7 migration, cuối `StatusConsistency`, và số dòng/checksum 45 bảng khớp v5 trước triển khai. Giữ nguyên database restore này.
- Đối chiếu trước/sau deploy bằng `COUNT_BIG(*)` và `CHECKSUM_AGG(BINARY_CHECKSUM(...))` theo danh sách **368 cột cũ**: 44 bảng nghiệp vụ giữ số dòng; 43 bảng giữ checksum. `BookingChangeRequests` chỉ đổi `Status` của **Id 3: Approved → Applied**, các trường khác giữ nguyên; ba Pending **Id 1/4/5** giữ nguyên. `Bookings.ChangeFeeAmount` toàn `0`, 13 cột mới còn lại toàn `NULL`.
- `__EFMigrationsHistory` được đối chiếu riêng: giữ 7 dòng cũ và thêm đúng dòng `ChangeRequestWorkflow`, thành 8 dòng. Kết quả snapshot này được ghi nhận **trước khi người dùng bật lại app**, không dùng để khẳng định dữ liệu sau khi app seed/job đã chạy.

### `EnumAndReviewChecks` — migration thứ 9, chỉ triển khai trên database thử

Migration `20261015200521_EnumAndReviewChecks` chỉ thêm **16 CHECK**: 13 whitelist từ enum, `CK_Reviews_Reply`, `CK_Settlements_Year` (2020–2100), `CK_OwnerRegistrations_Decision`. Schema sau migration có **60 CHECK**; giữ nguyên 45 bảng, 382 cột, 76 FK và 93 index ngoài PK. Không UPDATE dữ liệu trong migration; Down chỉ gỡ đúng 16 CHECK.

- Đã SELECT thống kê từng giá trị của 13 cột trên v5 và DeployTest trước khi viết CHECK: không có giá trị ngoài enum; dữ liệu hiện có cũng thỏa ba CHECK còn lại. Không có whitelist tương đương phải bỏ qua.
- `NeedsRevision`, `Approved`, `Rejected` cần `ReviewedByEmployeeId` và `ReviewedAt`; `NeedsRevision`/`Rejected` cần `ReviewNote` có nội dung. Các trạng thái còn lại không buộc xóa dấu duyệt cũ khi gửi lại hồ sơ.
- `OwnerReply` và `RepliedAt` cùng NULL, hoặc cùng có giá trị với nội dung trả lời khác rỗng. Chuỗi trắng có khác biệt giữa collation SQL Server và SQLite; service vẫn kiểm tra chuỗi trắng.
- Triển khai/thử bằng `deploy.sql` và `verify.sql` **chỉ trên ConstraintTest và DeployTest**. `constraints.sql` giữ đủ 35 ca cũ, thêm 52 ca, tổng 87; các script seeded giữ 28/42/65 ca.
- **CHƯA áp `EnumAndReviewChecks` lên v5**. v5 đang ở 8 migration/44 CHECK sau đợt triển khai `ChangeRequestWorkflow` ngày 16/10/2026; cần quy trình duyệt, backup và kiểm tra riêng trước lần triển khai tiếp theo.
- Quyết định backend đã chốt: chủ xác nhận đối soát tùy chọn, Paid đi từ Sent hoặc Confirmed; ghi actor vào AuditLog trực tiếp ở service. Không thêm cột actor/liên kết hồ sơ, không thêm CHECK `Settlements.ConfirmedAt`; xem 10 quyết định ngày 16/10 trong tài liệu database.
