#!/bin/sh
set -eu
# Only public trust material is installed; private keys stay in read-only mounts.
cp /run/tls/ca.crt /usr/local/share/ca-certificates/roadguard-server.crt
update-ca-certificates >/dev/null
exec setpriv --reuid=1654 --regid=10001 --clear-groups "$@"
