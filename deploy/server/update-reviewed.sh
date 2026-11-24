#!/bin/bash
# Deploy only a reviewed SHA. May be staged separately from the old release.
set -euo pipefail
expected="${1:-}" target="${2:-}"
[[ "$expected" =~ ^[0-9a-f]{40}$ && "$target" =~ ^[0-9a-f]{40}$ && $# == 2 ]] || {
    echo 'Usage: update-reviewed.sh EXPECTED_ACTIVE_SHA TARGET_SHA' >&2; exit 2;
}
current_code="$(readlink -f /opt/roadguard/current)"
test "$current_code" = "/opt/roadguard/releases/$expected"
test "$(cat "$current_code/REVISION")" = "$expected"
lock=/run/lock/roadguard-server.lock
if [ "${RG_BOOTSTRAP_REENTRY:-0}" = 1 ]; then
    RG_CODE="/opt/roadguard/releases/$target"
    test "$(cat "$RG_CODE/REVISION")" = "$target"
    [[ /proc/self/fd/9 -ef "$lock" ]] || { echo 'Missing inherited RoadGuard maintenance lock.' >&2; exit 1; }
else
    RG_CODE="$current_code"
fi
source "$RG_CODE/deploy/server/common.sh"
require_root
test "$(hostname)" = roadguardsystem
if [ "${RG_BOOTSTRAP_REENTRY:-0}" != 1 ]; then
    exec 9>"$lock"
fi
flock -n 9 || { echo 'Another RoadGuard operation is in progress.' >&2; exit 1; }
export RG_LOCK_INHERITED=1
test "$(cat "$RG_CONFIG/active-release")" = "$expected"
export RG_RELEASE="$expected"
if [ "$expected" = "$target" ]; then
    bash "$RG_CODE/deploy/server/manage.sh" status
    exit 0
fi

# No branch-tip deployment: fetch establishes provenance, target remains immutable.
git --git-dir="$RG_REPOSITORY" fetch origin deploy
git --git-dir="$RG_REPOSITORY" cat-file -e "$target^{commit}"
git --git-dir="$RG_REPOSITORY" merge-base --is-ancestor "$expected" "$target"
git --git-dir="$RG_REPOSITORY" merge-base --is-ancestor "$target" FETCH_HEAD

# Conservative baseline; this is not a claim that every build fits this budget.
available_kb="$(awk '/^MemAvailable:/ {print $2}' /proc/meminfo)"
test "$available_kb" -ge 4194304 || { echo 'Less than 4 GiB available RAM; defer deployment.' >&2; exit 1; }
data_bytes="$(du -sx --block-size=1 "$RG_DATA" | cut -f1)"
free_bytes="$(df -B1 --output=avail "$RG_DATA" | tail -1 | tr -d ' ')"
test "$free_bytes" -ge "$((data_bytes * 12 / 10 + 8589934592))" || {
    echo 'Insufficient conservative space budget for build and backup; no services stopped.' >&2; exit 1;
}
echo "Reviewed deployment: $expected -> $target"
extract_release "$target"
candidate="$RG_RELEASES/$target"

# The staged entrypoint first uses the active release's extractor. Re-enter
# from the target to load its composition and materialize its pinned AI assets.
if [ "${RG_BOOTSTRAP_REENTRY:-0}" != 1 ] && [ "$RG_CODE" != "$candidate" ]; then
    test -f "$candidate/deploy/server/update-reviewed.sh"
    exec env RG_BOOTSTRAP_REENTRY=1 \
        bash "$candidate/deploy/server/update-reviewed.sh" "$expected" "$target"
fi

RG_CODE="$candidate"
prepare_ai_assets
RG_CODE="$candidate" RG_RELEASE="$target" compose config --quiet
RG_CODE="$candidate" RG_RELEASE="$target" compose build api minio ai

# Snapshot the active release without starting candidate containers. Only
# activation may start them, after the backup and maintenance succeed.
if ! bash "$RG_CODE/deploy/server/manage.sh" backup-for-update "$expected"; then
    echo 'Backup failed. Candidate NOT activated. Preserve partial backup and inspect service state.' >&2
    exit 1
fi
test "$(cat "$RG_CONFIG/active-release")" = "$expected"
echo 'Backup completed. Activating the exact reviewed release.'
bash "$candidate/deploy/server/manage.sh" activate
test "$(cat "$RG_CONFIG/active-release")" = "$target"
echo "Deployment verified: $target"
