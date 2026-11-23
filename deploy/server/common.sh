#!/bin/bash
set -euo pipefail
readonly RG_REPOSITORY=/opt/roadguard/repository.git
readonly RG_RELEASES=/opt/roadguard/releases
readonly RG_CONFIG=/etc/roadguard
readonly RG_DATA=/srv/roadguard
readonly RG_AI_SUBMODULE=AI/roadguard-ai-engine
readonly RG_AI_MODEL_NAME=RoadGuard_YOLO11_UAVPDD2023_FT95_v1.pt
readonly RG_AI_MODEL_SHA256=bc14e1f0378310640d0e04583982ebb377475211d6d42837f2db494e32ed95a0
export COMPOSE_PROJECT_NAME=roadguard-server

require_root() {
    if [ "$(id -u)" -ne 0 ]; then echo 'Run this command using sudo.' >&2; exit 1; fi
}

extract_release() {
    local revision="$1" destination="$RG_RELEASES/$1" ai_sha ai_url ai_path
    [[ "$revision" =~ ^[0-9a-f]{40}$ ]] || { echo 'Invalid release revision.' >&2; return 1; }
    if [ ! -d "$destination" ]; then
        mkdir -m 755 "$destination"
        git --git-dir="$RG_REPOSITORY" archive "$revision" \
            .dockerignore Directory.Build.props global.json \
            RoadGuardSystem.API RoadGuardSystem.BusinessObjects RoadGuardSystem.DTOs \
            RoadGuardSystem.Repositories RoadGuardSystem.Services contracts/ai/fixtures/anh02 deploy/server \
            | tar --no-same-owner -xf - -C "$destination"
        # git archive preserves the gitlink only; materialize the pinned AI tree explicitly.
        ai_sha="$(git --git-dir="$RG_REPOSITORY" ls-tree "$revision" "$RG_AI_SUBMODULE" | awk '$2 == "commit" {print $3}')"
        [[ "$ai_sha" =~ ^[0-9a-f]{40}$ ]]
        ai_url="$(git --git-dir="$RG_REPOSITORY" show "$revision:.gitmodules" | awk '/path = AI\/roadguard-ai-engine/{getline; sub(/^.*url = /, ""); print}')"
        test "$ai_url" = "https://github.com/khoa025ne/roadguard-ai-engine.git"
        ai_path="$destination/$RG_AI_SUBMODULE"
        rm -rf -- "$ai_path"
        mkdir -p "$(dirname "$ai_path")"
        git clone --filter=blob:none --no-checkout "$ai_url" "$ai_path" >/dev/null
        git -C "$ai_path" fetch --depth=1 origin "$ai_sha" >/dev/null
        git -C "$ai_path" checkout --detach "$ai_sha" >/dev/null
        printf '%s\n' "$revision" > "$destination/REVISION"
    fi
    test "$(cat "$destination/REVISION")" = "$revision"
}

ai_configured() {
    test -s "$RG_CONFIG/ai-token" &&
        test -s "$RG_DATA/ai/models/ft95.pt" &&
        test "$(sha256sum "$RG_DATA/ai/models/ft95.pt" | awk '{print $1}')" = "$RG_AI_MODEL_SHA256"
}

prepare_ai_assets() {
    local source="$RG_CODE/$RG_AI_SUBMODULE/weights/trained/$RG_AI_MODEL_NAME"
    local destination="$RG_DATA/ai/models/ft95.pt"
    test -f "$source"
    test "$(sha256sum "$source" | awk '{print $1}')" = "$RG_AI_MODEL_SHA256"
    install -d -m 750 -o 10001 -g 10001 "$RG_DATA/ai" "$RG_DATA/ai/worker" "$RG_DATA/ai/models"
    install -d -m 770 -o 1654 -g 10001 "$RG_DATA/ai/be-spool"
    if [ -e "$destination" ]; then
        test "$(sha256sum "$destination" | awk '{print $1}')" = "$RG_AI_MODEL_SHA256"
    else
        install -o 0 -g 10001 -m 640 "$source" "$destination"
    fi
    if [ ! -e "$RG_CONFIG/ai-token" ]; then
        umask 077
        openssl rand -hex 48 > "$RG_CONFIG/ai-token"
        chown 0:10001 "$RG_CONFIG/ai-token"
        chmod 640 "$RG_CONFIG/ai-token"
    fi
    test -s "$RG_CONFIG/ai-token"
}

compose() {
    local profile=()
    if ai_configured; then
        profile=(--profile ai)
        export RG_AI_ENABLED=true RG_AI_RELEASE="${RG_AI_RELEASE:-$RG_RELEASE}"
    else
        export RG_AI_ENABLED=false
    fi
    docker compose "${profile[@]}" --project-directory "$RG_CODE/deploy/server" --env-file "$RG_CONFIG/server.env" \
        -f "$RG_CODE/deploy/server/compose.yaml" "$@"
}
