#!/bin/bash
# Read-only runtime check. Compare against the saved baseline after a restart.
set -euo pipefail
RG_CODE="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
source "$RG_CODE/deploy/server/common.sh"
source "$RG_CODE/deploy/server/release-manifest.sh"
require_root
test "$(hostname)" = roadguardsystem
case "${1-}" in
    '') test "$#" -eq 0 ;;
    --after-restart) test "$#" -eq 1 ;;
    *) echo 'Usage: recovery-check.sh [--after-restart]' >&2; exit 2 ;;
esac
exec 9>>/run/lock/roadguard-server.lock
flock -s -n 9
export RG_RELEASE="$(cat "$RG_CONFIG/active-release")"
[[ "$RG_RELEASE" =~ ^[0-9a-f]{40}$ ]]
test "$RG_CODE" = "$RG_RELEASES/$RG_RELEASE"
test "$(cat "$RG_CODE/REVISION")" = "$RG_RELEASE"
test "$(readlink -f /opt/roadguard/current)" = "$RG_CODE"
systemctl is-active --quiet docker
systemctl is-enabled --quiet docker

check_service() {
    local service="$1" container mount source observed image
    shift
    container="$(compose ps -q "$service")"
    for mount in "$@"; do
        source="$RG_DATA/${mount%%:*}"
        test -d "$source"
        observed="$(docker inspect --format '{{range .Mounts}}{{println .Source .Destination}}{{end}}' "$container")"
        grep -Fxq "$source ${mount#*:}" <<< "$observed"
    done
    image="$(service_image_id "$service")"
    printf '%s %s\n' "$service" "$image"
}

printf 'mode=%s\nrelease=%s\ndocker=active,enabled\n' "${1:-baseline}" "$RG_RELEASE"
check_service sql sql:/var/opt/mssql
check_service minio objects:/data
if ai_configured; then
    check_service ai ai/worker:/worker ai/models:/models
    printf 'modelSha256=%s\n' "$(sha256sum "$RG_DATA/ai/models/ft95.pt" | cut -d' ' -f1)"
fi
check_service api files:/files keys:/keys ai/be-spool:/ai-be-spool
enabled="$(compose exec -T api printenv AiRealWorker__Enabled)"
[[ "$enabled" =~ ^(true|false)$ ]]
if [ "$enabled" = true ]; then
    ai_configured
fi
printf 'aiEnabled=%s\n' "$enabled"
curl --fail --silent --show-error --max-time 20 --cacert "$RG_CONFIG/tls/ca.crt" \
    --resolve localhost:8443:127.0.0.1 https://localhost:8443/health
printf '\n'
query_deployment_sql "SELECT CONCAT('schema=',MAX(MigrationId)) FROM dbo.__EFMigrationsHistory;
    SELECT CONCAT('aiWorkState.',State,'=',COUNT_BIG(*)) FROM dbo.AiVideoWorkItems GROUP BY State ORDER BY State;
    SELECT CONCAT('jobs=',COUNT_BIG(*)) FROM dbo.ProcessingJobs;
    SELECT CONCAT('attempts=',COUNT_BIG(*)) FROM dbo.ProcessingAttempts;
    SELECT CONCAT('detections=',COUNT_BIG(*)) FROM dbo.AIDetections;
    SELECT CONCAT('registeredArtifacts=',COUNT_BIG(*)) FROM dbo.AiWorkerArtifacts WHERE FileId IS NOT NULL;
    SELECT CONCAT('projects=',COUNT_BIG(*)) FROM dbo.Projects;
    SELECT CONCAT('accounts=',COUNT_BIG(*)) FROM dbo.Users;"
printf 'completedResultsFingerprint='
query_deployment_sql "SELECT CONCAT(w.Id,' ',w.CurrentAttemptId,' ',a.Id,' ',a.Sha256,' ',a.FileId)
    FROM dbo.AiVideoWorkItems w JOIN dbo.AiWorkerArtifacts a ON a.WorkItemId=w.Id AND a.AttemptId=w.CurrentAttemptId
    WHERE w.State=5 AND a.FileId IS NOT NULL ORDER BY w.Id,a.Id;" | sha256sum | cut -d' ' -f1
