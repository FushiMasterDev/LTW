#!/bin/bash
# Chỉ người vận hành tự gọi trong cửa sổ bảo trì. Không chạy từ start/update/LaunchAgent.
set -euo pipefail
umask 077
die() { echo "Lỗi: $*" >&2; exit 1; }
[[ "${1:-}" == --apply && $# == 1 ]] || {
  echo 'Cách dùng: ./deploy/mac/sql-harden.sh --apply'
  echo 'Sao lưu mọi DB (trừ tempdb), restore DB người dùng sang volume Express MỚI, giữ container/volume cũ.'
  echo 'Phải tự dừng mọi app ghi SQL (kể cả app 5011), có bản backup ngoài máy và đọc runbook trước.'
  exit 1
}
[[ -t 0 ]] || die 'Cần terminal tương tác để xác nhận; không pipe yes vào script.'
source_name=sql-homestay
candidate=sql-homestay-express
stamp="$(date +%Y%m%dT%H%M%S)"
old_name="sql-homestay-before-express-$stamp"
volume="sql-homestay-express-$stamp"
backup_dir="${SQL_HARDEN_BACKUP_DIR:-$HOME/FujiHomestay/sql-harden-$stamp}"
[[ "$backup_dir" == /* && ! -e "$backup_dir" ]] || die 'Thư mục backup phải tuyệt đối, mới và ngoài repo.'
backup_dir="$(python3 -c 'import pathlib,sys; print(pathlib.Path(sys.argv[1]).resolve())' "$backup_dir")"
repo="$(cd "$(dirname "$0")/../.." && pwd)"
[[ -f "$repo/HomestayBooking.sln" ]] || die 'Chạy bản sql-harden.sh từ deploy/mac trong checkout repo.'
case "$backup_dir/" in "$repo/"*) die 'Backup/bí mật không được nằm trong repo.' ;; esac
docker inspect "$source_name" >/dev/null
! docker inspect "$candidate" >/dev/null 2>&1 || die 'Container đích đã tồn tại; kiểm lần bảo trì trước.'
! docker inspect "$old_name" >/dev/null 2>&1 || die 'Tên container lưu trữ đã tồn tại.'
! docker volume inspect "$volume" >/dev/null 2>&1 || die 'Volume đích đã tồn tại.'
! lsof -nP -iTCP:1434 -sTCP:LISTEN >/dev/null 2>&1 || die 'Cổng thử 1434 đang được dùng.'
# Pin đúng image ID hiện có: không pull/latest hay nâng version trong lúc hạ edition.
image="$(docker inspect --format '{{.Image}}' "$source_name")"
SQLCMDPASSWORD="$(docker exec "$source_name" printenv MSSQL_SA_PASSWORD)"
export SQLCMDPASSWORD
[[ -n "$SQLCMDPASSWORD" ]] || die 'Không đọc được mật khẩu sa; không in mật khẩu.'
[[ "$SQLCMDPASSWORD" != *$'\n'* && "$SQLCMDPASSWORD" != *$'\r'* ]] || die 'Mật khẩu có xuống dòng; cần di chuyển thủ công.'
sql_on() { docker exec -i -e SQLCMDPASSWORD "$1" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -r1 "${@:2}"; }
major="$(sql_on "$source_name" -h -1 -W -Q "SET NOCOUNT ON; SELECT SERVERPROPERTY('ProductMajorVersion');" | tr -d '[:space:]')"
[[ "$major" == 16 ]] || die 'Script dành cho SQL Server 2022; không tự hạ/nâng version khác.'
# Refuse cases needing a bespoke migration rather than silently losing server configuration.
sql_on "$source_name" -Q "
IF EXISTS (SELECT 1 FROM sys.databases WHERE name <> 'tempdb' AND (state <> 0 OR is_read_only = 1 OR source_database_id IS NOT NULL OR is_encrypted = 1))
 THROW 51000, 'DB offline/read-only/snapshot/encrypted: can ke hoach rieng.', 1;
IF EXISTS (SELECT 1 FROM sys.master_files WHERE database_id > 4 AND type = 0 GROUP BY database_id HAVING SUM(CONVERT(bigint,size))*8 > 10*1024*1024)
 THROW 51000, 'DB vuot 10 GB cua Express.', 1;
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE type IN ('S','U','G') AND name <> 'sa' AND name NOT LIKE '##%')
 THROW 51000, 'Co login rieng: can xuat/doi chieu login SID va quyen truoc.', 1;
IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE enabled = 1)
 THROW 51000, 'Co SQL Agent jobs; Express khong co Agent.', 1;"
dbs="$(sql_on "$source_name" -h -1 -W -s '|' -Q "SET NOCOUNT ON; SELECT CONVERT(varchar(12),database_id),name FROM sys.databases WHERE name <> 'tempdb' ORDER BY database_id;")"
printf '%s\n' "$dbs" | python3 -c '
import sys,re
rows=[x.strip().split("|") for x in sys.stdin if x.strip()]
if not rows or any(len(x)!=2 or not x[0].isdigit() or not re.fullmatch(r"[A-Za-z0-9_]+",x[1]) for x in rows):
    sys.exit("Ten DB dac biet: can ke hoach rieng.")
'
while IFS='|' read -r id db; do
  [[ -n "$id" && "$id" -gt 4 ]] || continue
  sql_on "$source_name" -d "$db" -Q "IF EXISTS (SELECT 1 FROM sys.dm_db_persisted_sku_features) THROW 51000, 'Tinh nang edition khong ho tro; dung de kiem tra.', 1;"
done <<< "$dbs"
echo 'Sẽ giữ nguyên volume cũ; backup master/model/msdb và mọi DB người dùng; tempdb được tạo lại.'
echo 'Express không giữ cấu hình cấp server từ master/msdb; cần kiểm riêng linked server/credential/certificate/Agent.'
echo 'Không tiếp tục nếu có cấu hình đó chưa được xuất, hoặc chưa có bản backup ngoài máy.'
read -r -p 'Đã tự dừng mọi app ghi SQL (cả 5011) và chuẩn bị khôi phục? Gõ DA_DUNG_APP_VA_SAO_LUU: ' answer
[[ "$answer" == DA_DUNG_APP_VA_SAO_LUU ]] || die 'Đã hủy; chưa sửa container.'
mkdir -p -m 700 "$backup_dir"
docker inspect --format '{{json .Mounts}}' "$source_name" > "$backup_dir/source-mounts.json"
docker inspect --format '{{.Image}} {{.Config.Image}} {{json .HostConfig.RestartPolicy}} {{json .NetworkSettings.Networks}}' "$source_name" > "$backup_dir/source-config.txt"
printf '%s\n' "$dbs" > "$backup_dir/databases.txt"
remote="/var/opt/mssql/backup/harden-$stamp"
docker exec "$source_name" mkdir -p "$remote"
while IFS='|' read -r id db; do
  [[ -n "$id" ]] || continue
  sql_on "$source_name" -Q "BACKUP DATABASE [$db] TO DISK=N'$remote/db_$id.bak' WITH COPY_ONLY,CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'$remote/db_$id.bak' WITH CHECKSUM;"
  docker cp "$source_name:$remote/db_$id.bak" "$backup_dir/db_$id.bak" >/dev/null
done <<< "$dbs"
(cd "$backup_dir" && shasum -a 256 ./*.bak > SHA256SUMS && shasum -a 256 -c SHA256SUMS)
echo "Backup đã VERIFYONLY và copy ra: $backup_dir. Không xóa bộ này."
read -r -p 'Đã chép bộ backup sang ổ khác, app vẫn dừng? Gõ RESTORE_EXPRESS: ' answer
[[ "$answer" == RESTORE_EXPRESS ]] || die 'Dừng sau backup, container nguồn vẫn chạy.'
docker volume create "$volume" >/dev/null
env_file="$backup_dir/container.env"
trap 'rm -f "$env_file"' EXIT
printf 'ACCEPT_EULA=Y\nMSSQL_PID=Express\nMSSQL_SA_PASSWORD=%s\n' "$SQLCMDPASSWORD" > "$env_file"
chmod 600 "$env_file"
# On any failure keep source running, new volume and backups for inspection; never delete source.
docker run -d --name "$candidate" --platform linux/amd64 --env-file "$env_file" \
  -p 127.0.0.1:1434:1433 -v "$volume:/var/opt/mssql" "$image" >/dev/null
ready=false
for ((n=0;n<90;n++)); do
  if sql_on "$candidate" -Q 'SELECT 1' >/dev/null 2>&1; then ready=true; break; fi
  sleep 1
done
[[ "$ready" == true ]] || die 'Express chưa sẵn sàng; nguồn chưa dừng, giữ đích để kiểm log.'
docker exec "$candidate" mkdir -p /var/opt/mssql/backup/harden
while IFS='|' read -r id db; do
  [[ -n "$id" && "$id" -gt 4 ]] || continue
  docker cp "$backup_dir/db_$id.bak" "$candidate:/var/opt/mssql/backup/harden/db_$id.bak" >/dev/null
  docker exec -u 0 "$candidate" chown mssql "/var/opt/mssql/backup/harden/db_$id.bak"
  files="$(sql_on "$candidate" -h -1 -W -s '|' -Q "RESTORE FILELISTONLY FROM DISK=N'/var/opt/mssql/backup/harden/db_$id.bak';")"
  moves="$(printf '%s\n' "$files" | python3 -c '
import sys
rows=[x.split("|") for x in sys.stdin if "|" in x]; db=sys.argv[1]; parts=[]
for i,r in enumerate(rows):
    if len(r)<3 or r[2].strip() not in ("D","L"): sys.exit("Loai file khong ho tro; nguon van con nguyen")
    name=r[0].strip().replace(chr(39),chr(39)*2); ext="ldf" if r[2].strip()=="L" else "mdf"
    parts.append("MOVE N\x27"+name+"\x27 TO N\x27/var/opt/mssql/data/"+db+"_"+str(i)+"."+ext+"\x27")
if not parts: sys.exit("Khong doc duoc FILELISTONLY")
print(", ".join(parts))' "$db")"
  sql_on "$candidate" -Q "IF DB_ID(N'$db') IS NOT NULL THROW 51000, 'Dich da ton tai', 1; RESTORE DATABASE [$db] FROM DISK=N'/var/opt/mssql/backup/harden/db_$id.bak' WITH CHECKSUM,$moves; DBCC CHECKDB ([$db]) WITH NO_INFOMSGS;"
done <<< "$dbs"
sql_on "$candidate" -Q "SELECT SERVERPROPERTY('Edition') AS Edition; IF CONVERT(nvarchar(128),SERVERPROPERTY('Edition')) NOT LIKE '%Express%' THROW 51000, 'Khong phai Express', 1; SELECT name,state_desc FROM sys.databases;"
echo 'Đã restore/CHECKDB trên localhost:1434. Dùng cấu hình app thử riêng để kiểm mọi DB, không cho khách ghi dữ liệu.'
echo "Chỉ cutover khi đối chiếu dữ liệu/quyền thành công. Hủy ở đây giữ cả nguồn và bản Express."
read -r -p 'Đã kiểm app trên 1434 và dừng app thử, đồng ý ngắt SQL cũ? Gõ CHUYEN_SANG_EXPRESS: ' answer
[[ "$answer" == CHUYEN_SANG_EXPRESS ]] || die 'Giữ nguồn chạy và bản thử ở 1434; chưa cutover.'
echo "Kế hoạch khôi phục trước khi đổi: nguồn $source_name, tên lưu $old_name, volume đích $volume."
echo 'Nếu bị ngắt giữa stop và rename nguồn: kiểm docker ps -a rồi start lại nguồn, giữ app bảo trì.'
docker stop "$candidate" >/dev/null
docker rm "$candidate" >/dev/null # Chỉ bỏ container thử của script, giữ volume mới.
# A source with restart=always must not reclaim 1433 after the next Docker restart.
# Its previous restart policy was saved in source-config.txt for manual rollback.
docker update --restart=no "$source_name" >/dev/null
docker stop "$source_name" >/dev/null
docker rename "$source_name" "$old_name"
echo "Container cũ được giữ là $old_name; volume nguồn không đổi."
echo "Nếu lỗi: docker stop sql-homestay; docker rename sql-homestay sql-homestay-express-failed-$stamp; docker rename $old_name sql-homestay; docker start sql-homestay"
docker run -d --name "$source_name" --platform linux/amd64 --restart unless-stopped --env-file "$env_file" \
  -p 127.0.0.1:1433:1433 -v "$volume:/var/opt/mssql" "$image" >/dev/null
ready=false
for ((n=0;n<90;n++)); do
  if sql_on "$source_name" -Q 'SELECT 1' >/dev/null 2>&1; then ready=true; break; fi
  sleep 1
done
[[ "$ready" == true ]] || die 'Đích chưa khỏe; giữ app dừng và dùng kế hoạch rollback vừa in.'
sql_on "$source_name" -Q "SELECT SERVERPROPERTY('Edition') AS Edition; IF CONVERT(nvarchar(128),SERVERPROPERTY('Edition')) NOT LIKE '%Express%' THROW 51000, 'Khong phai Express', 1;"
docker port "$source_name" 1433
echo 'Phải thấy duy nhất 127.0.0.1:1433. Tự khởi động lại/kiểm cả app Production và 5011, chạy backup.'
echo 'Giữ container cũ, volume cũ và backup; không rollback về bản cũ sau khi đã có giao dịch mới nếu chưa đồng bộ dữ liệu.'
