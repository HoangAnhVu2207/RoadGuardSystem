# ANH-01 - Identity, session and account persistence

- Owner/branch: Anh / anh
- deliveryStatus: DONE
- contractStatus: PROPOSED_DELTA
- implementationStatus: CURRENT_VERIFIED
- verificationStatus: PASS_FOCUSED_SQL_AUTH
- dependencyType: data-fixture
- sourceCheckpoint: base HEAD 4586c8caa5aa8439c1ea9f9e385a8ee59359f0bb; current HEAD 15444975988f77f585e7350a1467668781aad120; canonical OpenAPI SHA-256 ADA7F48F522C0C0DBDACE21F483224A00FC4C76264A9A61D12F3E2211ECCE665; new untracked SQL test SHA-256 B8DEE5B83629089C0FA72C35FD42FDA17079CF53BC7736AD5A825358EAC42247

## Task goal
Establish verified persistence facts for reporter onboarding, login/refresh/logout, password recovery/change, invitation acceptance, profile/account reads and account administration. Anh proves entity, repository, constraint, transaction, idempotency and concurrency behavior so Huy can implement the API without reaching into EF.

## Business context
Identity is the trust boundary for every later project operation. Reporter OTP verifies email once; it is not an OTP-per-login flow. Sessions, password changes, invitation replay, disabled users and account scope must preserve actor identity and audit evidence.

## Operation trace
V2-P1-001..015: login, refresh, logout, password recovery/change, reporter registration/verify/resend, invitation accept/create, me/account read/update/admin reset. Historical cards remain the individual contract references.

## Sources to read
- `docs/diagram/V2/02_Requirements/01_FRD_SRS.md` FR-01..03; `docs/diagram/V2/02_Requirements/02_Business_Rules.md` BR-02/29/45 and related use-case/story trace.
- `docs/diagram/V2/03_Data/02_ERD_V2.md`, `docs/diagram/V2/03_Data/01_Data_Dictionary.md` section 3.1, `docs/diagram/V2/03_Data/03_Domain_Model_V2.md`.
- `docs/diagram/V2/05_Technical/02_Auth_Permission_Model.md` sections 5.1, 5.4, 5.6; `docs/diagram/V2/05_Technical/05_Sequence_Diagrams.md` SQ-06.
- docs/adr/002-authentication.md and docs/adr/006-v2-endpoint-ownership-and-persistence-coordination.md.
- Current BusinessObjects/Identity, Repositories/Interfaces/Identity, Repositories/Implementations/Identity, DbContext/configuration and Identity SQL tests.

## Source evidence checkpoint

The table below preserves the original dirty-checkout checkpoint. `docs/design/**` was not published in the current checkout; the current source paths and evidence are recorded after the table.

| Source | Heading / ID | Evidence used | Label / checkpoint |
|---|---|---|---|
| `AGENTS.md` and ADR 006 | V2 ownership and shared hotspots | Anh owns entities, repositories, mappings, migrations and SQL tests; Huy owns API layer; handoff is required before Done | `CURRENT_VERIFIED` / HEAD `4586c8c` |
| `docs/adr/002-authentication.md` | Decision; Contract and verification | Server session is authoritative; refresh rotation is single-use and concurrency protected; OTP is hashed, short-lived and consumed atomically; no secret leakage | `CURRENT_VERIFIED` / HEAD `4586c8c` |
| `planning/V2/V2-3_DECISION_REGISTER.md` | D25, 36A, 37 | Reporter OTP is email verification only; web cookie/server session and Android access/refresh are approved design; TTL/rate limits are approved pilot configuration | `TARGET_DOCUMENTED` / HEAD `4586c8c` |
| `docs/design/02_Requirements/01_FRD_SRS.md` | FR-01, FR-02, FR-03 | Authentication/session, staff invitation and Reporter registration requirements traced by this group | `TARGET_DOCUMENTED` / HEAD `4586c8c` |
| `docs/design/02_Requirements/02_Business_Rules.md` | BR-02, BR-29, BR-45 | Server-side scope/ownership, privacy of Reporter data and append-only audit/retention boundaries | `TARGET_DOCUMENTED` / HEAD `4586c8c` |
| `docs/design/03_Data/ERD_Auth_Identity.md` | ApplicationUser, UserSession, RefreshToken, PasswordRecoveryRequest, ReporterRegistrationIntent, StaffInvitation | Entity/FK/rowversion shape and current persistence boundary match the inspected model; full migration proof remains separate | `CURRENT_VERIFIED` / ERD checkpoint `25834bd` |
| `docs/design/03_Data/01_Data_Dictionary.md` | §3.1 Auth & Access | Email/role/status/OTP/session/token invariants, UTC and no-plaintext-secret rules | `TARGET_DOCUMENTED` / HEAD `4586c8c` |
| `docs/design/05_Technical/02_Auth_Permission_Model.md` | §§5.1, 5.4, 5.6 | Active-session/account/scope authority, OTP abuse limits and invitation/account rules | `TARGET_DOCUMENTED` / HEAD `4586c8c` |
| `docs/design/05_Technical/05_Sequence_Diagrams.md` | SQ-06 authentication flow | Login/refresh/logout and security-event durable-effect ordering | `TARGET_DOCUMENTED` / HEAD `4586c8c` |
| `RoadGuardSystem.BusinessObjects/Identity` | ApplicationUser, UserSession, RefreshToken, ReporterRegistrationIntent, StaffInvitation | Current entity fields, derived state and rowversion facts | `CURRENT_VERIFIED` / source at HEAD + dirty diff reviewed |
| `RoadGuardSystem.Repositories/Interfaces/Identity` and `Implementations/Identity` | `IIdentityRepository`, `IIdentityOnboardingRepository`, `IdentityRepository`, `IdentityOnboardingRepository` | Repository facts expose absence/conflict/stale/replay outcomes and use transaction/idempotency/concurrency backstops | `CURRENT_VERIFIED` / source at HEAD + dirty diff reviewed |
| `tests/RoadGuardSystem.IntegrationTests/Identity` | `P110AuthenticationPersistenceTests`, `IdentityPersistencePositiveTests`, `IdentityPersistenceNegativeTests` | SQL Server coverage for rotation, replay, OTP, invitation, recovery, status, uniqueness, safe logs and rollback | `CURRENT_VERIFIED` / 64 focused tests executed |

### Current source evidence - 2026-09-30

| Label | Exact source and heading / ID | Invariant used | Checkpoint |
|---|---|---|---|
| CURRENT_VERIFIED | `AGENTS.md` / Source Evidence And Task Lifecycle, V2 Ownership; `docs/adr/006-v2-endpoint-ownership-and-persistence-coordination.md` / Decision | Anh owns this approved persistence slice; handoff reports facts but does not authorize or block owner work | HEAD `1544497`; owner clarification this session |
| TARGET_DOCUMENTED | `docs/adr/002-authentication.md` / Reporter self-registration amendment, Token Lifecycle; `planning/V2/V2-3_DECISION_REGISTER.md` / D25, 36A, 37 | OTP verifies Reporter email once; refresh is single-use; web cookie and pilot TTL remain rollout/config deltas | HEAD `1544497` |
| TARGET_DOCUMENTED | `docs/diagram/V2/02_Requirements/01_FRD_SRS.md` / FR-01..03; `02_Business_Rules.md` / BR-02, BR-29, BR-45; `04_Use_Cases.md` / CN01, CN02, CN10..12, QT01..02; `05_User_Stories_Acceptance_Criteria.md` / US-01, US-17, US-27, US-28-AC-01..03 | Server-side scope, one-time invitation, no unverified Reporter token, revocation and safe audit | Current `docs/diagram/V2` index |
| TARGET_DOCUMENTED | `docs/diagram/V2/05_Technical/02_Auth_Permission_Model.md` / §§5.1, 5.4, 5.6; `05_Sequence_Diagrams.md` / SQ-06 | Current account/session authority and atomic revocation; missing metadata or provider evidence is not inferred | Current `docs/diagram/V2` index |
| PROPOSED_DELTA | `docs/diagram/V2/05_Technical/openapi.yaml` / `login`, `refreshTokens`, `logout`, `registerReporter`, `verifyReporterOtp`, `resendReporterOtp`, `acceptInvitation`, `createInvitation`, `getMe`, `updateMe`, `adminResetPassword`, `updateAccount`, `getAccount`; `LoginRequest`, `InvitationRequest`, `AcceptInvitation` | Draft wire contract is a trace, not proof of emitted errors, transport or runtime | SHA-256 `ADA7F48F522C0C0DBDACE21F483224A00FC4C76264A9A61D12F3E2211ECCE665` |
| TARGET_DOCUMENTED / CURRENT_VERIFIED | `docs/diagram/V2/03_Data/01_Data_Dictionary.md` / §3.1; `02_ERD_V2.md` / Current verified persistence backbone, Account/session/membership; `03_Domain_Model_V2.md` / Identity/access | User/session/token anchors exist; DD target fields and physical schema remain distinct | HEAD `1544497` |
| CURRENT_VERIFIED | `RoadGuardSystem.Repositories/RoadGuardDbContext.cs` / Identity DbSets; `Configurations/{ApplicationUser,UserSession,RefreshToken,ReporterRegistrationIntent,StaffInvitation}Configuration.cs`; migration chain `20260918152126_AddIdentitySessionSecurityLogs`, `20260927153000_AddPasswordRecoveryRequests`, `20260928094828_AddIdentityOnboardingEmailTemplates` and model snapshot | SQL constraints, rowversions, hashed token/OTP columns, FK and indexes are represented in source and applied to isolated test databases | HEAD `1544497`; SQL Server `16.0.1000.6` |
| CURRENT_VERIFIED | `RoadGuardSystem.Repositories/Implementations/Identity/IdentityRepository.*.cs`, `IdentityOnboardingRepository.cs`; `Interfaces/Identity/IIdentityRepository.cs`, `IIdentityOnboardingRepository.cs`; `tests/RoadGuardSystem.IntegrationTests/Identity/IdentityOnboardingPersistenceTests.cs`; `tests/RoadGuardSystem.ApiTests/Authentication/V2IdentityOnboardingFlowTests.cs` | Atomic session/refresh and account updates, bounded onboarding replay/limits and durable SQL effects | 69/69 Identity SQL; 8/8 focused API; new test SHA-256 above |

## Contract checkpoint

1. Auth persistence is consumed through repository interfaces; actor and authorization decisions remain in Services.
2. Login/verification creates a session and hashed refresh token atomically, with rowversion/concurrency protection and no partial durable effect.
3. Refresh rotates one token; a stale/used/expired token cannot succeed twice; replay revokes the family.
4. Reporter OTP is hashed, expires, counts failed attempts, respects resend cooldown/rate limits, and consumes exactly once.
5. Invitation acceptance and password/account changes are idempotent where the interface declares an idempotency key and revoke/invalidate affected credentials atomically.
6. Repository results return facts (`NotFound`, `Conflict`, `StaleConcurrency`, replay and invalid-input states); they do not choose HTTP status or public error wording.
7. Sensitive values remain absent from logs/entities that are explicitly safe-code constrained.

## Business flow checkpoint

Actor/request -> Service validates actor/scope and supplies normalized input -> Identity repository reads with `AsNoTracking()` or performs one transaction -> entity/session/token/intent/log durable effects are committed or rolled back together -> repository returns fact/result -> Service maps it to DTO/error -> API decides HTTP response. Huy must not access `RoadGuardDbContext` or infer authorization from repository absence alone.

## Current versus target matrix

| Concern | Current verified in this checkout | Target/documented rule | Delivery consequence |
|---|---|---|---|
| User/session authority | `ApplicationUser`, `UserSession`, `RefreshToken`, rowversion and repository result statuses exist; 69 current Identity SQL tests pass | Server-side active account/session/role is authoritative; refresh is single-use and replay revokes family | API consumers map repository facts; no JWT-only authorization claim |
| Reporter onboarding | Registration intent stores normalized email, OTP hash, expiry, failed attempts, resend time and consumed/confirmed timestamps; onboarding repository is transactional | D25/37 require one-time email verification, no OTP-per-login, configured abuse limits and non-enumerating public response | Provider delivery and HTTP response remain Huy/service scope |
| Session transport/TTL | Persistence stores session and token expiry/revocation; transport is not selected by repository | D36A/D37 document web cookie/server session and Android access/refresh plus pilot TTLs | No cookie/JWT/provider implementation in ANH-01 |
| Password/recovery/account state | Repository has atomic password change/reset/recovery, status and credential invalidation facts; safe-code logs are constrained | Suspension, password change/reset and role change revoke affected credentials and preserve audit evidence | Service owns actor policy and stable HTTP error mapping |
| Invitation | Invitation token is stored as hash, linked to creator/projects, has expiry/revocation/accepted state and replay/idempotency outcomes | One-time invitation binds email/role/scope; accept payload cannot change role | No invitation email provider or API contract change |
| Schema/rollout | Existing migration/snapshot and SQL tests are present; no migration was created/applied to a live database by this task | Any schema or live rollout delta needs separate approved scope and migration evidence | Task completion covers current persistence facts; no production rollout claim |

## In scope
- Reconcile current identity entities, repository interfaces and SQL constraints with operation cards and decisions D25, 36A and 37.
- Verify atomic OTP issue/consume/resend, expiry, attempt limit, replay and duplicate email behavior.
- Verify session rotation/revocation, password fingerprint invalidation, invitation token replay and account status transitions.
- Add 1-3 focused SQL tests and minimal persistence fixes only where a failing invariant is inside the approved contract.
- Publish repository fact semantics, version/concurrency tokens, absence/conflict outcomes and fixtures to HUY-01.

## Out of scope
- Service, DTO, controller, HTTP, Postman or API test edits.
- Choosing web-cookie versus token transport, changing TTLs, SMTP/provider delivery or adding SSO.
- New schema/migration or live database application without separately approved scope.
- Rewriting historical auth completion records.

## Exact files and hotspots
Primary: RoadGuardSystem.BusinessObjects/Identity, Repositories/Interfaces/Identity, Repositories/Implementations/Identity, matching configurations and focused tests/RoadGuardSystem.IntegrationTests/Identity. Shared hotspots are RoadGuardDbContext, migrations/snapshot, seed, shared errors and DI; one writer only.

## Stop conditions
Start after Wave 0 and the common base. Stop and record BLOCKED when ERD, migration and current source disagree on a key/constraint; a new policy is needed; SMTP/runtime transport is required; or a schema change is necessary. Do not infer production behavior from a unit or mock test.

## Verification and acceptance
Build Repositories and IntegrationTests from current source. Run focused identity SQL tests with isolated SQL Server and record SQL version, discovered/pass/fail/skip counts, durable rows and replay outcomes. Acceptance requires source evidence and focused SQL evidence for every changed invariant. Handoff reports repository facts to API consumers; receiver acknowledgement is not an approval gate for Anh's work.

## Completion history

### 2026-09-30 03:07 +07:00 - PARTIAL

- Scope/result: Reconciled current identity entities, repository seams, configurations and focused SQL tests against D25, 36A, 37, ADR 002 and the auth ERD. No persistence code change was required; existing repository seams already expose transaction, idempotency, replay and stale-concurrency outcomes for the inspected flow.
- Files: `planning/V2/Execution/ANH-01-auth-identity.md`; handoff appended to `planning/CROSS_OWNER_HANDOFFS.md`. No production or test source was changed by this task.
- Acceptance criteria: Source checkpoint recorded; repository and identity test scope inspected; focused SQL auth invariants passed. HUY-01 receiver confirmation remains open, so task is not DONE.
- Verification: SQL Server `@@VERSION` PASS: Microsoft SQL Server 2022 RTM 16.0.1000.6 Express; `dotnet build RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj -nologo -v q -clp:ErrorsOnly` PASS (0 warning, 0 error); `dotnet build tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj -nologo -v q -clp:ErrorsOnly` PASS (353 warnings, 0 errors); `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~IntegrationTests.Identity" -nologo -v minimal` PASS (64 passed, 0 failed, 0 skipped, isolated SQL Server fixture, 13s).
- Reused/invalidated evidence: Existing identity source/tests were reviewed at HEAD `4586c8c`; no code edit invalidated the build/test evidence. The first repository build command used an obsolete project filename and failed path resolution; the canonical `.cRepositories.csproj` command passed.
- Side effects: No package, migration, schema, live database, data deletion or external provider side effect. Dirty worktree was preserved.
- Unverified/blockers: HUY-01 must consume the repository facts and respond `VERIFIED` or `NO_CHANGE_NEEDED`; API HTTP smoke is outside Anh scope. Proposed D25/36A/37 runtime rollout remains separate from current persistence proof.

### 2026-09-30 03:11 +07:00 - PARTIAL (review)

- Scope/result: Applied `PROMPT-REVIEW.md` diff-first to the ANH-01 task checkpoint and new cross-owner handoff. Found and fixed the missing explicit FR/BR source-evidence rows and added the actual SQL Server version to verification evidence.
- Files: `planning/V2/Execution/ANH-01-auth-identity.md`; `planning/CROSS_OWNER_HANDOFFS.md` reviewed as an untracked task artifact. No production, migration or test source was changed.
- Acceptance criteria: Review scope, source/rule references, current-vs-target matrix, ownership and evidence claims now align with the task and docs. No RED code finding remains.
- Verification: `python docs/design/ci/check_links.py` PASS (`1978` links); `python docs/design/ci/check_alignment.py` PASS (`133` tasks); `git diff --check` PASS. Existing Repositories build and 64-test SQL evidence remains valid because no runtime source changed.
- Handoff: `ANH-01-IDENTITY-01` remains `SENT`; receiver outcome is not fabricated. This blocks DONE and push under the project lifecycle.
- Blocker: Huy must record `NO_CHANGE_NEEDED` or `VERIFIED` with integration evidence before this task can be committed/pushed as complete.

### 2026-09-30 03:34 +07:00 - PARTIAL (handoff publication)

- Scope/result: Owner approved publishing this incomplete checkpoint through `anh` to `develop` for Huy's receiver review; `ANH-01-IDENTITY-01` remains `SENT` and no acceptance gate changed.
- Files: This task and `planning/CROSS_OWNER_HANDOFFS.md`; no identity production or test source changed.
- Verification: Fresh Repositories and IntegrationTests builds PASS; focused identity/survey/notification SQL filter 73 passed, 0 failed, 0 skipped on the dirty local checkout. Earlier 64/64 identity evidence remains narrower identity proof.
- Reused/invalidated evidence: No identity code edit; remote integration must be verified independently because the candidate baseline and `docs/design` restructure are not in this publication.
- Side effects: Git commit/push to `anh` and fast-forward of `develop` are owner-approved; no package, migration application, live data or external-provider effect.
- Unverified/blockers: Receiver outcome, API smoke and clean-branch SQL evidence remain open; deliveryStatus stays `PARTIAL`.

### 2026-09-30 11:37 +07:00 - PARTIAL (owner accepted Huy handoff)

- Scope/result: Owner accepted Huy's handoff. `ANH-01-IDENTITY-01` has receiver outcome `NO_CHANGE_NEEDED` for current repository consumers at clean HEAD `1544497`; this resolves the missing receiver-response gate, but does not turn HUY-01 API delivery or external SMTP into verified behavior.
- Files: this checkpoint and `planning/CROSS_OWNER_HANDOFFS.md`; no production or test source changed. Earlier 64/64 identity SQL evidence remains historical evidence from the dirty candidate-baseline checkout.
- Acceptance criteria: repository fact semantics and receiver mapping were compared with HUY-01's recorded API 4/4, identity SQL 38/38 and full-solution Integration 317/317 evidence on `d2338dc`; `1544497` changes request examples and planning only. The current local SQL gate is not passed, so `DONE` and handoff `VERIFIED` are not claimed.
- Verification: `dotnet build RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj -nologo -v q -clp:ErrorsOnly` PASS (51 warnings, 0 errors); `dotnet build tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj -nologo -v q -clp:ErrorsOnly` PASS (316 warnings, 0 errors). The combined identity/project/survey SQL filter on the freshly built Debug/net8.0 test project executed 122 tests: 1 passed, 121 failed, 0 skipped; failures report that configured `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` cannot connect. `MSSQL$HANHNAV` and Docker are stopped; starting the SQL service was denied by the current process. `python docs/diagram/V2/ci/check_alignment.py` PASS (133 tasks); `git diff --check` PASS; `python docs/diagram/V2/09_Frontend/contracts/check_contracts.py` fails `CONTRACT_LOCK_MISMATCH` on unchanged contract files.
- Reused/invalidated evidence: Huy's recorded clean-checkout results remain a separate receiver checkpoint; the changed SQL environment prevents reusing them as this local run's proof. No runtime source changed after the build.
- Side effects: no package, migration, schema, live data, external provider, commit or push; SQL service start was attempted but denied and the service remains stopped.
- Unverified/blockers: rerun focused identity SQL on a reachable SQL Server and inspect durable effects before setting this task `DONE`; hosted HTTP/SMTP and candidate-baseline rollout remain separate HUY-01/governance gates. `docs/design/**` and the candidate baseline are absent from this checkout.

### 2026-09-30 11:52 +07:00 - DONE (current persistence slice)

- Scope/result: Owner clarified that cross-owner handoff is a report, not a receiver approval gate. Reconciled current auth entities, repository interfaces, SQL mappings/migrations and consumer behavior against the indexed V2 sources. Existing production code already satisfies the approved current persistence contract; added three focused SQL tests for duplicate Reporter email, OTP resend/attempt/expiry limits and invitation expiry/replay. No Service, DTO, Controller, public contract or production persistence behavior changed.
- Files: `tests/RoadGuardSystem.IntegrationTests/Identity/IdentityOnboardingPersistenceTests.cs` (new, SHA-256 `B8DEE5B83629089C0FA72C35FD42FDA17079CF53BC7736AD5A825358EAC42247`), this task, `planning/CROSS_OWNER_HANDOFFS.md`, `planning/V2/Execution/README.md`, and current handoff-rule wording in ANH-02..04. Pre-existing uncommitted checkpoint edits were preserved.
- Acceptance criteria: SQL tests inspect durable `Users`, `ReporterRegistrationIntents`, `Sessions`, `RefreshTokens` and `StaffInvitations` rows. Same-key replay does not duplicate user/session; changed fingerprint conflicts; expired/locked OTP and expired invitation create no account/session; wrong OTP counts persist; the current consumer mapping remains `NO_CHANGE_NEEDED`. Current auth persistence facts are complete for this grouped task. HUY-01 API rollout, external SMTP, web-cookie transport and baseline governance are separate scopes.
- Verification: Debug/net8.0; SQL Server 2022 Express `16.0.1000.6`, isolated fixture databases. `dotnet build RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj -nologo -v q -clp:ErrorsOnly` PASS (51 warnings, 0 errors); `dotnet build tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj -nologo -v q -clp:ErrorsOnly` PASS after test edit (319 warnings, 0 errors); `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~IdentityOnboardingPersistenceTests"` PASS 3/3; `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v q --filter "FullyQualifiedName~IntegrationTests.Identity"` PASS 69/69, 0 failed/skipped. `dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj -nologo -v q -clp:ErrorsOnly` PASS (89 warnings, 0 errors); `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --nologo -v q --filter "FullyQualifiedName~V2IdentityOnboardingFlowTests|FullyQualifiedName~V2AuthenticationFlowTests"` PASS 8/8, 0 failed/skipped.
- Documentation checks: `python docs/diagram/V2/ci/check_alignment.py` PASS (133 tasks, Person 1 = 71, Person 2 = 62); `git diff --check` PASS. No canonical contract files changed.
- Reused/invalidated evidence: the earlier local 121 SQL failures were caused by stopped `MSSQL$HANHNAV`, not implementation failure. After the owner restarted it, the full selected Identity SQL filter was rerun. The new test edit invalidated the first fresh IntegrationTests binary, so it was rebuilt and the filter rerun. API binaries remained valid because no production/API source changed.
- Side effects: test fixture created and dropped isolated SQL databases; no package, migration creation/application to a live database, schema rollout, live data deletion, SMTP/provider call, commit or push. `UNCOMMITTED`.
- Unverified/risks: no external SMTP or hosted production smoke, web cookie/CSRF rollout, candidate baseline migration or live-schema proof. The pre-existing `CONTRACT_LOCK_MISMATCH` in unchanged OpenAPI/lock files is a separate documentation gate and does not validate or invalidate this persistence result.
