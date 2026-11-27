#!/bin/bash
# Sourced by manage.sh with the deployment lock held.

query_deployment_sql() {
    compose exec -T sql /bin/bash -c 'export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"; exec /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -N -C -d RoadGuard_Server -b -h -1 -W -Q "$1"' -- "SET NOCOUNT ON; SET LOCK_TIMEOUT 5000; $1" 2>/dev/null | tr -d '\r'
}

applied_migrations() {
    query_deployment_sql 'SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;'
}

service_image_id() {
    local service="$1" container actual expected state
    container="$(compose ps -q "$service")" || return
    [[ "$container" =~ ^[0-9a-f]{12,64}$ ]] || return 1
    state="$(docker inspect --format '{{.State.Health.Status}}' "$container")" || return
    test "$state" = healthy || return
    actual="$(docker inspect --format '{{.Image}}' "$container")" || return
    expected="$(compose config --format json | python3 -c \
        'import json,sys; print(json.load(sys.stdin)["services"][sys.argv[1]]["image"])' "$service")" || return
    # Match the configured service reference and its immutable local image ID.
    local reference
    reference="$(docker inspect --format '{{.Config.Image}}' "$container")" || return
    test "$reference" = "$expected" || return
    test "$(docker image inspect --format '{{.Id}}' "$reference")" = "$actual" || return
    printf '%s\n' "$actual"
}

write_release_manifest() {
    local output="$1" sha="$2" before="$3" after api ai='' minio sql model_hash='' model='' enabled
    test "$sha" = "$RG_RELEASE" || return
    after="$(applied_migrations)" || return
    api="$(service_image_id api)" || return
    minio="$(service_image_id minio)" || return
    sql="$(service_image_id sql)" || return
    enabled="$(compose exec -T api printenv AiRealWorker__Enabled)" || return
    if ai_configured; then
        ai="$(service_image_id ai)" || return
        model_hash="$(sha256sum "$RG_DATA/ai/models/ft95.pt" | cut -d' ' -f1)" || return
    fi
    if [ "$enabled" = true ]; then
        model="$(python3 -c 'import json,sys,uuid; print(uuid.UUID(json.load(open(sys.argv[1]))["AiRealWorker"]["ReleasedModelVersionId"]))' "$RG_CONFIG/application.json" 2>/dev/null)" || return
        model="$(query_deployment_sql "SELECT LOWER(CONVERT(varchar(36),Id)) FROM dbo.AIModelVersions WHERE Status=2 AND Id='$model';")" || return
    fi
    python3 "$RG_CODE/deploy/server/release_manifest.py" "$output" "$sha" "${RG_SOURCE_BRANCH:-deploy}" \
        "$RG_CODE/REVISION" "$RG_CONFIG/application.json" "$api" "$ai" "$minio" "$sql" \
        "$enabled" "$model_hash" "$model" "$before" "$after"
}
