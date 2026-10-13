-- Chạy schema.sql lên một database đã tạo sẵn, có chặn nhầm database đích.
-- Dùng sqlcmd (hoặc SSMS bật SQLCMD Mode), đứng trong thư mục database/:
--   sqlcmd -S <server> -U <user> -C -d <TenDb> -v TargetDb=<TenDb> -i deploy.sql
-- TargetDb phải trùng database đang kết nối, nếu không script dừng trước khi tạo bất cứ thứ gì.
:on error exit
SET NOCOUNT ON;
-- schema.sql (EF sinh) không tự bật: lỗi giữa chừng phải hủy cả migration đang chạy
SET XACT_ABORT ON;
-- Bắt buộc khi tạo index có điều kiện (WHERE). sqlcmd mặc định QUOTED_IDENTIFIER OFF → Msg 1934.
-- Đặt ở batch riêng trước mọi batch của schema.sql: QUOTED_IDENTIFIER có hiệu lực từ batch sau cho cả phiên
-- (kể cả EXEC(N'...') bên trong), nên áp cho toàn bộ schema.sql.
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF SESSIONPROPERTY('QUOTED_IDENTIFIER') <> 1 OR SESSIONPROPERTY('ANSI_NULLS') <> 1
    THROW 50002, N'SET options chưa đúng (cần QUOTED_IDENTIFIER và ANSI_NULLS ON) - dừng.', 1;
IF DB_NAME() <> N'$(TargetDb)' OR DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
    THROW 50000, N'Database đang kết nối khác TargetDb (hoặc là database hệ thống) - dừng, không thay đổi gì.', 1;
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
   AND EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
    THROW 50001, N'Database đã có bảng nhưng không có __EFMigrationsHistory (không do EF tạo) - dừng.', 1;
PRINT N'Đích: ' + DB_NAME() + N' — bắt đầu áp schema.sql';
GO

:r schema.sql

PRINT N'Xong. Chạy verify.sql để kiểm tra.';
GO
