# Database — Fuji Homestay (HomestayBooking)

Dựng database SQL Server cho ứng dụng trên một máy hoặc server mới.

| File | Vai trò |
|---|---|
| `schema.sql` | Toàn bộ schema (bảng, khóa, CHECK, index) **sinh tự động từ EF Core migrations**, không sửa tay |
| `deploy.sql` | Chạy `schema.sql` với kiểm tra database đích và `XACT_ABORT` |
| `verify.sql` | Kiểm tra sau khi dựng: số bảng/khóa/index, migration đã áp, các ràng buộc chính. Chỉ đọc |
| `tests/constraints.sql` | Thử ràng buộc trên database **mới dựng, chưa seed** (mục 4) |
| `tests/hardening_seeded.sql` | Thử các ràng buộc của migration `DatabaseHardening` trên database **đã seed** (mục 4) |
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

Cách khác, không dùng file SQL: `dotnet ef database update --project HomestayBooking.Web --connection "<chuỗi kết nối>"` áp cùng các migration đó.

## 4. Xác minh

```bash
sqlcmd -S localhost,1433 -U sa -C -d HomestayBooking -i verify.sql
```

Kết quả đúng: mọi dòng ở bảng đầu là `OK`, có 7 migration (cuối là `20261013032617_StatusConsistency`), mọi ràng buộc ở bảng cuối là `OK`.

| Hạng mục | Kỳ vọng |
|---|---|
| Bảng (gồm `__EFMigrationsHistory`) | 45 |
| Cột | 368 |
| Khóa chính / khóa ngoại / CHECK | 45 / 71 / 37 |
| Default (`DEFAULT` ở database) | 3 (`Bookings.DiscountAmount`, `SettlementItems.DiscountAmount`, `HomestayImages.SortOrder`) |
| Khóa ngoại `ON DELETE CASCADE` | 13 (các bảng con thuần túy; còn lại NO ACTION) |
| Index (không tính khóa chính) / có điều kiện | 87 / 10 |

### Thử ràng buộc (tùy chọn, trước khi seed)

`tests/constraints.sql` chỉ chạy trên `HomestayBooking_ConstraintTest` (database rỗng). Hai file `tests/hardening_seeded.sql` và `tests/status_consistency_seeded.sql` chạy trên `HomestayBooking_DeployTest` (database đã seed).

`tests/constraints.sql` thử 35 ca trên SQL Server thật:
- 26 ca dữ liệu sai phải bị chặn đúng tên ràng buộc (trùng phòng cùng đêm, mỗi đơn một khoản cọc/hoàn, CHECK tiền đơn/voucher/đối soát, hợp đồng hiệu lực, ảnh bìa, FK NO ACTION);
- 9 ca hợp lệ phải chạy được (kể cả xóa khóa ngày thì các đêm khóa tự xóa).

```bash
cd database
sqlcmd -S localhost,1433 -U sa -C -d HomestayBooking_ConstraintTest -i tests/constraints.sql
```

- Chỉ chạy trên database tên `HomestayBooking_ConstraintTest`, đã dựng bằng `deploy.sql` và **chưa có dòng dữ liệu nào** (trước khi seed). Sai một trong các điều kiện đó thì script dừng trước khi ghi. Muốn dùng tên khác thì sửa dòng kiểm tra `DB_NAME()` ở đầu file.
- Mọi dữ liệu thử nằm trong một transaction và luôn bị ROLLBACK, kể cả khi một ca trượt hoặc có lỗi. Dòng cuối in `open_tran = 0`, `rows_left = 0`.
- Kết quả đúng: `Kết quả: 35 đạt, 0 trượt`, mã thoát 0. Có ca trượt hoặc lỗi thì mã thoát khác 0.
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

`tests/status_consistency_seeded.sql` thử 42 ca cho 4 CHECK của migration `StatusConsistency`: `ConfirmedAt` của đơn theo trạng thái (hủy được cả trước lẫn sau xác nhận), người gửi yêu cầu đổi/hủy (`Customer`/`Employee`), trường bắt buộc theo loại yêu cầu, và `DecidedAt` theo trạng thái yêu cầu:

```bash
cd database
sqlcmd -S localhost,1433 -U sa -C -d HomestayBooking_DeployTest -i tests/status_consistency_seeded.sql
```

- Cùng điều kiện an toàn như `hardening_seeded.sql`: chỉ chạy trên `HomestayBooking_DeployTest` đã áp `StatusConsistency`, dùng khóa thử riêng (SĐT `+84998…`, mã đơn `SC-…`, khu vực `StatusConsistencyTest`), luôn ROLLBACK. Kết quả đúng: `42 đạt, 0 trượt`, `open_tran = 0`, `test_rows_left = 0`, mã thoát 0.
- Collation `SQL_Latin1_General_CP1_CI_AS` không phân biệt hoa thường nên SQL Server nhận `'customer'` (EF vẫn đọc ra `Customer`); test C# chạy trên SQLite thì chặn giá trị này.

## 5. Dữ liệu mẫu (seed)

`schema.sql` **chỉ có schema, không có dữ liệu**. Dữ liệu mẫu (khu vực, homestay, phòng, giá, tài khoản demo, đơn đặt phòng ở mọi trạng thái, đánh giá…) do ứng dụng tạo ở lần chạy đầu (`SeedData.InitializeAsync`):

- Mỗi lần khởi động, ứng dụng **tự chạy migration rồi seed** vào database trong chuỗi kết nối `ConnectionStrings:Default`. Với database đã dựng bằng `deploy.sql`, bước migration không làm gì.
- Seed chạy lặp an toàn: bảng nào đã có dữ liệu thì bỏ qua.
- Ngày của đơn mẫu tính theo **ngày chạy seed**, nên luôn có đơn sắp tới, đang ở và đã qua. Vì thế repo không kèm file dữ liệu SQL tĩnh (sẽ cũ sau vài tuần).
- Tài khoản demo và mật khẩu mặc định: xem `HomestayBooking.Web/Data/SeedData.cs` (`DefaultPassword`). Chỉ dùng để demo.

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
dotnet ef migrations has-pending-model-changes      # phải báo không có thay đổi
dotnet ef migrations script --idempotent -o ../database/schema.sql
```

Sau đó cập nhật số kỳ vọng trong `verify.sql` và bảng ở mục 4.

Nếu một bản ứng dụng đang chạy từ `bin/Debug` (ví dụ `dotnet run --no-build`), build bằng `--configuration Release` để không ghi đè file nó đang dùng.

## 7. Cập nhật database đang có dữ liệu thật

Thứ tự an toàn (ví dụ áp `DatabaseHardening` lên database đang dùng):

1. **Thử trước trên database thử nghiệm** có dữ liệu giống thật: `deploy.sql` → `verify.sql` → `tests/hardening_seeded.sql` và `tests/status_consistency_seeded.sql` → các truy vấn kiểm tra nhất quán.
2. **Kiểm tra dữ liệu thật không vi phạm** các CHECK mới bằng truy vấn SELECT (SQL Server từ chối thêm CHECK nếu có dòng vi phạm, và cả migration được hủy).
3. **Backup** database thật (`BACKUP DATABASE … WITH COPY_ONLY, CHECKSUM` rồi `RESTORE VERIFYONLY`).
4. **Áp bằng `deploy.sql`** với `-d` và `-v TargetDb` là tên database thật, rồi chạy `verify.sql`.

Lưu ý: ứng dụng tự gọi `MigrateAsync` mỗi lần khởi động. Một bản ứng dụng **đã build có migration mới** mà khởi động với chuỗi kết nối tới database thật sẽ tự áp migration đó, bỏ qua các bước trên. Chỉ build/khởi động lại ứng dụng trỏ vào database thật sau khi đã quyết định áp migration.
