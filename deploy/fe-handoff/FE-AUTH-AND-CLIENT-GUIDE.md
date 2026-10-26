# FE authentication and client guide — preparation

Status: **NOT_VERIFIED / FE_INTEGRATION_PREVIEW**. This guide describes inspected source. F04 still requires examples executed against the final immutable candidate; parent R1/R2/R3 gates remain open. No FE stack or native-device integration is assumed.

Use the exact artifact, base URL and trusted HTTPS certificate selected by the final handoff. Current preview environment instructions are in [FE-HTTPS-LOCAL.md](FE-HTTPS-LOCAL.md). Do not mix `localhost` and `127.0.0.1`: they are different cookie hosts. The FE origin must be explicitly allowed by CORS, and exposed headers must be verified on that environment. Do not disable certificate validation, CSRF or browser security to make a request succeed.

## Browser Web session

All paths below begin `/api/v1`. Use `credentials: 'include'` and `cache: 'no-store'`. Cookies are Secure, HttpOnly and SameSite=Lax; JavaScript must not read, persist or synthesize them. Configure FE and API to use HTTPS on the same site, with the precise FE origin permitted by the local profile.

1. `GET /auth/web/csrf` returns `{requestToken, headerName}` and sets the antiforgery cookie. Keep the request token only in memory.
2. `POST /auth/web/login` sends JSON `{email,password}` with the returned `X-CSRF-TOKEN`. Login itself requires CSRF even before authentication. Obtain credentials from the isolated environment's approved credential procedure; do not put them in source, URLs or reports.
3. Bootstrap CSRF again after login, because the token must match the authenticated identity. Read `GET /auth/web/session` for the current user/session view. Do not expect an access token from Web login.
4. Use cookie requests only for operations with approved Web-cookie support. Eligibility is operation-specific; it cannot be inferred from an `[Authorize]` requirement alone. Consult the operation catalog and actual transport evidence.
5. Supply CSRF on protected mutations. Keep `Idempotency-Key` and `If-Match` independent of CSRF. Read-only GET requests do not require a CSRF header.
6. `POST /auth/web/renew` uses the protected renewal cookie and CSRF, with no Authorization header. It renews the cookie ticket only while current server-side session authority remains valid. Bootstrap CSRF for the current identity before a renewal attempt. Do not repeatedly retry a rejected renewal.
7. `POST /auth/web/logout` requires CSRF and a nonempty `Idempotency-Key`. On success204, discard in-memory CSRF/user state. Server revocation governs future requests even when a client retains an old cookie.

The Web response contains `user` (`id`, `displayName`, `role`, `version`), `mustChangePassword`, `issuedAt`, nullable `absoluteExpiresAt`/`idleExpiresAt`, `renewable`, and `ticketLifetimeSeconds`. Cookie ticket lifetime is43200 seconds. A ticket lifetime does not override session revocation, actor suspension, role changes or project authority.

The [browser example](examples/roadguard-web-client.mjs) implements explicit CSRF/login/session/renew/logout calls and protected JSON requests. Serve it through the FE development origin as a module. It is browser code; Node fetch does not provide the browser cookie jar automatically. Candidate runtime verification is pending.

An Authorization header takes precedence over a Web cookie. An invalid Bearer credential must not fall back to a valid cookie. Different actors in simultaneous Bearer/cookie credentials are rejected. Keep separate clients for Web cookie and Android Bearer authentication.

## Android / Bearer

Use `POST /auth/android/login` with JSON `{email,password}` for the Android transport. Retain the returned access/refresh credentials only through the platform's protected credential storage and the application's approved lifecycle. Never put tokens into query strings, logs or issue reports. The actual native secure-storage implementation is outside this Backend-local verification.

Use `Authorization: Bearer <accessToken>` over validated TLS. `POST /auth/android/refresh` sends `{refreshToken}`. Keep refresh serialization within one session; do not issue competing refresh requests. Consult the exact refresh operation for optional operation-key validation and replay semantics. Generic `/auth/login` and `/auth/refresh` are separate existing routes; do not interchange transport credentials without approved evidence. Authenticated `POST /auth/logout` requires its operation key and revokes the identified session.

Role and project IDs are routing inputs, not authority. The server checks current actor/session/role and the exact resource/project relationship. Do not reuse a successful response from another project or assume Supervisor always needs ProjectMember membership. See the approved item-scoped B2 contract for global Supervisor access.

For uploads, use the exact upload initiation/parts/completion contract from [OpenAPI](openapi.json). A successful object PUT or multipart transfer is not Backend evidence verification. Wait for the server verification state before passing a file as evidence. Re-obtain a private signed URL through the authorized operation when needed; existing issued private URLs can remain usable until their300-second expiry after revocation. AI frame access uses authorized files metadata/content and does not mint a new signed URL. Never cache private content in an anonymous/shared cache.

Offline replay must use the approved signed envelope, device/grant scope and original immutable operation identity. A stored command is not current permission. Follow W09's exact admission window and recovery evidence; do not invent client-side authority, backdate a command or generate a new key merely to bypass a conflict.

## Mutation and response handling

| Situation | Client action |
|---|---|
| Network interruption after a mutation | Keep the same operation key and original command. Retry only when the operation's approved replay contract permits it. Do not automatically retry every mutation. |
| Successful detail with strong `ETag` | Retain the exact quoted header. Send it as `If-Match` only for the mutation that requires that resource version. |
| `412 concurrency_conflict` | Refresh the resource and ask the user to reconcile the change. Do not silently overwrite or strip the precondition. |
| `428` | Supply the required precondition/header from that exact operation contract; some428 responses have no JSON body. |
| `409` | Inspect the operation's actual error code. An idempotency collision and a business transition conflict require different actions. |
| `401` | Stop protected operations, clear stale client identity and use the appropriate explicit login/renew path. Avoid infinite renewal/replay loops. |
| `403` | Do not retry unchanged. Distinguish `csrf_failed` from current role/project denial. Refreshing CSRF cannot grant authority. |
| `404` | Treat as unavailable. A concealed resource or withdrawn public publication is intentionally indistinguishable from missing data. |
| `400` or another documented validation status | Show the contract's error and correct the input. Do not assume every endpoint uses422. |
| Successful204 | Do not parse JSON. Some error responses are empty as well; preserve HTTP status when there is no body. |
| Collection response | Treat `nextCursor` as opaque; pass it back unchanged and stop when null. Do not infer totals or decode it as a permission token. |

Error code names, status ordering, key limits and replay behavior vary by operation. Preserve exact field/schema definitions from the frozen OpenAPI. `ProblemDetails.instance` may echo the requested path; sanitize private identifiers when filing an issue. Retain `X-Correlation-ID` for diagnostics, without collecting raw credentials or sensitive payloads.

## Source and evidence boundaries

Inspected source: `WebAuthController`, `AuthController`, `WebCookieConfiguration`, `WebCookieRequestMiddleware`, and `AuthoritativeSessionValidator`. Related raw execution identities and source hashes are recorded per operation in [the exact dimension ledger](review-evidence/milestone2-r1-closure/EXACT-DIMENSION-ACCEPTANCE.json).

The original A1 replay defect and failures are retained in [A1-REPLAY-BOUNDARY-OPEN.md](review-evidence/milestone2-r1-closure/A1-REPLAY-BOUNDARY-OPEN.md). Owner approved identity-bound new receipts and fail-closed legacy409. Current source-matched regression evidence and remaining preview predicates are reconciled in [preview gate inputs](review-evidence/fe-preview-closeout/PREVIEW-GATE-INPUTS.json); the historical open note is not the current implementation verdict. FE must not mint service JWTs or invoke internal AI callbacks. A1, B2, C1, G02–G06 and API238 need their exact special-case documentation and final candidate verification. This guide does not close those dimensions or F04.
