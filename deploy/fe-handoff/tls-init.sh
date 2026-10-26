#!/bin/sh
set -eu
umask 077
if [ -e /tls/localhost.crt ] || [ -e /tls/localhost.key ]; then
    test -s /tls/localhost.crt && test -s /tls/localhost.key
    openssl x509 -in /tls/localhost.crt -checkend 0 -noout >/dev/null
    openssl x509 -in /tls/localhost.crt -checkhost localhost -noout >/dev/null
    cert_key=$(openssl x509 -in /tls/localhost.crt -pubkey -noout | openssl sha256)
    private_key=$(openssl pkey -in /tls/localhost.key -pubout | openssl sha256)
    test "$cert_key" = "$private_key"
else
    test -z "$(ls -A /tls)"
    openssl req -x509 -newkey rsa:2048 -nodes -days 365 \
      -subj /CN=localhost -addext 'subjectAltName=DNS:localhost,IP:127.0.0.1,IP:::1' \
      -keyout /tls/localhost.key -out /tls/localhost.crt 2>/dev/null
fi
chown 1654:1654 /tls/localhost.key
chmod 400 /tls/localhost.key
chmod 444 /tls/localhost.crt
echo 'Local TLS certificate ready; private key omitted.'
