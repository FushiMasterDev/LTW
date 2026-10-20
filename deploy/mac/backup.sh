#!/bin/bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
load_env
mkdir "$FUJI_RUN/backup.lock" 2>/dev/null || fail 'Có backup khác đang chạy.'
trap 'rmdir "$FUJI_RUN/backup.lock"' EXIT
sql_auth
stamp="${DATABASE_NAME}_$(date +%Y%m%dT%H%M%S)_$$"
remote="/var/opt/mssql/backup/$stamp.bak"
local_dir="$FUJI_HOME/backups"
docker exec "$SQL_CONTAINER" mkdir -p /var/opt/mssql/backup
sql -d master -Q "BACKUP DATABASE [$DATABASE_NAME] TO DISK=N'$remote' WITH COPY_ONLY, CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'$remote' WITH CHECKSUM;"
docker cp "$SQL_CONTAINER:$remote" "$local_dir/$stamp.bak.part" >/dev/null
mv "$local_dir/$stamp.bak.part" "$local_dir/$stamp.bak"
chmod 600 "$local_dir/$stamp.bak"
tar -czf "$local_dir/$stamp.files.tar.gz.part" -C "$FUJI_HOME/data" App_Data uploads
mv "$local_dir/$stamp.files.tar.gz.part" "$local_dir/$stamp.files.tar.gz"
(cd "$local_dir" && shasum -a 256 "$stamp.bak" "$stamp.files.tar.gz" > "$stamp.sha256")
# Chỉ giữ 14 bộ hoàn chỉnh của database này, gồm cả bản nằm trên volume SQL.
python3 - "$local_dir" "$DATABASE_NAME" "$SQL_CONTAINER" <<'PY'
import pathlib,sys,subprocess,re
p=pathlib.Path(sys.argv[1]); db=sys.argv[2]
pattern=re.compile(re.escape(db)+r'_\d{8}T\d{6}_\d+\.sha256$')
sets=sorted((x for x in p.glob(db+'_*.sha256') if pattern.fullmatch(x.name)),key=lambda x:x.name,reverse=True)
for manifest in sets[14:]:
    stem=manifest.name[:-7]
    for suffix in ('.bak','.files.tar.gz','.sha256'): (p/(stem+suffix)).unlink(missing_ok=True)
    subprocess.run(['docker','exec',sys.argv[3],'rm','-f','/var/opt/mssql/backup/'+stem+'.bak'],check=True)
PY
echo "Backup: $local_dir/$stamp.bak"
