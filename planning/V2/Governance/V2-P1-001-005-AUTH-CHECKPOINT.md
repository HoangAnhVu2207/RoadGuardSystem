# V2-P1-001..005 Auth Delivery Checkpoint

- Owner: anh
- Branch: `anh`
- Baseline before auth delivery: `49c7eae`
- Alignment baseline already present: `V2-ALIGN-2026-09-28`, canonical OpenAPI SHA `65a92d0e872d49f7abe48732068a4f63aa6728aa320879ba9e83c1ee8c8f32ab`
- Delivery authorization: user-approved V2-P1-001..005 scope on 2026-09-29.
- Existing alignment changes are preserved and must not be treated as auth implementation evidence.
- Auth-delivery changes begin after this checkpoint and are limited to the five auth slices, their direct tests, HTTP/Postman requests, and task evidence.
- No commit, push, live migration, live data reset, package upgrade, or runtime implementation of unrelated proposed contracts is authorized.

## Current/Target Contract Card

| Operation | Current source | V2 target | Evidence required |
|---|---|---|---|
| login | `AuthController.Login` -> `IAuthService` -> identity repository; email and `TokenPair` shape already present | email login, role mapping, stable auth errors, durable session/token issuance | API build, focused auth tests, real smoke with persisted session |
| refreshTokens | `/auth/refresh`; rotation and replay paths already present | one-time rotation, expiry/revocation/replay fail-closed, concurrent winner | focused refresh tests plus SQL concurrency and smoke |
| logout | authenticated `/auth/logout`, actor/session claims and idempotency path present | same actor/session only, family revocation, idempotent 204 | focused tests plus repeated-key smoke and durable revocation |
| requestPasswordRecovery | neutral service result and durable `PasswordRecoveryRequest` path present | neutral 202, `Location`, no account enumeration | known/unknown/invalid email tests and durable readback |
| changePassword | authenticated `/auth/change-password`, idempotency and session revocation path present | current-user binding, policy, idempotency, revoke all sessions | focused tests plus old/new login and session effects |

## Contract (implementation card)

1. Routes are `POST /api/v1/auth/login`, `/refresh`, `/logout`, `/password-recovery-requests`, and `/change-password`.
2. Login and refresh return the existing `TokenPair`; logout/change-password return 204 without a body.
3. Login/recovery are public; logout/change-password require the authenticated actor; refresh accepts only a valid unrevoked token.
4. Required bodies are email/password, refreshToken, forgot-password email, and current/new password as defined by the V2 DTOs.
5. Idempotency-Key is required for logout and change-password; same key/payload replays, changed payload conflicts.
6. Refresh rotation is single-use; expiry, revocation, replay, and concurrent races fail closed and revoke the family where required.
7. Recovery is neutral and durable, returning 202 plus Location without revealing account existence.
8. No migration is applied to a live database; a new migration is allowed only if source evidence proves the contract requires it.

## Change separation

- Alignment-only files: all pre-existing modified/untracked documentation, governance, contract snapshots, generated artifacts, and guards before this checkpoint.
- Auth-delivery files: production/test/Postman/HTTP files changed after this checkpoint and the five task evidence sections.
- Verification must report these two groups separately.

## Verification completion (2026-09-29)

- The three initial Identity failures were stale assertions expecting four roles. `IdentityRoleSeedStep` and the successful Seeder CLI output prove five canonical roles, including `REPORTER`; only integration test names/counts were corrected.
- Fresh IntegrationTests build passed with 292 existing warnings and 0 errors. Identity breadth passed `68/68` after the test-only correction.
- External-host smoke passed on `http://127.0.0.1:5127` using Development migrations/initializer/seed against isolated database `RoadGuard_AuthSmoke_20260929`; the database was dropped after the run.
- Smoke evidence: login `200` + TokenPair/role, refresh `200` + new token, replay `401` + durable session revocation, logout `204` + durable revocation, recovery known/unknown `202` + Location and empty body, change-password `204` + session revocation + old password `401` + replacement password `200`.
- Postman idempotency variables are distinct (`logoutKey`, `changePasswordKey`); retries retain the same key/payload and new operations use a new key. No token/password was persisted or printed.
