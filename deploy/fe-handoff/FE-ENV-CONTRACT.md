> Closeout current status: see [FE-CLOSEOUT-POLICY-CONTRACT.md](FE-CLOSEOUT-POLICY-CONTRACT.md). A1/B2/C1 exact policies are OWNER_CONFIRMED and implemented with the stated bounded evidence and remaining acceptance gaps. Earlier pending-policy/wire statements below are HISTORICAL. No independent release PASS or Git publication.

# FE environment contract

The kit generates secrets per instance in a private Docker state volume; no production credential is required. `.env` beside compose.yaml contains only optional non-secret port/origin choices. Defaults: RG_FE_API_PORT=18081, RG_FE_OBJECT_PORT=19001, RG_FE_MAIL_PORT=18026. Choose unique free ports in 1..65535. RG_FE_ORIGINS accepts comma-separated exact loopback origins, without wildcard, path or trailing slash.

Backend DNS is `sql`, `minio:9000`, `mailpit:1025`. Browser clients use localhost host ports, never internal Docker DNS. SQL exposes no host port and no external server option. MinIO remains private; object capability URLs use the configured host object port. Changing that port after initialization does not rewrite saved configuration.

Bearer API base is `http://localhost:<API_PORT>/api/v1`. Browser cookie base with compose.https.yaml is `https://localhost:<HTTPS_PORT>/api/v1` (18443 default); follow FE-HTTPS-LOCAL.md for certificate trust, HTTPS origins, credentials and CSRF. HTTPS profile origins can be overridden by RG_FE_HTTPS_ORIGINS without rewriting persistent state; the same strict loopback validation applies. Android Emulator/physical-phone access is an external networking gate: the kit binds loopback and capabilities embed localhost. Do not promise direct phone access or broaden bindings without an approved network profile.

Credentials/JWT keys persist in private state, key ring in the keys volume. Never paste account output, OTPs, tokens or signed URLs into evidence. Docker operators can access local secrets; host access controls remain necessary. Containers mount no host credential directories. Automated tests use separately owned disposable SQL; FE Session data is preserved.
