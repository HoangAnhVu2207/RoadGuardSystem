# V2-P1-006..015 code checkpoint

## Metadata

- Owner/branch: `anh` / `anh`
- Base revision: `561dd0a8aace5b4edbb228616a3b9264a941c036`
- Canonical contract SHA-256: `dd991f20c9a27a564bf37c06bba63776424b770f3abfa54ec2bef9d1a1e678bd`
- Working state: `UNCOMMITTED`
- Delivery: code complete; focused onboarding/seed verification partial; external/manual email smoke not run
- Shared hotspots reserved for this batch: identity onboarding service/repository, identity V2 service/repository, identity DI, canonical OpenAPI, HTTP/Postman, V2 task manifest. No concurrent writer was used.

## Authorized slices and dependencies

1. Reporter onboarding: 006 -> 007/008.
2. Staff invitation: 010 -> 009.
3. Self profile: 011 -> 012.
4. Administrative account: 015 -> 013/014.

## Contract and source comparison

| Task | Route / actor | Input and success | State, persistence and stable handling | Current implementation |
|---|---|---|---|---|
| 006 `registerReporter` | `POST /api/v1/auth/reporter-registrations`; public | email/password/displayName/reporterType + Idempotency-Key; `202` RegistrationIntent + Location | Pending Reporter, hashed OTP, durable idempotency, no secret echo; valid email is provider-neutral | Reused controller/DTO/repository/entity/mapping/DI; changed `IdentityOnboardingService.RegisterReporterAsync` to remove Gmail-only policy |
| 007 `verifyReporterOtp` | `POST /api/v1/auth/reporter-registrations/verify`; public intent holder | intentId/otp + Idempotency-Key; `200` TokenPair | OTP expiry/attempt limits, one activation/session, replay-safe | Reused `ReporterRegistrationsController.Verify`, onboarding service/repository and existing schema |
| 008 `resendReporterOtp` | `POST /api/v1/auth/reporter-registrations/resend`; public intent holder | intentId + Idempotency-Key; `202` RegistrationIntent + Location | 60s cooldown, at most 3 committed resends per 15 minutes, rotated hashed OTP, replay-safe | Extended onboarding options/interface/repository using existing durable idempotency records; no schema change |
| 009 `acceptInvitation` | `POST /api/v1/invitations/accept`; public token holder | invitationToken/displayName/password + Idempotency-Key; `200` TokenPair | One-time unexpired invitation, account/membership/session creation in repository transaction, no token persistence in clear | Reused `InvitationsController.Accept`, onboarding service/repository and existing schema |
| 010 `createInvitation` | `POST /api/v1/invitations`; active Supervisor | email/role/projectIds + Idempotency-Key; `201` Invitation + Location/ETag | Role/project checks, token hash, expiry, delivery, durable idempotency | Removed the undocumented create-time displayName from DTO/controller/service/HTTP/Postman; acceptInvitation remains the source of the final display name |
| 011 `getMe` | `GET /api/v1/me`; authenticated self | bearer identity; `200` Actor | Read-only no-tracking profile projection; role mapping includes internal `DRONE_OPERATOR` -> API `OPERATOR` | Reused `MeController.Get`, `IdentityV2Service.GetMeAsync`, `IIdentityRepository.GetUserProfileAsync` |
| 012 `updateMe` | `PATCH /api/v1/me`; authenticated self | displayName + Idempotency-Key + strong If-Match; `200` Actor | Atomic profile update, idempotent replay/conflict, `428` missing and `412` stale version | Reused `MeController.Update`, `IdentityV2Service.UpdateMeAsync`, identity repository atomic update |
| 013 `adminResetPassword` | `POST /api/v1/users/{userId}/password-reset`; Supervisor | temporaryPassword/reason + Idempotency-Key; `204` | Replaces hash, sets forced change, revokes all target sessions, audit/idempotency; password is not returned | Reused `UsersController.ResetPassword`, `IdentityV2Service.ResetPasswordAsync`, repository reset transaction |
| 014 `updateAccount` | `PATCH /api/v1/users/{userId}`; Supervisor | status/role/reason + Idempotency-Key + strong If-Match; `200` Actor | Atomic status/role change, audit/idempotency/session revocation, `428`/`412`; D06 rescue flow remains a separate capability | Reused the existing slice and added API `REPORTER` -> internal Reporter role parsing |
| 015 `getAccount` | `GET /api/v1/users/{userId}`; Supervisor | target userId; `200` Actor + ETag | Authorized no-tracking projection; `403` wrong role, `404` missing | Reused `UsersController.Get`, `IdentityV2Service.GetAccountAsync`, identity repository projection |

## Changed artifacts

- `RoadGuardSystem.Services/Implementations/Authentication/IdentityOnboardingService.cs`: provider-neutral Reporter email validation.
- `RoadGuardSystem.Services/Options/IdentityOnboardingOptions.cs`, onboarding repository interface/implementation: enforce 3 resends per 15-minute window using existing idempotency records.
- `IdentityOnboardingRepository`: translate concurrent resend/verify/create/accept persistence races to replay/conflict outcomes without duplicate effects.
- `CreateInvitationRequestDto`, invitation controller/service interface/service, API HTTP, Postman and focused test source: remove create-time `displayName` not present in the approved schema.
- `RoadGuardSystem.Services/Implementations/Identity/IdentityV2Service.cs`: accept `REPORTER` in account role updates.
- `docs/diagram/V2/05_Technical/openapi.yaml`: 006 summary/description/email rule aligned with D25.
- `docs/diagram/V2/08_Delivery/manifest.json`: hashes synchronized for changed contract/docs plus two pre-existing stale CI-script entries; script contents were not changed.
- `docs/postman/RoadGuardSystem-V2.postman_collection.json`: 006 validation example now uses a syntactically invalid email; all ten runtime requests already existed and were retained.
- `RoadGuardSystem.API/RoadGuardSystem.API.http`: all ten requests already existed; no change required.
- Task files 006..015 and `planning/V2/task_manifest.json`: implementation/delivery evidence synchronized.

## Development/Postman fixture addendum - 2026-09-29 11:22 +07:00

- Reused the existing `DbInitializer`; no second initializer was created.
- Added `PostmanScenarioSeedStep` after role/device/account steps. It creates a deterministic Development-only project graph: memberships, handover, road section/current version, segments, survey plan/request/operator assignment, completed survey, defect and repair-crew inspection assignment.
- The step is idempotent and never creates `ReporterRegistrationIntent`, `StaffInvitation`, confirmed Reporter accounts or email delivery effects. Real recipient addresses live only in the ignored `docs/postman/RoadGuard.local.private.postman_environment.json`.
- Added committed placeholder variables for `reporterRegistrationEmail`, `otpResendEmail`, `invitationEmail`, `recoveryEmail` and backups. Requests that can send real email require an explicit `manualEmailScenario` gate and are skipped by default.
- Added `RoadGuardSystem.API/appsettings.Development.local.example.json`; the committed Development config no longer contains an SMTP sender address. SMTP credentials remain local-only.
- Updated Postman/HTTP fixtures and `docs/postman/README.md` with precondition queries, first-time/retry rules, manual OTP/token entry and explicit fake-sender versus real-delivery evidence.

## Side effects and remaining gates

- Migration/schema/package/SDK/data/external effects: none. A migration was considered, but the final rate-limit design reuses existing persistence and requires no schema change; no migration file was created or applied.
- No migration was created or applied; no database was started or changed.
- Build evidence: `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj --no-restore --nologo -v q -clp:ErrorsOnly` PASS (0 errors, 0 reported warnings).
- Focused SQL evidence: two selected seeding tests PASS 2/2, proving DI order, repeated-run idempotency and no onboarding recipient/invitation creation.
- Focused API evidence: `V2IdentityOnboardingFlowTests` PASS 3/3 with `CapturingIdentityMessageSender`, covering registration/verify, OTP resend invalidating the old code, and invitation create/accept/replay. No SMTP connection occurred.
- Static Postman evidence: collection/environment JSON parse PASS; 52 YAML files parse PASS; required manual-email guards and committed empty recipient placeholders PASS; tracked real-recipient scan PASS.
- Alignment guard remains FAIL outside this slice: `V2-P2-001 missing deliveryStatus`. No Person 2 task was changed.
- Tasks 006..010 remain `PARTIAL` with `verificationStatus=PARTIAL_FOCUSED_AUTH`; tasks 011..015 keep their prior verification state. Real SMTP delivery, mailbox receipt, manual OTP verify/login, invitation accept and external smoke remain NOT_RUN.
