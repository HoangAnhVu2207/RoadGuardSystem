# Authentication, OTP and session

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

## Registration and OTP

`POST /api/v1/auth/reporter-registrations` (API-010) is anonymous and requires `Idempotency-Key`; request schema is `RegisterReporterRequestDto`. The server returns an intent, not a logged-in Reporter. The Expo flow stores only the intent identifier and displays server cooldown/expiry. It must not store the password in AsyncStorage, SQLite, logs or analytics.

`POST /api/v1/auth/reporter-registrations/verify` (API-012) and `/resend` (API-011) are anonymous and each uses its own command key. Source options currently describe a 10-minute OTP, five attempts, 60-second resend cooldown and three resends per 15 minutes; these are source defaults/configuration, not client authority. SMTP delivery is `NOT_VERIFIED`. Verify may return a legacy compatibility credential; the handoff requires a fresh Android login before Reporter business calls. Do not send that legacy refresh credential to API-003.

## Android transport

- Login: API-002 `POST /api/v1/auth/android/login` with `LoginRequestDto`; the server decides the role. Accept only an authoritative `REPORTER` response for this workspace.
- Refresh: API-003 `POST /api/v1/auth/android/refresh` with the current `RefreshRequestDto`. Refresh tokens rotate and can be revoked as a family. A timeout is retried with the same operation key only when the contract provides one; never loop indefinitely.
- Logout: API-007 `POST /api/v1/auth/logout` with bearer and required `Idempotency-Key`; successful response is 204. Logout ends the server session but local pending drafts need an explicit product policy before deletion.
- Password: API-005 forced change can be required before business calls; API-004 changes an authenticated password; API-008 only requests recovery and does not prove self-service reset.

Access tokens are short-lived (the current source contract documents 900 seconds). Keep the access token in memory where possible and persist refresh/access secrets only in an Expo protected secret store such as SecureStore. Never use plain AsyncStorage, SQLite, crash reports or URLs. The exact Expo SDK/native keychain implementation is `PROPOSED_EXPO_UI`; native hardware protection and uninstall behavior are `NOT_VERIFIED_EXPO_E2E`.

## Session state machine

`SIGNED_OUT → OTP_PENDING → VERIFIED_NEEDS_LOGIN → AUTHENTICATED → REFRESHING → AUTHENTICATED`.

On 401, pause one request queue, perform one refresh, atomically replace both tokens, then replay requests whose payload and K are unchanged. If refresh is rejected or the account is revoked, clear session secrets and return to login. Do not treat local JWT decoding as permission. Do not mix Web cookies/CSRF with Android bearer calls. Server account, role, current session and resource ownership remain authoritative.

**Evidence:** [API-002](../../04_API-Contracts/02_Full-API-Catalog.md#api-002), [API-003](../../04_API-Contracts/02_Full-API-Catalog.md#api-003), [API-007](../../04_API-Contracts/02_Full-API-Catalog.md#api-007), [API-010](../../04_API-Contracts/02_Full-API-Catalog.md#api-010), [API-011](../../04_API-Contracts/02_Full-API-Catalog.md#api-011), [API-012](../../04_API-Contracts/02_Full-API-Catalog.md#api-012), `AuthController.cs`, `ReporterRegistrationsController.cs`, `AuthService.cs`. Backend static contract is `CURRENT_VERIFIED`; Android/SMTP/E2E is `NOT_VERIFIED_EXPO_E2E`.
