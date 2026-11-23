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

check() {
    # Probe the existing loopback binding with TLS validation; public storage URLs are not API addresses.
    curl --fail --silent --show-error --max-time 20 --cacert "$RG_CONFIG/tls/ca.crt" --resolve localhost:8443:127.0.0.1 "https://localhost:8443/health"
    printf '\n'
    compose ps
}

backup() {
    local stamp directory query
    stamp="$(date -u +%Y%m%dT%H%M%SZ)"
    directory="$RG_DATA/backups/$stamp"
    mkdir -m 700 "$directory"
    # Also stop storage: outstanding signed uploads must not mutate its snapshot.
    compose stop api minio
    query="BACKUP DATABASE [RoadGuard_Server] TO DISK=N'/var/opt/mssql/roadguard-$stamp.bak' WITH COPY_ONLY, INIT, CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'/var/opt/mssql/roadguard-$stamp.bak' WITH CHECKSUM;"
    compose exec -T sql /bin/bash -c 'export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"; exec /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -N -C -b -Q "$1"' -- "$query"
    mv "$RG_DATA/sql/roadguard-$stamp.bak" "$directory/database.bak"
    tar -C / -czf "$directory/storage-keys-config.tar.gz" srv/roadguard/objects srv/roadguard/files srv/roadguard/keys etc/roadguard
    printf '%s\n' "$RG_RELEASE" > "$directory/RELEASE"
    (cd "$directory" && sha256sum database.bak storage-keys-config.tar.gz RELEASE > SHA256SUMS)
    echo "Backup verified and saved: $directory"
}

activate() {
    compose up -d --wait --wait-timeout 300 sql minio
    compose run --rm --no-deps maintenance
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
    compose up -d --wait --wait-timeout 300 api
    check
    ln -sfn "$RG_CODE" /opt/roadguard/current
    printf '%s\n' "$RG_RELEASE" > "$RG_CONFIG/active-release"
    echo "Active release: $RG_RELEASE"
}

case "${1:-status}" in
    initialize)
        test ! -e "$RG_CONFIG/active-release"
        compose config --quiet
        compose build api minio
        activate
        ;;
    status) check ;;
    logs) compose logs --tail 100 api sql minio ;;
    backup) backup; compose up -d --wait --wait-timeout 300 minio api; check ;;
    start) compose up -d --wait --wait-timeout 300 sql minio api; check ;;
    stop) compose stop api minio sql ;;
    update) echo 'Use update-reviewed.sh EXPECTED_ACTIVE_SHA TARGET_SHA; branch-tip updates are disabled.' >&2; exit 2 ;;
    activate) activate ;;
    *) echo 'Usage: manage.sh initialize|status|logs|backup|start|stop|update' >&2; exit 1 ;;
esac
