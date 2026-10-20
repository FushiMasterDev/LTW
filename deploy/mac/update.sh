#!/bin/bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
load_env
mkdir "$FUJI_RUN/update.lock" 2>/dev/null || fail 'Có cập nhật khác đang chạy.'
stage="$FUJI_HOME/update-stage-$$"; previous="$FUJI_HOME/release-previous-$(date +%Y%m%dT%H%M%S)-$$"
swapped=false
stopped=false
restore_binary() {
  local result=$?
  trap - EXIT
  if [[ "$result" != 0 && "$swapped" == true ]]; then
    echo 'Cập nhật lỗi, phục hồi binary trước.' >&2
    "$SCRIPT_DIR/stop.sh" || { rmdir "$FUJI_RUN/update.lock"; exit 1; }
    if [[ -d "$FUJI_APP" ]]; then mv "$FUJI_APP" "$FUJI_HOME/release-failed-$(date +%Y%m%dT%H%M%S)-$$"; fi
    mv "$previous" "$FUJI_APP"
    "$SCRIPT_DIR/start.sh" || echo 'Rollback chưa khỏe; xem log.' >&2
  elif [[ "$result" != 0 && "$stopped" == true ]]; then
    "$SCRIPT_DIR/start.sh" || echo 'Bản cũ chưa khỏe; xem log.' >&2
  fi
  rmdir "$FUJI_RUN/update.lock"
  exit "$result"
}
trap restore_binary EXIT
# Không tự migrate: rollback binary không thể đảo schema hay dữ liệu nghiệp vụ.
"$SCRIPT_DIR/backup.sh"
"$SCRIPT_DIR/publish.sh" "$stage"
"$SCRIPT_DIR/stop.sh"
stopped=true
mv "$FUJI_APP" "$previous"
swapped=true
mv "$stage" "$FUJI_APP"
"$SCRIPT_DIR/start.sh"
health >/dev/null
swapped=false
echo "Cập nhật thành công. Bản trước: $previous"
