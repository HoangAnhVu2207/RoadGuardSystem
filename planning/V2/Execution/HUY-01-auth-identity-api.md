# HUY-01 - Identity, session and account API

- Owner/branch: Huy / huy
- deliveryStatus: PARTIAL
- contractStatus: PROPOSED_DELTA
- implementationStatus: CURRENT_VERIFIED_FOR_EXISTING_SLICES
- verificationStatus: PARTIAL_FOCUSED_API_SQL
- dependencyType: contract
- sourceCheckpoint: HEAD d2338dcffd8838198c2b50ee369b9678e7e06690; canonical OpenAPI SHA-256 ADA7F48F522C0C0DBDACE21F483224A00FC4C76264A9A61D12F3E2211ECCE665

## Task goal
Expose the approved identity flow through Services, DTOs and API: login/refresh/logout, password recovery/change, reporter registration/OTP, invitations, me/profile and account administration. Preserve stable errors, actor scope, privacy and current compatible clients.

## Business context
Reporter uses one-time email verification, not OTP on every login. Session policy differs by client and remains a compatibility decision. Identity endpoints are prerequisites for all project authorization.

## Operation trace
V2-P1-001..015. Historical operation owner fields do not override ADR 006; Huy owns Service/DTO/API behavior.

## Sources to read
Read the matching operation cards, docs/design requirements FR-01..03, BR identity rules, ERD_Auth_Identity, DD 3.1, Auth_Permission_Model 5.1/5.4/5.6, API Specification, Error Handling, SQ-06, ADR 002/006 and ANH-01 handoff.

## In scope
- Write a 5-8 line contract and current-versus-approved matrix for each operation.
- Map repository facts to DTOs and stable ProblemDetails without exposing OTP/password/token material.
- Verify wrong actor/scope, expired/replayed OTP, disabled account, stale version and idempotent replay behavior.
- Update API.http and existing Postman requests when wire behavior changes; add focused API/unit tests and real HTTP smoke.
- Record missing persistence facts to ANH-01; consume only VERIFIED facts.

## Out of scope
- BusinessObjects, repositories, EF, migrations, live DB, SMTP provider, SSO, new auth policy or web-cookie rollout without decision.
- Rewriting historical operation status or claiming external email delivery from a fake sender.

## Exact files and hotspots
Services/Implementations/Authentication, Services/Interfaces/Authentication, DTOs/Identity/Authentication, API authentication controllers, API.http, Postman and API/unit tests. Shared auth middleware, DI, errors and OpenAPI require one writer.

## Stop conditions
Stop on contract-lock mismatch, repository fact conflict, transport incompatibility, missing authorization rule or external SMTP gate. Mark PARTIAL with exact operations; do not silently choose a new status/header/error.

## Verification and acceptance
Build Services/API/ApiTests fresh. Run focused identity tests with a non-empty filter, then real HTTP smoke checking status, body, headers and durable session/account result. Static Postman validation is separate. Acceptance requires ANH-01 handoff VERIFIED or NO_CHANGE_NEEDED and every changed operation evidenced.

## Source evidence - 2026-09-30 receiver review

| Label | Exact source / heading or ID | Evidence used |
|---|---|---|
| CURRENT_VERIFIED | `RoadGuardSystem.Services/Implementations/Authentication/AuthService.cs` (`LoginAsync`, `RefreshAsync`, `LogoutAsync`, `ChangePasswordAsync`) | Service consumes identity repository facts for session issue, refresh rotation/family replay, invalidation, stale/idempotency outcomes; it does not use EF/HTTP. |
| CURRENT_VERIFIED | `RoadGuardSystem.Services/Implementations/Authentication/IdentityOnboardingService.cs` and `RoadGuardSystem.Services/Implementations/Identity/IdentityV2Service.cs` | OTP/invitation/account/profile result mapping is service-owned; sensitive material is not mapped into response DTOs. |
| CURRENT_VERIFIED | `RoadGuardSystem.API/Controllers/AuthController.cs`, `ReporterRegistrationsController.cs`, `InvitationsController.cs`, `MeController.cs`, `ProfileController.cs`, `UsersController.cs`, `AdminUsersController.cs` | Existing controllers bind HTTP, extract actor, call service interfaces and map stable ProblemDetails/status; no Controller -> Repository access. |
| CURRENT_VERIFIED | `tests/RoadGuardSystem.ApiTests/Authentication/V2AuthenticationFlowTests.cs`, `Identity/V2IdentityEndpointContractTests.cs`, `tests/RoadGuardSystem.UnitTests/Authentication/*`, `tests/RoadGuardSystem.IntegrationTests/Identity/*` | Fresh API identity 4/4, Unit/architecture 162/162, SQL identity 38/38 on this checkout. |
| TARGET_DOCUMENTED | `planning/V2/Person_1/V2-P1-001_login.md` through `V2-P1-015_getAccount.md`; FR-01..03; BR-02/29/45; SQ-06; ADR 002/006 | Operation routes, actor/scope, one-time reporter OTP and server-session authority are target contract references, not proof of runtime. |
| NOT_ENABLED | `docs/design/**` and `RoadGuardSystem.Repositories/Migrations/20260929184004_Baseline20260930.cs` | `docs/design` and candidate baseline are absent from `d2338dc`; no clean-branch migration/rollout proof is claimed. |

## Contract checkpoint for routes actually consumed

1. `POST /api/v1/auth/login`: public actor; validate email/password; success 200 `AuthTokenResponseDto` with actor/version; stable invalid-credential/validation/conflict ProblemDetails; repository issues a durable session/hashed refresh token; no password/token logging or leakage.
2. `POST /api/v1/auth/refresh`: public refresh credential; validate token; success 200 rotated token pair; invalid/expired/revoked/replay map to stable 401 errors; repository rotation is single-use and family replay revokes; no bearer-only inference.
3. `POST /api/v1/auth/logout` and `/change-password`: authenticated actor/session; require idempotency where declared; success 204; stale/reused key maps to conflict; repository revokes/invalidate credentials atomically; actor is taken from claims and sensitive input is never echoed.
4. `POST /api/v1/auth/password-recovery-requests`: public email input; normalize and validate without account enumeration; success 202; persistence creates recovery fact; delivery/external SMTP is not claimed; response contains no secret or account existence signal.
5. `POST /api/v1/auth/reporter-registrations`, `/verify`, `/resend`: public email-verification flow; validate idempotency/OTP payload; success is 202/200 per current controller; replay/expired/attempt/cooldown facts map to stable errors; OTP remains one-time email verification, not login OTP; provider delivery is gated.
6. `POST /api/v1/invitations` and `/accept`: authenticated creator or public token accept; enforce actor/project role scope in Service; success 201/200 with invitation or token response; replay/conflict/not-found map to stable errors; repository owns hash/replay/idempotency facts; invitation token/password are not exposed.
7. `GET/PATCH /api/v1/me`, `GET/PUT /api/v1/profile`, `GET/PATCH /api/v1/users/{userId}`, and admin password reset: actor/resource scope is service policy; success DTOs expose safe profile/account projection and opaque version; stale If-Match/idempotency maps to 412/409; repository supplies rowversion/status facts; cross-account and secret data remain private.

## Current versus target and outcome

Existing identity routes consume the ANH-01 repository result semantics without a repository contract change. The operation cards remain `PROPOSED_CONTRACT` or historical `DONE`; only current source/tests are runtime evidence. Real hosted HTTP/SMTP smoke and clean-baseline rollout are not verified in this receiver pass, so HUY-01 remains `PARTIAL`.

## Completion history

### 2026-09-30 - PARTIAL (ANH-01 receiver processing)

- Scope/result: Received ANH-01 and confirmed existing Service/DTO/API mapping uses repository facts for session rotation/replay, OTP, invitation, password and account outcomes. No production code change was necessary.
- Files: `planning/V2/Execution/HUY-01-auth-identity-api.md`; receiver ledger row in `planning/CROSS_OWNER_HANDOFFS.md`. No Services/DTOs/API source changed.
- Acceptance: Actor scope, stable error mapping, rowversion/idempotency facts and sensitive-data boundaries are bounded to current source. SMTP/external delivery and clean migration rollout remain unverified.
- Verification: sequential `dotnet build RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj -nologo -v q -clp:ErrorsOnly`, `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly`, `dotnet build tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj -nologo -v q -clp:ErrorsOnly`, and `dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj -nologo -v q -clp:ErrorsOnly` PASS; `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~V2AuthenticationFlowTests"` PASS 4/4; `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-build --nologo -v q --filter "FullyQualifiedName~Authentication|FullyQualifiedName~Identity|FullyQualifiedName~Surveys|FullyQualifiedName~Notifications|FullyQualifiedName~Processing|FullyQualifiedName~Files|FullyQualifiedName~Inspections|FullyQualifiedName~Projects|FullyQualifiedName~Architecture"` PASS 162/162; `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~IdentityPersistence"` PASS 38/38. SQL fixture evidence is persistence corroboration, not external SMTP proof.
- Postman/static: collection schema v2.1, 83 requests, 77 collection variables, 70 environment variables, 0 unresolved references and 0 duplicate request names; no request changed.
- Handoff outcome: `PROCESSED / NO_CHANGE_NEEDED` for the current identity consumer seam; ANH-01 remains `PROCESSED`, not `VERIFIED`, because receiver integration and external smoke gates are incomplete.
- Side effects/risk: no package, migration, schema, live data, provider, commit or push; missing `docs/design` and candidate baseline are recorded as `NOT_ENABLED`.

### 2026-09-30 - PARTIAL (forced password change request coverage)

- Scope/result: Added the existing `POST /api/v1/auth/forced-password-change` route to `RoadGuardSystem.API/RoadGuardSystem.API.http` and the Postman collection. The request uses the current DTO (`username`, current/new/confirm password and operation ID) and is guarded against seeded fixture accounts and unattended destructive runs. No Service/DTO/Repository behavior changed.
- Verification: Postman static parse PASS (v2.1, 84 requests, 0 unresolved references, 0 duplicate names/IDs); alignment guards PASS. Existing focused authentication/API/unit/identity persistence evidence remains valid because production source was unchanged; no SMTP or hosted HTTP claim is added.
- Acceptance/status: forced-password-change remains covered as a current identity route with 204/idempotent semantics; external delivery and clean-baseline rollout remain unverified. Task stays `PARTIAL`.
- Side effects/risk: API example, collection and README documentation only; no package, migration, schema, live data, provider, commit or push.

- Full-solution gate after this slice: `dotnet test RoadGuardSystem.slnx --nologo -v minimal` passed Unit `170/170`, API `117/118` with one optional MinIO skip, and Integration `317/317`; no failures. This does not replace the missing hosted HTTP/SMTP and clean-baseline evidence.



