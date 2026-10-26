# Local browser HTTPS profile

Use both files from the same extracted candidate on **every** Compose invocation:

```sh
docker compose -p roadguard-fe -f compose.yaml -f compose.https.yaml up -d --build
docker compose -p roadguard-fe -f compose.yaml -f compose.https.yaml ps -a
```

API: `https://localhost:18443`; readiness: `/ready`. HTTP 18081 remains available for bearer clients and internal health checks. Browser cookie clients must use HTTPS. The profile adds a private named TLS volume; it preserves SQL, objects, state and data-protection keys. Existing certificates are checked for expiration and matching private key, never silently regenerated. An expired or incomplete pair requires an explicit renewal procedure; do not delete the volume to bypass the check.

The profile's `RG_FE_HTTPS_ORIGINS` defaults to explicit HTTPS loopback origins on ports 5173/8081. Set it before recreation to the exact local FE origins; wildcards, remote hosts, paths and trailing slashes are rejected. This development-only override does not rewrite persistent state or change production CORS. Use the **same hostname and scheme** for FE and API (for example HTTPS localhost on different ports). Mixing localhost and 127.0.0.1, or HTTP FE with HTTPS API, changes browser site semantics. Cookies remain Secure, HttpOnly and SameSite=Lax; CSRF remains mandatory.

The browser must trust the local API certificate. Export **only the public certificate**:

```sh
docker compose -p roadguard-fe -f compose.yaml -f compose.https.yaml cp tls-init:/tls/localhost.crt ./roadguard-local.crt
```

Review its SHA-256 fingerprint before an operator chooses to trust it in their local development browser/OS. This task does not install a Windows trust root. Never export the private key or put it in Git. Configure the separate Vite repository's existing HTTPS certificate mechanism independently; this package does not modify FE repositories. Expo bearer/device networking remains separate and loopback is not reachable from a physical phone without an explicitly designed local networking setup.

Browser sequence: GET `/api/v1/auth/web/csrf` with `credentials: include` → POST `/auth/web/login` with credentials and returned `X-CSRF-TOKEN` → GET `/auth/web/session` → refresh CSRF after identity changes → POST `/auth/web/logout` with CSRF and a unique `Idempotency-Key`. Paths after the first example have prefix `/api/v1`. Never expose credentials or CSRF values in logs. Only routes explicitly eligible for web cookies accept that transport; do not assume all bearer endpoints accept cookies.

Task34 continuation executed a real headless Edge context against a separate loopback HTTPS page and the Docker API: login missing-CSRF403, CSRF200, login200, Secure/HttpOnly/Lax cookie, authenticated session200, logout missing-CSRF403, valid logout204, session401, unapproved CORS origin denied. Nine assertions PASS. Certificate verification was bypassed **only in that disposable test context**. Actual Vite/Expo application E2E and operator-installed certificate trust remain NOT_RUN.
