#!/bin/bash
set -euo pipefail
readonly RG_REPOSITORY=/opt/roadguard/repository.git
readonly RG_RELEASES=/opt/roadguard/releases
readonly RG_CONFIG=/etc/roadguard
readonly RG_DATA=/srv/roadguard
export COMPOSE_PROJECT_NAME=roadguard-server

require_root() {
    if [ "$(id -u)" -ne 0 ]; then echo 'Run this command using sudo.' >&2; exit 1; fi
}

extract_release() {
    local revision="$1" destination="$RG_RELEASES/$1"
    [[ "$revision" =~ ^[0-9a-f]{40}$ ]] || { echo 'Invalid release revision.' >&2; return 1; }
    if [ ! -d "$destination" ]; then
        mkdir -m 755 "$destination"
        git --git-dir="$RG_REPOSITORY" archive "$revision" \
            .dockerignore Directory.Build.props global.json \
            RoadGuardSystem.API RoadGuardSystem.BusinessObjects RoadGuardSystem.DTOs \
            RoadGuardSystem.Repositories RoadGuardSystem.Services contracts/ai/fixtures/anh02 deploy/server \
            | tar --no-same-owner -xf - -C "$destination"
        printf '%s\n' "$revision" > "$destination/REVISION"
    fi
    test "$(cat "$destination/REVISION")" = "$revision"
}

compose() {
    docker compose --project-directory "$RG_CODE/deploy/server" --env-file "$RG_CONFIG/server.env" \
        -f "$RG_CODE/deploy/server/compose.yaml" "$@"
}
