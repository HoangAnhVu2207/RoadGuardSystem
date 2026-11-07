#!/bin/bash
# Download this file at the exact reviewed deploy SHA, then execute with sudo.
set -euo pipefail
if [ "$(id -u)" -ne 0 ]; then echo 'Use sudo bash bootstrap.sh <40-character SHA> <LAN IPv4>' >&2; exit 1; fi
revision="${1:?Supply the reviewed deploy SHA}"
host="${2:?Supply the dedicated server LAN IPv4}"
[[ "$revision" =~ ^[0-9a-f]{40}$ ]] || { echo 'Invalid release SHA.' >&2; exit 1; }
python3 -c 'import ipaddress,sys; a=ipaddress.IPv4Address(sys.argv[1]); assert a.is_private and not a.is_loopback and not a.is_unspecified and not a.is_multicast' "$host"
for command in git python3 openssl docker flock curl tar; do command -v "$command" >/dev/null; done
docker info >/dev/null
docker compose version
if [ -e /opt/roadguard ] || [ -e /etc/roadguard ] || [ -e /srv/roadguard ]; then
    echo 'An existing installation was found. Bootstrap refuses overwrite; inspect it and use manage.sh instead.' >&2
    exit 1
fi
install -d -m 755 /opt/roadguard /opt/roadguard/releases
git clone --bare --depth 1 --single-branch --branch deploy --filter=blob:none \
    https://github.com/HoangAnhVu2207/RoadGuardSystem.git /opt/roadguard/repository.git
git --git-dir=/opt/roadguard/repository.git fetch --depth 1 origin "$revision"
release="/opt/roadguard/releases/$revision"
install -d -m 755 "$release"
git --git-dir=/opt/roadguard/repository.git archive "$revision" \
    .dockerignore Directory.Build.props global.json \
    RoadGuardSystem.API RoadGuardSystem.BusinessObjects RoadGuardSystem.DTOs \
    RoadGuardSystem.Repositories RoadGuardSystem.Services contracts/ai/fixtures/anh02 deploy/server \
    | tar --no-same-owner -xf - -C "$release"
printf '%s\n' "$revision" > "$release/REVISION"
python3 "$release/deploy/server/setup.py" --host "$host"
exec bash "$release/deploy/server/manage.sh" initialize
