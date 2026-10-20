#!/bin/bash
set -euo pipefail
umask 077
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
[[ -f "$REPO_ROOT/HomestayBooking.sln" ]] || REPO_ROOT=""
fail() { echo "Lỗi: $*" >&2; exit 1; }
validate_db() { [[ "$1" =~ ^HomestayBooking(_[A-Za-z0-9_]+)?$ ]] || fail 'Tên database phải bắt đầu HomestayBooking, chỉ chữ/số/gạch dưới.'; [[ "$1" != HomestayBooking_v5* ]] || fail 'Không cho phép thao tác v5.'; }
load_env() {
  FUJI_ENV="${FUJI_ENV:-$HOME/FujiHomestay/production.env}"
  [[ -f "$FUJI_ENV" && ! -L "$FUJI_ENV" ]] || fail 'Không có file env thường.'
  local mode owner
  mode="$(stat -f '%Lp' "$FUJI_ENV")"; owner="$(stat -f '%u' "$FUJI_ENV")"
  [[ "$mode" == 600 && "$owner" == "$(id -u)" ]] || fail 'File env phải thuộc bạn và có quyền 600.'
  # File này là mã shell do chính người vận hành quản lý; không nhận file từ bên ngoài.
  set -a
  source "$FUJI_ENV"
  set +a
  FUJI_HOME="${FUJI_HOME:-$HOME/FujiHomestay}"
  [[ "$FUJI_HOME" == /* ]] || fail 'FUJI_HOME phải là đường dẫn tuyệt đối ngoài repo.'
  FUJI_HOME="$(python3 -c 'import pathlib,sys; print(pathlib.Path(sys.argv[1]).resolve())' "$FUJI_HOME")"
  [[ -z "$REPO_ROOT" || "$FUJI_HOME" != "$REPO_ROOT"* ]] || fail 'FUJI_HOME phải ngoài repo.'
  FUJI_APP="$FUJI_HOME/app"; FUJI_RUN="$FUJI_HOME/run"; FUJI_LOG="$FUJI_HOME/logs"
  mkdir -p "$FUJI_RUN" "$FUJI_LOG" "$FUJI_HOME/data/App_Data" "$FUJI_HOME/data/uploads" "$FUJI_HOME/backups"
  chmod 700 "$FUJI_HOME" "$FUJI_HOME/data" "$FUJI_HOME/data/App_Data" "$FUJI_HOME/data/uploads"
  [[ "${ASPNETCORE_ENVIRONMENT:-}" == Production ]] || fail 'Chỉ dùng Production.'
  [[ "${ASPNETCORE_URLS:-}" == http://127.0.0.1:8080 ]] || fail 'App phải chỉ nghe http://127.0.0.1:8080.'
  : "${ConnectionStrings__Default:?Thiếu chuỗi kết nối}" "${DATABASE_NAME:?Thiếu DATABASE_NAME}"
  validate_db "$DATABASE_NAME"
  python3 - <<'PY'
import os,re,sys
s=os.environ['ConnectionStrings__Default']
m=re.search(r'(?:^|;)\s*(?:Database|Initial Catalog)\s*=\s*([^;]+)',s,re.I)
if not m or m[1].strip()!=os.environ['DATABASE_NAME']: sys.exit('Database trong chuỗi kết nối không khớp DATABASE_NAME')
PY
  SQL_CONTAINER="${SQL_CONTAINER:-sql-homestay}"
  DOTNET_BIN="${DOTNET_BIN:-$(command -v dotnet || true)}"
  [[ -x "$DOTNET_BIN" ]] || fail 'Không tìm thấy dotnet; đặt DOTNET_BIN tuyệt đối trong env.'
  export FUJI_HOME FUJI_APP FUJI_ENV SQL_CONTAINER DOTNET_BIN
}
sql_auth() { SQL_CONTAINER="${SQL_CONTAINER:-sql-homestay}"; SQLCMDPASSWORD="$(docker exec "$SQL_CONTAINER" printenv MSSQL_SA_PASSWORD)"; export SQLCMDPASSWORD; [[ -n "$SQLCMDPASSWORD" ]] || fail 'Không đọc được mật khẩu SQL.'; }
sql() { docker exec -i -e SQLCMDPASSWORD "$SQL_CONTAINER" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -r1 "$@"; }
app_running() {
  [[ -f "$FUJI_RUN/app.pid" ]] || return 1
  APP_PID="$(cat "$FUJI_RUN/app.pid")"
  [[ "$APP_PID" =~ ^[0-9]+$ ]] || fail 'PID file không hợp lệ.'
  kill -0 "$APP_PID" 2>/dev/null || return 1
  local command
  command="$(ps -p "$APP_PID" -o command=)"
  [[ "$command" == *"$FUJI_APP/HomestayBooking.Web.dll"* ]] || fail 'PID thuộc tiến trình khác; không gửi tín hiệu.'
}
health() { curl --fail --silent --show-error --max-time 5 http://127.0.0.1:8080/healthz; }
wait_health() { local n; for n in {1..60}; do if app_running && health >/dev/null 2>&1; then return 0; fi; sleep 1; done; return 1; }
