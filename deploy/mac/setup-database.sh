#!/bin/bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
[[ -n "$REPO_ROOT" ]] || fail 'Chạy script này từ deploy/mac trong repo.'
db="${1:-HomestayBooking}"
validate_db "$db"
sql_auth
# CREATE trong cùng batch với kiểm tồn tại; không bao giờ xóa hay cập nhật DB có sẵn.
sql -d master -Q "IF DB_ID(N'$db') IS NOT NULL THROW 51000, 'Database da ton tai; dung, khong ghi de.', 1; CREATE DATABASE [$db];"
work="/tmp/fuji-deploy-$$"
docker exec "$SQL_CONTAINER" mkdir -p "$work"
trap 'docker exec "$SQL_CONTAINER" rm -rf "$work" >/dev/null' EXIT
for file in deploy.sql schema.sql verify.sql; do
  docker cp "$REPO_ROOT/database/$file" "$SQL_CONTAINER:$work/$file" >/dev/null
done
docker exec -i -e SQLCMDPASSWORD -w "$work" "$SQL_CONTAINER" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -d "$db" -v "TargetDb=$db" -i deploy.sql
sql -d "$db" -i "$work/verify.sql"
echo "Đã dựng và kiểm schema: $db"
