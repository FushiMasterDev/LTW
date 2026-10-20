#!/bin/bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
load_env
backup="${1:?Cần đường dẫn .bak}"; target="${2:?Cần tên database kiểm tra mới}"
validate_db "$target"
[[ "$target" != "$DATABASE_NAME" && "$target" == *_Restore* ]] || fail 'Đích phải khác DB đang chạy và có hậu tố _Restore.'
[[ -f "$backup" && "$backup" == *.bak ]] || fail 'Không có file .bak.'
sql_auth
sql -d master -Q "IF DB_ID(N'$target') IS NOT NULL THROW 51000, 'Dich restore da ton tai; dung.', 1;"
docker exec "$SQL_CONTAINER" mkdir -p /var/opt/mssql/backup
work="/var/opt/mssql/backup/fuji-restore-$$.bak"
trap 'docker exec "$SQL_CONTAINER" rm -f "$work" >/dev/null' EXIT
docker cp "$backup" "$SQL_CONTAINER:$work" >/dev/null
docker exec -u 0 "$SQL_CONTAINER" chown mssql "$work"
# Lấy tên logical từ chính backup, không giả định bằng tên database.
list="$(sql -d master -h -1 -W -s '|' -Q "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK=N'$work';")"
moves="$(printf '%s\n' "$list" | python3 -c '
import sys
rows=[r.split("|") for r in sys.stdin.read().splitlines() if "|" in r]
target=sys.argv[1]; parts=[]
for i,r in enumerate(rows):
    if len(r)<3 or r[2].strip() not in ("D","L"): raise SystemExit("Backup co loai file khong ho tro")
    name=r[0].strip().replace("\x27","\x27\x27"); ext="ldf" if r[2].strip()=="L" else "mdf"
    parts.append("MOVE N\x27"+name+"\x27 TO N\x27/var/opt/mssql/data/"+target+"_"+str(i)+"."+ext+"\x27")
if not parts: raise SystemExit("Khong doc duoc FILELISTONLY")
print(", ".join(parts))' "$target")"
sql -d master -Q "IF DB_ID(N'$target') IS NOT NULL THROW 51000, 'Dich da ton tai', 1; RESTORE DATABASE [$target] FROM DISK=N'$work' WITH CHECKSUM, $moves; DBCC CHECKDB ([$target]) WITH NO_INFOMSGS;"
echo "Đã restore kiểm tra: $target. File upload: giải nén bộ .files.tar.gz cạnh .bak vào thư mục kiểm tra riêng, không chép đè dữ liệu đang dùng."
