#!/bin/bash
set -euo pipefail
RG_CODE="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
source "$RG_CODE/deploy/server/common.sh"
require_root
if [ "${RG_LOCK_INHERITED:-0}" != 1 ]; then
    exec 9>/run/lock/roadguard-server.lock
fi
flock -n 9 || { echo 'Another RoadGuard operation is in progress.' >&2; exit 1; }
export RG_RELEASE="$(cat "$RG_CODE/REVISION")"
[[ "$RG_RELEASE" =~ ^[0-9a-f]{40}$ ]] || { echo 'Invalid release directory.' >&2; exit 1; }
umask 077
source "$RG_CODE/deploy/server/release-manifest.sh"

check() {
    # Probe the existing loopback binding with TLS validation; public storage URLs are not API addresses.
    curl --fail --silent --show-error --max-time 20 --cacert "$RG_CONFIG/tls/ca.crt" --resolve localhost:8443:127.0.0.1 "https://localhost:8443/health"
    printf '\n'
    compose ps
}

backup() {
    local backed_up_release="$1" stamp directory query
    stamp="$(date -u +%Y%m%dT%H%M%SZ)"
    directory="$RG_DATA/backups/$stamp"
    mkdir -m 700 "$directory"
    # Also stop storage: outstanding signed uploads must not mutate its snapshot.
    if ai_configured; then compose stop api ai minio; else compose stop api minio; fi
    query="BACKUP DATABASE [RoadGuard_Server] TO DISK=N'/var/opt/mssql/roadguard-$stamp.bak' WITH COPY_ONLY, INIT, CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'/var/opt/mssql/roadguard-$stamp.bak' WITH CHECKSUM;"
    compose exec -T sql /bin/bash -c 'export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"; exec /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -N -C -b -Q "$1"' -- "$query"
    mv "$RG_DATA/sql/roadguard-$stamp.bak" "$directory/database.bak"
    tar -C / -czf "$directory/storage-keys-config.tar.gz" srv/roadguard/objects srv/roadguard/files srv/roadguard/keys srv/roadguard/ai etc/roadguard
    printf '%s\n' "$backed_up_release" > "$directory/RELEASE"
    (
        cd "$directory"
        sha256sum database.bak storage-keys-config.tar.gz RELEASE > SHA256SUMS
        sha256sum --check --strict SHA256SUMS
    )
    echo "Backup verified and saved: $directory"
    if [ -n "${2:-}" ]; then
        printf '%s\n' "$directory" > "$2"
    fi
}

start_storage() {
    compose up -d --wait --wait-timeout 300 sql
    compose up -d --wait --wait-timeout 300 minio
}

start_application() {
    if ai_configured; then
        compose up -d --wait --wait-timeout 300 ai
    fi
    compose up -d --wait --wait-timeout 330 api
    check
}

publish_release() {
    local code="$1" sha="$2" link marker
    link="$(mktemp -d /opt/roadguard/.activate.XXXXXX)"
    ln -s "$code" "$link/current"
    mv -Tf "$link/current" /opt/roadguard/current
    rmdir "$link"
    marker="$(mktemp "$RG_CONFIG/.active-release.XXXXXX")"
    printf '%s\n' "$sha" > "$marker"
    mv -Tf "$marker" "$RG_CONFIG/active-release"
}

activate() {
    local receipt="$1" before="$2" target_schema="${3:-}"
    start_storage
    compose run --rm --no-deps maintenance
    if [ -n "$target_schema" ]; then
        test "$(applied_migrations)" = "$target_schema"
    fi
    # Remove first-login plaintext after successful bootstrap. Never change its DB hash.
    python3 - <<'PY'
import json,os,pathlib
p=pathlib.Path('/etc/roadguard/maintenance.json')
config=json.loads(p.read_text())
if config['ServerDeployment'].pop('InitialSupervisor',None) is not None:
    tmp=p.with_suffix('.tmp')
    tmp.write_text(json.dumps(config,indent=2,ensure_ascii=False))
    os.chown(tmp,1654,10001); tmp.chmod(0o640); tmp.replace(p)
PY
    start_application
    write_release_manifest "$receipt" "$RG_RELEASE" "$before"
    publish_release "$RG_CODE" "$RG_RELEASE"
    echo "Active release: $RG_RELEASE"
}

rollback() {
    local backup="$1" previous before target current code active link
    [[ "$backup" =~ ^"$RG_DATA"/backups/[0-9]{8}T[0-9]{6}Z$ ]]
    test -d "$backup" && test ! -L "$backup"
    previous="$(cat "$backup/RELEASE")"
    [[ "$previous" =~ ^[0-9a-f]{40}$ ]]
    test "$previous" != "$RG_RELEASE"
    code="$RG_RELEASES/$previous"
    test "$(cat "$code/REVISION")" = "$previous"
    test "$(sed -n '2p' "$backup/TRANSITION")" = "$RG_RELEASE"
    test "$(sed -n '1p' "$backup/TRANSITION")" = "$previous"
    active="$(cat "$RG_CONFIG/active-release")"
    link="$(readlink -f /opt/roadguard/current)"
    [[ "$active" = "$previous" || "$active" = "$RG_RELEASE" ]]
    [[ "$link" = "$code" || "$link" = "$RG_CODE" ]]
    before="$(cat "$backup/MIGRATIONS_BEFORE")"
    target="$(cat "$backup/MIGRATIONS_TARGET")"
    current="$(applied_migrations)"
    if [ "$current" != "$before" ] && [ "$current" != "$target" ]; then
        echo 'Database is outside the verified compatibility range. Rollback requires review.' >&2
        return 1
    fi
    export RG_LOCK_INHERITED=1
    env -u RG_AI_RELEASE bash "$code/deploy/server/manage.sh" start
    publish_release "$code" "$previous"
    printf '%s\n' "$previous" > "$backup/ROLLED_BACK"
}

case "${1:-status}" in
    initialize)
        test ! -e "$RG_CONFIG/active-release"
        compose config --quiet
        prepare_ai_assets
        compose build api minio ai
        activate "$RG_CONFIG/release-receipt.json" ''
        ;;
    status) check ;;
    logs) if ai_configured; then compose logs --tail 100 api ai sql minio; else compose logs --tail 100 api sql minio; fi ;;
    backup) backup "$RG_RELEASE"; start_storage; start_application ;;
    backup-for-update)
        expected="${2:?Missing active release for deployment backup.}"
        [[ "$expected" =~ ^[0-9a-f]{40}$ ]]
        test "$(cat "$RG_CONFIG/active-release")" = "$expected"
        test "$(readlink -f /opt/roadguard/current)" = "$RG_RELEASES/$expected"
        backup "$expected" "${3:-}"
        ;;
    start) start_storage; start_application ;;
    stop) if ai_configured; then compose stop api ai minio sql; else compose stop api minio sql; fi ;;
    update) echo 'Use update-reviewed.sh EXPECTED_ACTIVE_SHA TARGET_SHA; branch-tip updates are disabled.' >&2; exit 2 ;;
    migrations) applied_migrations ;;
    activate)
        test "${RG_LOCK_INHERITED:-0}" = 1
        activate "${2:?Missing release receipt path.}" "${3-}" "${4-}"
        ;;
    rollback) rollback "${2:?Missing verified update backup directory.}" ;;
    *) echo 'Usage: manage.sh initialize|status|logs|backup|start|stop|migrations|rollback' >&2; exit 1 ;;
esac
