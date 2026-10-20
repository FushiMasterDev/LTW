#!/bin/bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
[[ -n "$REPO_ROOT" ]] || fail 'Chạy script này từ deploy/mac trong repo.'
load_env
target="${1:-$FUJI_APP}"
case "$target" in
  "$FUJI_APP"|"$FUJI_HOME"/update-stage-[0-9]*) ;;
  *) fail 'Chỉ publish vào FUJI_HOME/app hoặc update-stage-PID do update.sh tạo.' ;;
esac
if [[ "$target" == "$FUJI_APP" ]] && app_running; then fail 'App đang chạy; dùng update.sh.'; fi
stage="$(mktemp -d "$FUJI_HOME/.publish.XXXXXX")"
trap 'rm -rf "$stage"' EXIT
"$DOTNET_BIN" publish "$REPO_ROOT/HomestayBooking.Web" -c Release -o "$stage"
# Dữ liệu người dùng sống ngoài release. Nhập dữ liệu kiểu cũ trước lần nâng cấp đầu.
if [[ -d "$target/App_Data" && ! -L "$target/App_Data" ]]; then cp -Rp "$target/App_Data/." "$FUJI_HOME/data/App_Data/"; fi
if [[ -d "$target/wwwroot/uploads" && ! -L "$target/wwwroot/uploads" ]]; then cp -Rp "$target/wwwroot/uploads/." "$FUJI_HOME/data/uploads/"; fi
rm -rf "$stage/App_Data" "$stage/wwwroot/uploads"
ln -s "$FUJI_HOME/data/App_Data" "$stage/App_Data"
ln -s "$FUJI_HOME/data/uploads" "$stage/wwwroot/uploads"
[[ ! -e "$target" ]] || mv "$target" "$FUJI_HOME/release-before-publish-$(date +%Y%m%dT%H%M%S)-$$"
mv "$stage" "$target"
echo "Đã publish: $target"
