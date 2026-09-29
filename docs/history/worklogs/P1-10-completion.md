# RoadGuard task completion log - P1-10

This log was created for assignment, then continued by Codex Implementer round 1 on 2026-09-19. The assignment history below is preserved; the current implementation evidence and blocker disposition are appended near the end. No Git integration or publication was performed.

## Identity and scope

- Task ID/title: `P1-10` / Login, refresh, logout, authoritative session validation, and forced password-change continuation.
- Owner / self-reviewer: Person 1 (`anh`); Codex Implementer for Person 1 performs the task-owner self-review.
- Implementer: Codex Implementer for Person 1.
- Mandatory acceptance reviewer / Done authority: Codex Reviewer in a separate task/session that did not author the submitted artifacts.
- Date / branch or commit: assigned 2026-09-19 on branch `anh`; baseline HEAD `7513d576dac2b0d470059973aff70b1043828c9e`. Final Codex Implementer Round 4 submission completed after pre-handoff fixes; independent acceptance Round 3 returned `Changes requested`.
- Reviewed baseline and exact change scope (commit or working-tree diff): preparation began from a clean tracked/untracked working tree (`git status --short --branch` returned only `## anh...origin/anh`). This assignment file is the only change made by the preparation session.
- Trace (`US-*`, use case, acceptance criteria): `US-01` AC 1, AC 2, the P1-10 portion of AC 6, and its authentication exceptions; `CN01`; identity/session precondition of `CN03`; forced-change continuation after `CN10`; ADR 002 sections 1-3, 5-7; P1-10 plan row. `CN02` profile behavior and project-membership authorization under the remainder of `CN03` are explicitly excluded below.
- In-scope behavior:
  - Internally provisioned username/password login for Supervisor, PM, Drone Operator, and Repair Crew, using ASP.NET Core Identity password hashing/verification.
  - Opaque high-entropy refresh-token generation, hash-only persistence, short-lived JWT access tokens, Session creation, refresh rotation, replay response, current-session logout, and stable ProblemDetails codes.
  - Authoritative per-request checks of `sub`, `sid`, current Session state, current User status, and JWT role snapshot against `User.role_code`; no positive session cache.
  - Forced password-change continuation for accounts with `must_change_password = true`: valid credentials must not grant business access; the user changes the password through the bounded auth flow, the flag is cleared, active credentials are revoked, and a fresh login is required.
  - HMAC-SHA256 JWT signing with a configuration-only key ring and active `kid`; issuer, audience, access/session/refresh lifetimes, active key ID, and key material are required options with no source-controlled secret/default. Old validation keys may remain only through the maximum access-token lifetime, then be retired operationally.
  - Remove the two explicitly deferred Google-authentication package references; do not activate Google/OAuth/SSO.
- Explicitly out of scope:
  - Profile update and Supervisor/Admin password reset (`P1-11`). P1-10 only consumes the resulting `must_change_password` state and implements the user's forced-change continuation.
  - `ProjectMember` lookup, active/effective membership, project/resource scope, Supervisor project bypass, and project HTTP 403 policy (`P2-11`/`P1-12`). A successful P1-10 identity check is necessary but not sufficient project authorization.
  - Account suspend/reactivate, global-role mutation, open-work handover, and `UserRoleChanged` production command (`P1-64`). P1-10 must reject credentials already revoked by those flows and detect a stale role snapshot.
  - User/Role/Session/RefreshToken property or enum shape, EF mapping, migrations, seed data, SQL constraints, or edits to accepted migration history (`P2-10`).
  - External SSO, MFA, distributed session cache, FE implementation, deployment, and Git integration/publication.
- Intended files / exclusive ownership check:
  - P1-10 exclusive new paths: `RoadGuardSystem.DTOs/Authentication/**`, `RoadGuardSystem.Services/Authentication/**`, `RoadGuardSystem.API/Authentication/**`, `RoadGuardSystem.API/Controllers/AuthController.cs`, `tests/RoadGuardSystem.UnitTests/Authentication/**`, `tests/RoadGuardSystem.ApiTests/Authentication/**`, and `tests/RoadGuardSystem.ApiTests/Infrastructure/AuthenticationSqlServerFixture.cs`.
  - P1-10 task-scoped existing files: `RoadGuardSystem.BusinessObjects/Identity/ApplicationUser.cs` (domain methods only; no property/schema shape), `RoadGuardSystem.API/Constants/ApiErrorCodes.cs`, `RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj`, `RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj`, `tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj`, `tests/RoadGuardSystem.ApiTests/Infrastructure/CustomWebApplicationFactory.cs`, `RoadGuardSystem.API/RoadGuardSystem.API.http`, `docs/api-errors.md`, and this worklog.
  - Shared hotspots reserved for P1-10 only when implementation starts: `RoadGuardSystem.API/Program.cs`, `RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs`, API/Services/API-test project files, and non-secret authentication option sections in `RoadGuardSystem.API/appsettings.json`. Do not place signing keys or credentials in tracked settings.
  - Blocked ownership paths, not assigned to P1-10 yet: `RoadGuardSystem.Repositories/Identity/IIdentityRepository.cs`, `RoadGuardSystem.Repositories/Identity/IdentityRepository.cs`, `RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs`, and SQL integration tests under `tests/RoadGuardSystem.IntegrationTests/**`.
- Conflict warning: `P2-01` is currently `In Progress` but owns only CI workflow/verifier/evidence paths, so no current overlap was found. A real P1/P2 ownership gap remains: the accepted P2-10 repository contract can read User/Session/token state, rotate a token, and revoke a family, but it does not expose an atomic initial Session+RefreshToken issuance operation or an atomic replay-revocation+`auth_token_replay_detected` audit operation. P1-10 must not reach through to `RoadGuardDbContext` or edit Person 2 repository/SQL-test paths without an owner decision. Required sequencing: the repository owner must either (a) authorize a narrow P1-10 exception for the listed repository contract/implementation and matching SQL tests without schema changes, or (b) assign Person 2 a bounded persistence follow-up and integrate/accept it before the affected P1-10 slices. Until then, AC-02, AC-06, and AC-07 are blocked; independent DTO/token/options/unit-test preparation may proceed only in a later explicitly requested implementation turn.

## Assignment and acceptance contract

- Assignment author/date and baseline revision: Codex, 2026-09-19, baseline `7513d576dac2b0d470059973aff70b1043828c9e` on `anh`.
- Dependencies and current-checkout evidence:
  - `P1-01` is `Done`; accepted API versioning, ProblemDetails, correlation, startup, and API-test foundation are present. Historical acceptance names commit `3072852`; the latest combined P1-01/P1-02 worklog revision is `3e13ca6`.
  - `P1-02` is `Done`; accepted ADR 002 and error taxonomy are present at current HEAD, with final worklog revision `3e13ca6cbff3dc17fcd8354b1994a6b884029bd0`.
  - `P2-10` is `Done`; implementation commit `d0229869bc2dd5cb153d6ed8dfc81b62e4c0ead1`, its accepted entities/repository/migrations/tests, and its Round-4 `Done` evidence are integrated into current `anh` HEAD through merge `7513d57`.
  - Actual handoff types found in the checkout include `ApplicationUser`, `ApplicationRole`, `UserSession`, `RefreshToken`, `UserRoleCode`, `UserStatus`, `RoadGuardUserStore`, `RoadGuardRoleStore`, and `IIdentityRepository` security-state/rotate/revoke operations. The missing write capabilities are recorded in the conflict warning rather than assumed.
- Required checks and justified N/A cases:
  - Lean TDD per auth behavior slice: negative/edge behavioral RED, smallest positive contract, implementation to GREEN, then affected project tests.
  - Unit tests for credential/status decisions, option validation, JWT claims/key selection, forced-change gating, hash-only refresh handling, refresh/replay/logout mappings, and authoritative request validation.
  - API tests through `WebApplicationFactory` for validation and stable ProblemDetails, login/refresh/logout/forced-change paths, no business access with a forced-change account, revoked/expired/stale-role access, correlation IDs, and sanitized responses/logs.
  - Real SQL Server API/integration proof for atomic session issuance, exactly-one refresh winner, replay family revocation plus one append-only audit effect, logout, and forced-change credential revocation. EF InMemory is not accepted for these claims.
  - Submission gate because security, DI, packages, API contracts, and cross-project persistence are affected: restore, non-incremental full build, format verification, affected tests with non-zero discovery/no unexplained skips, full solution tests, dependency security for changed projects, documentation verifier, EF pending-model check, and secret/plaintext scan.
  - Migration apply/downgrade is N/A only if the accepted P2-10 schema and migrations remain unchanged. A discovered schema need stops the slice for Person 2/owner direction.
  - Project-membership/wrong-project tests are N/A for P1-10 because P1-12 owns `ProjectMember` authorization; P1-10 still tests all four global roles, stale role claims, and server-side User/Session authority.
- Ready for review gate: all in-scope AC are implemented; behavioral RED/GREEN chronology and applicable command evidence are recorded; required real-SQL/API/security gates pass; no zero-discovery or unexplained skipped required tests remain; Codex Implementer completes self-review; exact changed/untracked artifact identity and reviewer prompt are recorded; submitted artifacts are frozen.
- Done gate: a separate Codex Reviewer verifies every AC, actual dependency integration, required SQL/API/security evidence, self-review, no open mandatory finding, and resolution of the repository ownership conflict for the exact submitted artifacts. Only that reviewer may update P1-10 to `Done`. Done does not authorize merge, push, deployment, or publication.

| AC ID | Trace / observable acceptance criterion | In-scope behavior | Required test/evidence |
|---|---|---|---|
| AC-01 | `US-01` AC 1; `CN01`; ADR 002 sections 1 and 5 | Valid internally provisioned credentials are verified with ASP.NET Core Identity; null/blank/malformed credentials, unknown user, wrong password, and `PENDING`/`SUSPENDED` accounts fail without account enumeration or secret leakage. | Unit decision matrix plus API 400/401 ProblemDetails tests; allowed-path tests for all four stable roles; no password/token in response or logs. |
| AC-02 | `US-01` AC 1; `CN01`; Data Dictionary 3.1; ADR 002 section 2 | Successful login atomically creates one active Session and initial refresh-token record, stores only the refresh hash, updates successful-login state, and returns access/refresh credentials once. Session/device metadata is schema-validated and never returned or logged. | Real SQL API proof of state and hash-only persistence; rollback test for partial issuance; duplicate hash/DB failure fails closed; configuration-bound expiry assertions. Blocked pending repository ownership resolution. |
| AC-03 | P1-10 plan row; `US-01` AC 6 continuation; `CN10` | Correct credentials with `must_change_password = true` return `auth_password_change_required` and no business-capable credentials. The bounded change request verifies the current credential, applies Identity password policy, clears the flag, revokes active credentials, records a sanitized audit effect, and requires a fresh login. | Negative weak/mismatched/reused/wrong-current password tests; API proof that business access is denied before change; real SQL proof of password-hash change, flag transition, credential revocation, sanitized audit, and successful fresh login. Admin reset itself remains P1-11. |
| AC-04 | ADR 002 sections 2 and 6 | JWTs are short-lived and contain `sub`, unique `jti`, `sid`, `role`, `iat`, `nbf`, and `exp`; no project membership or secret claim is included. Issuer/audience/lifetimes/HMAC key ring/active `kid` are fail-fast configuration with no tracked key. | Unit token/claim/clock-boundary tests; startup/options negative tests for missing/weak/unknown active key, issuer, audience, or invalid lifetimes; source/config secret scan. |
| AC-05 | `CN01`; identity part of `CN03`; ADR 002 section 2.3 and 2.5 | Every authenticated request loads authoritative Session and User state. Missing/invalid claims, inactive/expired/revoked session, non-active account, store timeout/ambiguity, or mismatched role claim fails closed with 401 and `auth_unauthorized` or `auth_session_revoked`; stale role mismatch revokes the token family. No positive cache is used. | Unit validator matrix and API request-after-state-change tests against real SQL; assert protected action is not invoked; current matching role/session succeeds. |
| AC-06 | ADR 002 section 3.1-3.2; P1-10 plan row | Refresh input is hashed before lookup; expired, unknown, revoked, wrong-session, or malformed tokens fail. A valid token rotates atomically to a new access/refresh pair, revokes the old record, and never persists/logs plaintext. | Negative service/API tests plus controlled real-SQL concurrent rotation: exactly one winner; old token and loser cannot obtain credentials; hash/new-token state asserted. Blocked pending repository ownership resolution. |
| AC-07 | ADR 002 section 3.3-3.4 | Reuse of a consumed/revoked refresh token is classified as replay, atomically revokes the entire Session/token family, appends exactly one `auth_token_replay_detected` security audit effect with correlation ID, and returns no credential/token detail. | Real SQL replay, duplicate retry, audit append-only/sanitization, rollback, and concurrency tests; all family credentials become unusable. Blocked pending repository ownership resolution. |
| AC-08 | `US-01` AC 2; `CN01` | Authenticated logout revokes the current Session/token family; duplicate logout is idempotent and does not create duplicate side effects. Old access and refresh credentials are unusable immediately. | API + real SQL happy path, repeated logout, stale/missing `sid`, and request-after-logout tests. |
| AC-09 | `US-01` exceptions; ADR 002 section 6; `docs/api-errors.md` | Auth endpoints are versioned/thin and publish DTOs only. Validation/auth/conflict outcomes use stable lowercase `snake_case` ProblemDetails codes with correlation IDs; responses/logs exclude password hashes, plaintext credentials, token hashes, signing keys, device metadata, stack traces, and persistence entities. | OpenAPI/DTO boundary tests; 400/401/403/409 contract matrix; reflection/serialization/log capture and tracked-secret scans. |
| AC-10 | ADR 002 section 7 and follow-up allocation; P1-10 plan output | Remove inactive `Google.Apis.Auth` and `Microsoft.AspNetCore.Authentication.Google` references without adding SSO or upgrading unrelated packages; DI/authentication/Swagger configuration boots in production-like settings. | Package diff and dependency-security checks; API factory/startup/OpenAPI tests; full submission gate. |

Keep this contract stable during implementation. Any product/schema/ownership change must be recorded with decision, impact, and affected AC before code proceeds.

## Preconditions and decisions

- Actor and project-scope rule: all four internal roles may authenticate. P1-10 proves current global role and Session/User authority only. It grants no project access; non-Supervisor project checks and verified Supervisor bypass remain P1-12 after P2-11.
- State before / allowed state after:
  - Login: active User + correct password + no forced-change flag -> new active Session and refresh family.
  - Forced change: active User + correct current credential + `must_change_password = true` -> new password hash + flag false + active credentials revoked -> fresh login required.
  - Refresh: active Session + one active unconsumed refresh token -> old token revoked + one new token; a consumed-token replay -> entire family revoked.
  - Logout: current active Session -> revoked; retry remains revoked/idempotent.
- Data/version/immutability rules: UTC `DateTimeOffset`; Session device metadata is write-once; token plaintext is response-only at issuance and never persisted/logged; audit is append-only; existing P2-10 rowversion/concurrency and schema are preserved; no submitted/accepted persistence history is edited in place.
- Audit event and stable error codes: mandatory internal event `auth_token_replay_detected`; forced password change uses a sanitized append-only security event. Public codes include `validation_error`, `auth_invalid_credentials`, `auth_password_change_required`, `auth_unauthorized`, `auth_session_revoked`, and `auth_concurrency_conflict`; replay details stay in the internal audit while the public response fails closed as `auth_session_revoked`.
- Idempotency/concurrency behavior: logout is idempotent; refresh has one winner; replay/duplicate delivery cannot create additional credentials or audit effects; stale Session/token/User versions fail closed. Login retries are separate authentication attempts and must not share plaintext/idempotency payloads.
- Assumptions, ADRs, or specification conflicts:
  - Technical choice resolved for assignment: HMAC-SHA256 with a configured key ring/active `kid`; no source-controlled key and no package/framework upgrade. This implements ADR 002's deferred signing choice without changing schema/product behavior.
  - Technical choice resolved for assignment: a forced-change account receives no business-capable token until password change completes, and successful change revokes credentials and requires fresh login. This is the fail-closed interpretation of "must change at next login."
  - No Data Dictionary/ERD/Domain Model conflict was found for P1-10.
  - Ownership decision still required for missing persistence write/audit operations, as recorded in the Conflict warning. No implementation may silently bypass repository ownership or inject `RoadGuardDbContext` into Services/API.

## Files changed

| Change | File | Purpose |
|---|---|---|
| Added | `docs/worklogs/P1-10-completion.md` | Record P1-10 assignment, acceptance contract, dependencies, ownership, checks, blocker, and gates before implementation. |

No production code, test, plan, specification, package, configuration, or Git history was changed by this preparation session.

## Database, API, config, and operations impact

- Migration added and recovery/downgrade note: none for assignment; P1-10 is constrained to the accepted P2-10 schema. Any schema need is a blocker and requires Person 2/owner direction.
- API/OpenAPI compatibility impact: future additive `/api/v1/auth` login, refresh, logout, and forced-change contracts plus Bearer security/OpenAPI description; exact implementation is not started.
- Configuration/secret/environment impact: future required JWT issuer/audience/lifetime/key-ring options and SQL connection through existing persistence options; signing keys remain external secrets. No secret value is added by this assignment.
- Seed/data migration impact: none; P2-10 role seed is reused.
- Worker/storage/queue impact: none.

## Negative-first evidence

No behavior tests were written or run because the user requested assignment preparation only. The following matrix is the required future RED order, not pass evidence.

| Test | Layer | Expected failure/code | Result |
|---|---|---|---|
| Null/empty/malformed credentials, refresh token, claims, or forced-change body | Unit/API | `validation_error` or generic `auth_invalid_credentials`; no enumeration | Not run - implementation not started |
| Boundary/oversize credentials, metadata, token/key/lifetime options | Unit/API/component | Reject before persistence/startup; no secret echo | Not run - implementation not started |
| Unauthorized/stale role/inactive account | Service/API | 401 `auth_unauthorized` or `auth_session_revoked`; protected action not invoked | Not run - implementation not started |
| Invalid transition/prerequisite | Service/API | Pending/suspended login denied; forced-change account has no business access | Not run - implementation not started |
| Duplicate retry/idempotency | API/SQL | Logout stable; replay has no new credential or duplicate audit effect | Not run - implementation not started |
| Stale concurrency | SQL/API | One refresh winner; loser fails closed/409 mapping as contracted | Not run - implementation not started |
| DB timeout/disconnect | Component/API | Fail closed; no credential issued and no partial state | Not run - implementation not started |
| Integrity/secret/immutable history | API/SQL/security | Hash-only refresh storage; append-only sanitized audit; no tracked/logged plaintext | Not run - implementation not started |

## Positive evidence

| Test | Layer | Expected state/output | Result |
|---|---|---|---|
| Smallest valid path | Unit/API/SQL | Active user login creates one Session/family and returns a valid current-role token pair | Not run - implementation not started |
| Representative full path | API/SQL | login -> refresh -> protected request -> logout; old credentials unusable | Not run - implementation not started |

## Review packet and evidence reuse

- Implementer task/session and independent reviewer task/session identity: not assigned; this session prepared assignment only and cannot later self-accept implementation artifacts.
- Ready-to-run reviewer prompt: to be filled from prompt C in `docs/prompts/RoadGuard_Task_Workflow.md` only after exact submitted content exists.
- Changed files including untracked, AC -> evidence, addressed finding IDs, gaps/risks: assignment file only. AC evidence does not yet exist. Main blocker is repository write/audit ownership for AC-02/06/07.
- Gate selection and full-suite trigger or N/A reason: full solution gate is required because P1-10 changes security, authentication middleware/DI, packages, API contracts, and cross-project persistence behavior.
- Reused evidence: dependency acceptance is inspected prerequisite evidence only, not P1-10 behavior proof. P2-10 Round-4 SQL/full-gate results prove its submitted persistence artifact, not unimplemented P1-10 orchestration.

## Commands run

These are assignment/discovery checks only. No runtime suite was run because no production behavior or test was changed.

| Command | Exit code | Result/coverage | Timestamp |
|---|---:|---|---|
| `git status --short --branch` | 0 | `anh` tracking `origin/anh`; clean before assignment | 2026-09-19T09:14+07:00 |
| `git rev-parse HEAD; git branch --show-current` | 0 | HEAD `7513d576dac2b0d470059973aff70b1043828c9e`; branch `anh` | 2026-09-19T09:14+07:00 |
| Task/dependency/spec/code discovery with `rg`, `git log`, `git show`, and read-only file inspection | 0 | Located P1-10 row, current-checkout dependency artifacts, accepted ADR/spec contracts, existing test/DI layout, and missing persistence operations | 2026-09-19T09:14-09:27+07:00 |

## Self-review and conflict report

- Observable demo/output: assignment contract only; no runtime demo claimed.
- Known gaps, skipped tests, and reason: all implementation checks are intentionally unexecuted because this task was prepared but not started.
- Unexecuted environments / external acceptance dependencies (FE, real AI, field data): FE auth integration, deployment/key provisioning, and live operations remain external. Real AI/field data are not applicable.
- Residual risks: repository ownership is unresolved; signing-key provisioning/rotation must be supplied by deployment secrets; forced-change and replay paths are high-risk and require real SQL/API evidence.
- Self-review findings and resolution: scope was checked against P1-10/P1-11/P1-12/P1-64 boundaries; project membership, Admin reset, account/role administration, schema, SSO, cache, and integration/publication were excluded. P2-10 artifacts were verified in the current checkout rather than inferred from another branch.
- Conflict warning final state: open. AC-02/06/07 cannot start until the repository owner selects and records one of the two ownership/sequencing options above.
- Codex Implementer submission revision/diff identity, including relevant untracked files: none; assignment preparation is not an implementation submission.
- Handoff: not applicable; no submitted artifacts exist to freeze.
- Exact next task/action: repository owner decides the P1/P2 ownership of the missing atomic persistence operations. After that decision is recorded, a later P1-10 implementation turn may set `In Progress` and begin only P1-10 with negative-first tests.
- Latest status assessment date and evidence; supersedes earlier handoff where applicable: 2026-09-19; initial assignment, no earlier P1-10 handoff exists.
- Implementation status: `Blocked` after completing the independent options/token/session-validation primitive slice; AC-02/03/06/07/08 and endpoint wiring remain blocked by the unresolved repository ownership decision. Resume as `In Progress` only after that decision is recorded and the required persistence contract is integrated.

## Codex acceptance review - append one section per round

### Round 1 - independent blocker and partial-artifact review

- Reviewer / round / date: independent Codex Reviewer, current P1-10 acceptance task/session, Round 1, `2026-09-19T10:05:57+07:00`. This reviewer did not author the submitted implementation/test artifacts.
- Reviewed artifact: branch `anh`, HEAD/baseline `7513d576dac2b0d470059973aff70b1043828c9e`, no staged files, 6 tracked modified implementation/metadata files, 8 untracked implementation/test files, and this mutable untracked worklog. The reviewer recomputed all 14 listed file hashes and the LF-joined manifest SHA-256 `3d17215c73b8ea7aa0233d31c02b2f6b0e0541949dea88511d30dbfd3bd42cf7`; every entry matched the handoff. Reviewer-only worklog/plan bookkeeping after this identity check does not alter the reviewed production/test content.
- Scope and dependency inspection: the P1-10 plan row, AC-01 through AC-10, self-review, RED/GREEN chronology, changed and untracked files, ADR 002, P2-10 `Done` status, and the integrated `IIdentityRepository`/`IdentityRepository` contract were inspected. P2-10 supplies authoritative reads, refresh rotation, and family revocation, but does not supply atomic initial Session+RefreshToken issuance or atomic replay-family revocation plus exactly-once `auth_token_replay_detected` audit. The recorded P1/P2 ownership blocker is therefore confirmed in this checkout. P2-01 remains `In Progress` only on hosted-CI workflow/verifier/evidence paths and does not overlap the P1-10 metadata write.
- AC disposition: AC-01 is open except for the error-code registry; AC-02, AC-03, AC-06, AC-07, and AC-08 remain blocked on the persistence ownership/dependency decision and composed endpoint flow; AC-09 is open except for stable codes/documentation; AC-04 and AC-05 have useful application primitives but remain partial; AC-10 package removal is present while production-like DI/authentication/Swagger boot remains open. No AC is accepted as complete for the task-level Done gate in this round.
- Findings: no concrete production-code defect was found in the submitted partial primitive slice. No `F-*` finding is opened in this round.
- Verification gap `VG-01` (open, Person 1 / P1-10): AC-04 and the Round-1 GREEN statement claim time-bound and uniqueness coverage, but `AccessTokenFactoryTests.Create_ValidIdentity_EmitsRequiredClaims` only asserts that `iat`/`nbf`/`exp` and one nonblank `jti` exist. Trigger: a regression can emit the wrong configured expiry or a constant nonblank `jti` and this test still passes. Impact: the required short-lived/unique JWT contract is not regression-proven. Closure: add boundary assertions for `iat`, `nbf`, and configured `exp`, plus generation of two tokens with distinct parseable `jti` values, then rerun the authentication filter.
- Verification gap `VG-02` (open, Person 1 / P1-10): AC-05 requires a validator decision matrix, but the current unit tests do not exercise empty claims, missing session/user, wrong session owner, revoked session, or expiry boundary; API and real-SQL request-after-state-change proof is already explicitly absent. Trigger: those branches can regress while the 15-test filter remains green. Impact: authoritative fail-closed behavior is only partially evidenced. Closure: complete the unit matrix and later add the required bearer/API/real-SQL checks once the composed flow and dependency are available.
- Reviewer reruns: `dotnet test tests/RoadGuardSystem.UnitTests --no-restore --filter "FullyQualifiedName~RoadGuardSystem.UnitTests.Authentication"` exited 0 with 15 passed, 0 failed, 0 skipped; `git diff --check` exited 0; file-hash and aggregate-manifest recomputation matched the handoff exactly. `dotnet list` inspection confirmed `System.IdentityModel.Tokens.Jwt` 8.12.0 is a direct Services dependency and the expected package removals are present.
- Inspected, not rerun: the implementer-recorded restore, non-incremental full build, format verification, full solution test result (265 passed, 0 failed/skipped), dependency vulnerability checks, documentation verifier, and secret scan were tied to the matching manifest, same Windows/.NET 8 environment, and same review date. They remain valid regression evidence for the submitted partial content, but do not prove the absent SQL/API operations. Required future P1-10 SQL/API/security gates are not waived.
- Verdict: `Blocked`. The blocker is a required ownership/dependency decision rather than an observed defect in the partial primitives. P1-10 is not `Ready for review`, `Done`, or integration-ready; ACs and the required SQL/API/security evidence remain incomplete.
- Next bounded action: the repository owner selects either the recorded narrow P1-10 persistence exception or a bounded Person 2 follow-up. After the chosen dependency is integrated and accepted, Codex Implementer resumes P1-10 as `In Progress`, closes `VG-01`/`VG-02`, completes AC-01 through AC-10 negative-first, refreshes invalidated gate evidence, self-reviews, and submits a new frozen identity for independent review.
- Plan status update: `planning/RoadGuard_Plan_Person_1.md`, P1-10 `Blocked -> Blocked` (reviewer-confirmed), actor independent Codex Reviewer, `2026-09-19T10:05:57+07:00`. No production code, test, commit, merge, push, deployment, or publication action was performed by the reviewer.

## Codex Implementer round 1 - independent slice and blocker handoff

### Status and artifact identity

- Implementer / Person / branch: Codex Implementer / Person 1 / `anh`.
- Baseline: HEAD `7513d576dac2b0d470059973aff70b1043828c9e`; no commit, merge, push, or publication was performed.
- Status: `Blocked`. The task entered `In Progress` before edits; the independent AC-04/AC-05/AC-09/AC-10 primitives below were completed, then the task returned to `Blocked` because the previously recorded repository ownership conflict still prevents the transactional auth flows.
- Exact implementation content identity: SHA-256 manifest `3d17215c73b8ea7aa0233d31c02b2f6b0e0541949dea88511d30dbfd3bd42cf7`, calculated from the 14 implementation/test/plan files listed below as `<file-sha256><two spaces><path>`. This intentionally excludes this mutable task worklog; reviewer/status-only bookkeeping does not change the implementation identity.
- Git state at handoff: no staged files; 6 tracked modified files and 8 untracked implementation/test files, plus this untracked worklog. `git diff --check` passed. Full paths are enumerated below rather than relying on collapsed directory entries in `git status`.

### Files changed in round 1

| Change | File(s) | Purpose |
|---|---|---|
| Added | `RoadGuardSystem.Services/Authentication/AccessTokenFactory.cs`; `JwtOptions.cs`; `RefreshTokenGenerator.cs` | HMAC-SHA256 JWT with configured key ring/`kid`, fail-fast options validation, opaque 256-bit refresh credentials and SHA-256 hash-only persistence material. |
| Added | `RoadGuardSystem.Services/Authentication/AuthoritativeSessionValidator.cs` | Load Session/User on every validation, reject inactive/missing state, fail closed on persistence errors, and revoke the family for stale role snapshots. |
| Added | `tests/RoadGuardSystem.UnitTests/Authentication/AccessTokenFactoryTests.cs`; `AuthoritativeSessionValidatorTests.cs`; `JwtOptionsTests.cs`; `RefreshTokenGeneratorTests.cs` | Fifteen P1-10 unit cases for invalid identity/role/options/token input, inactive/store-failure/stale-role decisions, required JWT claims and positive current-state contracts. |
| Modified | `RoadGuardSystem.API/Constants/ApiErrorCodes.cs`; `docs/api-errors.md` | Register the stable auth ProblemDetails codes fixed by the assignment. No endpoint mapping is claimed yet. |
| Modified | `RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj`; `RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj` | Remove the two deferred Google authentication packages without package upgrades or SSO activation. |
| Modified | `tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj` | Reference Services and the existing JWT package version for direct token inspection. |
| Modified | `planning/RoadGuard_Plan_Person_1.md` | Record the start and final `Blocked` disposition/resume point for P1-10. |

Implementation manifest entries:

```text
92e0e51ee73550cca287c844b8ca0d1252aaa00ed93589ac571f3681ec829b52  RoadGuardSystem.API/Constants/ApiErrorCodes.cs
59c29f945dd3e2921f1a8728a2496630d7a6364c58da60fa2531e32807c27cbf  RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj
dc41170d6c092b806ef1f5eb35e252e54a806d39355aa8263676626a261a7cd7  RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj
e2858882114a92469dbc5f7c0d8b0b5e7c41cd3e88c0c74a7f0385dae7a1068d  RoadGuardSystem.Services/Authentication/AccessTokenFactory.cs
b14fa8f74dd8c25706cc01de9135b279ce3fd4fa7a33121c55b1160fd5889949  RoadGuardSystem.Services/Authentication/AuthoritativeSessionValidator.cs
3f35ac5340ae6df73fd704eae380d872d327f0378e3530b5709039e0de729aec  RoadGuardSystem.Services/Authentication/JwtOptions.cs
d870cb2512481e4535ec77608fd0e699b5f7a2c3dea6ef79a98fb8eae2e8e26b  RoadGuardSystem.Services/Authentication/RefreshTokenGenerator.cs
fe21058ad7a559ba4dd005deca530358191038591ea5f9898215b73fd9bdd348  docs/api-errors.md
b0a067a83dd887f04417b42ce37be8b3fd9016552905c30d7103dfa6dbc14328  planning/RoadGuard_Plan_Person_1.md
7d9f320cadd975f2de4189b93a1182ede06aeaecea7350bdff4986e74b9f0e6b  tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj
f0f2323fad086f4729dff7216704f1bb112bb4be72d32707984a373237bbb3b4  tests/RoadGuardSystem.UnitTests/Authentication/AccessTokenFactoryTests.cs
8b09c25df2ebd45c1ecabe5c3d48b1ca46a1e3e76aba0088eecfda45f305a5a1  tests/RoadGuardSystem.UnitTests/Authentication/AuthoritativeSessionValidatorTests.cs
0ad39badf5f80057b19cb6416316c0417401d7cf5c08be28663cd31b05e56803  tests/RoadGuardSystem.UnitTests/Authentication/JwtOptionsTests.cs
4667904f149b7eab55259aa36663cf98530a54e3cdc6ba2141769fdac9d322c0  tests/RoadGuardSystem.UnitTests/Authentication/RefreshTokenGeneratorTests.cs
```

### RED/GREEN chronology and AC coverage

| Slice | RED | GREEN / coverage |
|---|---|---|
| JWT/options/refresh primitives | Initial type-first run exited 1 at compile because the production types did not exist; this is setup evidence, not behavioral RED. A later focused edge test `Create_EmptyIdentity_Throws` built and failed 2/2 because empty `sub`/`sid` values were accepted, establishing the required behavioral RED. | Minimal empty-Guid guard added; complete authentication filter passed 15/15, 0 failed/skipped. Options tests reject missing/unknown/weakly encoded key configuration and invalid lifetimes; token tests verify required claims, canonical role, `kid`, issuer/audience, uniqueness and SHA-256 hash-only material. |
| Authoritative Session/User/role validation | Negative cases were authored before the service type; initial run exited 1 at compile and is not claimed as behavioral RED. | Focused validator filter passed 6/6, 0 failed/skipped: stale role revokes, inactive statuses reject, store failure fails closed, and matching current state succeeds. Runtime bearer middleware/API proof remains incomplete and blocked with the endpoint flow. |

| AC | Round-1 evidence / disposition |
|---|---|
| AC-01 | Stable invalid-credential code registered only. Credential verification/login behavior is not implemented; open. |
| AC-02 | Blocked: missing atomic initial Session + RefreshToken issuance repository operation and matching SQL proof. |
| AC-03 | Blocked with endpoint flow: forced password change requires atomic password/flag/credential-revocation/audit persistence behavior not exposed by the accepted contract. |
| AC-04 | Application primitives implemented and unit-covered for required claims, time bounds, HMAC key ring/active `kid`, role code and malformed identities. Host option binding/startup and bearer-validation API tests remain open. |
| AC-05 | Application validator implemented and unit-covered for Session/User/status/role authority, stale-role revocation and store failure. Per-request middleware/API/real-SQL proof remains open. |
| AC-06 | Blocked: rotate read primitive exists, but the complete service/API transaction and required concurrent SQL proof cannot be completed under current ownership; initial issuance is also absent. |
| AC-07 | Blocked: no atomic replay family-revocation plus exactly-once append-only `auth_token_replay_detected` operation exists. |
| AC-08 | Blocked with endpoint flow: repository revoke exists, but authenticated endpoint, stale/missing `sid`, duplicate-effect proof and old-credential API verification depend on the missing composed auth flow. |
| AC-09 | Stable auth codes and documentation added; DTO/controller/OpenAPI/ProblemDetails/API tests remain open. |
| AC-10 | The two inactive Google packages were removed; restore/build/format/full tests, dependency vulnerability checks and documentation verification passed. DI/authentication/Swagger production-like boot remains open. |

### Commands and results

Environment: Windows/PowerShell, .NET 8 solution, local SQL Server integration fixture used by the existing suite; evidence captured 2026-09-19T09:31-09:53+07:00 against the working-tree content above.

| Command | Exit | Result |
|---|---:|---|
| `dotnet test tests/RoadGuardSystem.UnitTests --filter "FullyQualifiedName~AccessTokenFactoryTests.Create_EmptyIdentity_Throws"` before fix | 1 | Behavioral RED: 0 passed, 2 failed, 0 skipped; token factory accepted empty user/session identifiers. |
| `dotnet test tests/RoadGuardSystem.UnitTests --filter "FullyQualifiedName~RoadGuardSystem.UnitTests.Authentication"` after fix | 0 | 15 passed, 0 failed, 0 skipped. |
| `dotnet restore RoadGuardSystem.slnx` | 0 | All solution dependencies restored; removed Google packages no longer required. |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | 9 projects built; 0 warnings, 0 errors. |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | No formatting changes required. |
| `dotnet test RoadGuardSystem.slnx --no-build --no-restore` | 0 | 265 passed, 0 failed, 0 skipped: Unit 100, API 26, SQL Integration 139. |
| `dotnet list RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj package --vulnerable --include-transitive` | 0 | No vulnerable packages from configured sources. |
| `dotnet list RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj package --vulnerable --include-transitive` | 0 | No vulnerable packages from configured sources. |
| `pwsh -NoProfile -File tests/Documentation/Verify-P102Docs.ps1` | 0 | P1-02 docs/role/dependency contracts passed. |
| `git diff --check`; status, explicit file hash manifest and tracked secret-pattern inspection | 0 | No whitespace errors; no signing key/refresh plaintext introduced. Secret-pattern search matched only historical P2-10 worklog prose, not production/config content. |

### Self-review, blocker and resume point

- Authorization: token role remains a snapshot; the validator reads current Session and User on every call and revokes on role mismatch. No project authorization was introduced; P1-12 remains owner of that policy.
- Transitions/immutability: refresh plaintext exists only in the returned in-memory material, while persistence receives the lowercase SHA-256 hash. No schema/entity/migration or accepted persistence history changed.
- Retry/concurrency/audit: no claim is made for atomic issuance, exactly-one refresh winner, replay audit, or forced-change audit. Those remain mandatory and blocked, not waived by the passing existing SQL suite.
- Secrets: no key/default was added to tracked settings; key ring content is required externally and validated as Base64 with at least 256 bits. Responses/controllers/logging do not yet exist in this round.
- Tests: the completed primitives have 15 targeted tests. Missing API/real-SQL P1-10 flows are explicit gaps; existing 139 SQL tests prove regression safety only, not the absent P1-10 operations.
- Findings addressed: none; no independent review round or finding IDs exist.
- Blocking decision: repository owner must choose either (a) authorize a narrow P1-10 exception over `IIdentityRepository`, `IdentityRepository`, persistence DI and matching SQL tests, or (b) assign/integrate/accept a bounded Person 2 persistence follow-up. Required operations are atomic initial Session+RefreshToken issuance, forced-change persistence/audit as allocated, and atomic replay family revocation with exactly-once sanitized security audit.
- Resume point: after that decision and dependency artifact are present, set P1-10 back to `In Progress`; start with negative real-SQL tests for rollback/concurrent issuance/rotation/replay audit, then implement repository operations, service/API/DTO/bearer wiring, forced change and logout. Rerun the full submission gate on final content. Do not mark `Ready for review` until AC-01 through AC-10 and all required SQL/API/security evidence pass.

### Ready-to-run independent reviewer prompt (Prompt C)

This prompt can verify the round-1 artifact and blocker disposition, but it cannot accept P1-10 as `Done` because the task is intentionally `Blocked` and the listed AC remain open.

```text
Bạn là Codex Reviewer độc lập cho RoadGuard P1-10, Person 1, branch anh.
Baseline: 7513d576dac2b0d470059973aff70b1043828c9e. Submission: working-tree implementation manifest SHA-256 3d17215c73b8ea7aa0233d31c02b2f6b0e0541949dea88511d30dbfd3bd42cf7; no staged files; 6 tracked modified files, 8 untracked implementation/test files, and the mutable untracked worklog enumerated in docs/worklogs/P1-10-completion.md.
Worklog: docs/worklogs/P1-10-completion.md.

Phiên này phải là task/session riêng, không phải phiên đã tạo submitted artifacts. Nếu bạn đã author artifact, dừng acceptance và yêu cầu reviewer khác. Không sửa production code/tests. Bạn được ghi review/status đúng task; nếu yêu cầu chỉ đọc/report-only thì không sửa file.

Đọc AGENTS.md, dùng roadguard-review và roadguard-review-p1. Kiểm tra status/HEAD, assignment/AC, dependency trong checkout, file ownership, manifest, self-review và blocker. Review đúng artifact gồm relevant untracked files. Xác minh AC-04/05 primitives và AC-10 package cleanup; không suy diễn existing SQL suite chứng minh các P1-10 transaction chưa tồn tại. Kiểm tra AC-01/02/03/06/07/08/09 còn mở và conflict thiếu atomic repository operations.

Chạy regression mới và high-risk checks khi cần; xác minh evidence còn lại theo content/environment. Finding phải có ID ổn định, severity, owner, file/dòng, trigger -> impact, AC và closure condition. Không sửa implementation hoặc mở rộng AC.

Ghi review round, reviewer task/session, time, artifact identity, AC coverage, checks/dispositions và verdict vào worklog. Giữ P1-10 Blocked nếu dependency/ownership vẫn thiếu; không đánh Done hoặc Ready for review. Trả findings/gaps, checks thực chạy hoặc evidence đã đối chiếu, verdict/status thực ghi và yêu cầu resume ngắn theo finding IDs nếu có.
```

## Codex Implementer round 2 - VG-01/VG-02 coverage fix

### Start state and bounded scope

- Implementer / Person / branch / baseline: Codex Implementer / Person 1 / `anh` / HEAD `7513d576dac2b0d470059973aff70b1043828c9e`.
- Status transition: reviewer-confirmed `Blocked -> In Progress` on 2026-09-19 for the independent review-gap slice requested by the repository owner.
- Findings addressed: `VG-01` and `VG-02` from independent review Round 1. No `F-*` code finding exists.
- In scope: add observable AC-04 regression coverage for configured JWT temporal claims and per-issuance `jti` uniqueness across different users; add the missing AC-05 authoritative session/user validation matrix.
- Out of scope: production behavior changes unless a new regression exposes a defect; DTO/API/DI/login/refresh/logout/forced-change flows; Repositories, migrations, SQL integration tests, schema, merge, commit, push, deployment, and publication.
- Exclusive paths for this round: `tests/RoadGuardSystem.UnitTests/Authentication/AccessTokenFactoryTests.cs`, `tests/RoadGuardSystem.UnitTests/Authentication/AuthoritativeSessionValidatorTests.cs`, this worklog, and the P1-10 row in `planning/RoadGuard_Plan_Person_1.md`.
- Conflict warning: the open P1/P2 persistence ownership decision is unchanged. This round does not edit `RoadGuardSystem.Repositories/**` or `tests/RoadGuardSystem.IntegrationTests/**`.
- Test chronology rule: these gaps concern missing evidence for existing behavior. New regression tests are run immediately after authoring; if they pass against the unchanged implementation, record that as initial GREEN/characterization evidence rather than inventing a behavioral RED. Any observed behavioral failure must be diagnosed before a production fix.

### Coverage chronology and AC disposition

| Gap / slice | First run against unchanged production | Result and disposition |
|---|---|---|
| `VG-01` / AC-04 temporal and JWT identity contract | `dotnet test tests/RoadGuardSystem.UnitTests --no-restore --filter "FullyQualifiedName~AccessTokenFactoryTests"` | Exit 0, 6 passed, 0 failed/skipped. The two new tests were initial GREEN characterization evidence: exact `iat`/`nbf`/configured `exp` and parseable distinct `jti` values for tokens issued to two deterministic, different user identities. No production fix was required. `VG-01` is addressed, awaiting independent reviewer verification. |
| `VG-02` / AC-05 authoritative validation matrix | `dotnet test tests/RoadGuardSystem.UnitTests --no-restore --filter "FullyQualifiedName~AuthoritativeSessionValidatorTests"` | Exit 0, 14 passed, 0 failed/skipped. New negative cases cover empty user/session claims, `Unknown` role, missing session/user, wrong session owner, revoked session, and the `ExpiresAt == now` boundary. No production fix was required. `VG-02` is addressed, awaiting independent reviewer verification. |
| Combined authentication regression | `dotnet test tests/RoadGuardSystem.UnitTests --no-restore --filter "FullyQualifiedName~RoadGuardSystem.UnitTests.Authentication"` | Exit 0, 25 passed, 0 failed/skipped after deterministic fixture refactor. |

- AC-04/AC-05 disposition: the two review gaps are addressed by regression evidence, but neither whole AC is claimed complete because host bearer wiring, API request-after-state-change, and real-SQL proof remain absent.
- Remaining AC disposition is unchanged from review Round 1: AC-01/02/03/06/07/08/09 and production-like AC-10 wiring remain open; the missing atomic persistence operations continue to block the affected flows.
- Negative-first note: no behavioral RED occurred because Round 1 identified verification gaps rather than faulty behavior. The first executions passed against unchanged production. Recording a fabricated RED or weakening the existing implementation would violate the repository evidence contract.

### Files and exact content identity

- Round-2 code/test change: only `tests/RoadGuardSystem.UnitTests/Authentication/AccessTokenFactoryTests.cs` and `tests/RoadGuardSystem.UnitTests/Authentication/AuthoritativeSessionValidatorTests.cs` changed. Production source, package references, API codes, and documentation from Round 1 remain byte-identical.
- Reviewer/status bookkeeping: this worklog and the P1-10 row in `planning/RoadGuard_Plan_Person_1.md` were updated. No staged files, commit, merge, push, deployment, or publication was performed.
- Final round-2 implementation identity: SHA-256 manifest `d2606b1c107d290b25568ca04cbbe8429731fec79627568cab56edd2abe81bd2`, calculated from the following LF-joined entries as `<file-sha256><two spaces><path>`; this mutable worklog is intentionally excluded.

```text
92e0e51ee73550cca287c844b8ca0d1252aaa00ed93589ac571f3681ec829b52  RoadGuardSystem.API/Constants/ApiErrorCodes.cs
59c29f945dd3e2921f1a8728a2496630d7a6364c58da60fa2531e32807c27cbf  RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj
dc41170d6c092b806ef1f5eb35e252e54a806d39355aa8263676626a261a7cd7  RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj
e2858882114a92469dbc5f7c0d8b0b5e7c41cd3e88c0c74a7f0385dae7a1068d  RoadGuardSystem.Services/Authentication/AccessTokenFactory.cs
b14fa8f74dd8c25706cc01de9135b279ce3fd4fa7a33121c55b1160fd5889949  RoadGuardSystem.Services/Authentication/AuthoritativeSessionValidator.cs
3f35ac5340ae6df73fd704eae380d872d327f0378e3530b5709039e0de729aec  RoadGuardSystem.Services/Authentication/JwtOptions.cs
d870cb2512481e4535ec77608fd0e699b5f7a2c3dea6ef79a98fb8eae2e8e26b  RoadGuardSystem.Services/Authentication/RefreshTokenGenerator.cs
fe21058ad7a559ba4dd005deca530358191038591ea5f9898215b73fd9bdd348  docs/api-errors.md
6ae5d8b98a31c4d200598e4db64f77b5489ca85218b9755679658be7a3c1a29a  planning/RoadGuard_Plan_Person_1.md
7d9f320cadd975f2de4189b93a1182ede06aeaecea7350bdff4986e74b9f0e6b  tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj
a0fbc9c267f5184822068cb37a7e4f03f7348041b4bccedaddbba96ed2f70f0a  tests/RoadGuardSystem.UnitTests/Authentication/AccessTokenFactoryTests.cs
edf9b4fc5358cd9b5bd39d05127a7f895610aef4a1c53fae78ab7804a35e6311  tests/RoadGuardSystem.UnitTests/Authentication/AuthoritativeSessionValidatorTests.cs
0ad39badf5f80057b19cb6416316c0417401d7cf5c08be28663cd31b05e56803  tests/RoadGuardSystem.UnitTests/Authentication/JwtOptionsTests.cs
4667904f149b7eab55259aa36663cf98530a54e3cdc6ba2141769fdac9d322c0  tests/RoadGuardSystem.UnitTests/Authentication/RefreshTokenGeneratorTests.cs
```

### Submission checks and self-review

Environment/time: Windows PowerShell, pinned .NET SDK 10.0.401 targeting net8.0, local SQL Server integration fixture, evidence completed `2026-09-19T10:19:13+07:00`.

| Command | Exit | Result |
|---|---:|---|
| `dotnet restore RoadGuardSystem.slnx` | 0 | All projects up to date. |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | 9 projects built, 0 warnings/errors. |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | No formatting changes required. |
| `dotnet test RoadGuardSystem.slnx --no-build --no-restore` | 0 | 275 passed, 0 failed/skipped: Unit 110, API 26, SQL Integration 139. Existing SQL tests remain regression evidence only and do not prove the absent P1-10 atomic operations. |
| `pwsh -NoProfile -File tests/Documentation/Verify-P102Docs.ps1` | 0 | Documentation contracts, mappings, role codes, and dependency checks passed after the final worklog/plan update. |
| `git diff --check` | 0 | No whitespace errors. |

- Authorization: the expanded matrix proves invalid/missing claims and authoritative user/session state fail closed; stale role-family revocation coverage remains green. Project membership is still P1-12 scope.
- State/immutability: no production state transition, entity, schema, migration, or persistence history changed.
- Retry/concurrency/audit: no new claim is made. Atomic issuance, replay audit, concurrent refresh, forced-change audit, and logout remain blocked/open and require real SQL/API evidence after the ownership decision.
- Secrets: test fixtures contain only deterministic public GUIDs; no credential, token, signing key, connection string, or sensitive log content was added.
- Test quality: temporal expectations are literal and independent of the factory calculation; different-user inputs are deterministic; expiry uses the exact rejection boundary; the repository stub exercises the real validator while replacing only the external persistence boundary.
- Evidence reuse: Round-1 package vulnerability and secret-scan evidence was inspected and remains applicable because package and production source hashes are unchanged. Hosted CI was not run in this round; FE integration, deployment key provisioning, live operations, real AI, and field data remain external/not applicable to this bounded fix.
- Self-review verdict: `VG-01` and `VG-02` are addressed, awaiting independent verification. No implementation finding remains in this bounded slice.
- Final status: `Blocked`, not `Ready for review` or `Done`, because the task-level persistence ownership/dependency conflict and incomplete ACs remain. The plan row was returned `In Progress -> Blocked` after this independent slice.
- Resume point: repository owner chooses the already recorded P1 persistence exception or a bounded P2 follow-up. After the accepted dependency exists, Codex Implementer resumes P1-10, completes all remaining ACs and required SQL/API/security gates, then produces a new full-task review packet.

### Ready-to-run independent reviewer prompt - Round 2

```text
Bạn là Codex Reviewer độc lập cho RoadGuard P1-10, Person 1, branch anh.
Baseline: 7513d576dac2b0d470059973aff70b1043828c9e. Submission: working-tree implementation manifest SHA-256 d2606b1c107d290b25568ca04cbbe8429731fec79627568cab56edd2abe81bd2; no staged files; 6 tracked modified files, 8 untracked implementation/test files, and the mutable untracked worklog enumerated in docs/worklogs/P1-10-completion.md.
Worklog: docs/worklogs/P1-10-completion.md.

Phiên này phải là task/session riêng và không phải phiên đã author round-2 artifacts. Không sửa production code/tests. Đọc AGENTS.md, dùng roadguard-review và roadguard-review-p1. Review đúng manifest gồm relevant untracked files.

Xác minh closure của VG-01: exact iat/nbf/configured exp và jti parseable, khác nhau cho hai user khác nhau. Xác minh VG-02: empty claims, Unknown role, missing session/user, wrong owner, revoked và ExpiresAt == now đều fail closed. Rerun regression mới/high-risk checks khi cần và phân biệt với evidence đã inspect.

Giữ P1-10 Blocked nếu persistence ownership/dependency vẫn chưa được quyết định; không suy diễn 139 existing SQL tests chứng minh các operation chưa tồn tại. Ghi review round, finding/gap dispositions, checks, artifact identity và verdict vào worklog/plan. Không mark Done hoặc Ready for review khi các AC task-level còn mở.
```

## Codex Implementer round 3 - owner-authorized persistence exception

### Decision, ownership, and slice design

- Decision/date: repository owner explicitly selected the narrow P1-10 persistence exception on 2026-09-19. The earlier ownership blocker is resolved for the exact paths below; it is not general permission for P1 to take future P2 work.
- Status: `Blocked -> In Progress`. Implementer / Person / branch / baseline: Codex Implementer / Person 1 / `anh` / HEAD `7513d576dac2b0d470059973aff70b1043828c9e` plus the existing P1-10 working tree.
- Temporary exclusive paths: `RoadGuardSystem.Repositories/Identity/IIdentityRepository.cs`, `RoadGuardSystem.Repositories/Identity/IdentityRepository.cs`, `RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs` only if registration requires a change, new P1-10 SQL tests under `tests/RoadGuardSystem.IntegrationTests/**`, both existing plans, and the P1-10/P2-10 worklog notices.
- In scope: atomic initial Session+RefreshToken issuance with authoritative user/concurrency checks and successful-login update; atomic forced-password-change persistence with credential revocation, sanitized append-only audit and retry semantics; atomic refresh-token replay family revocation with exactly-once sanitized audit; real-SQL rollback/concurrency/idempotency tests; DI resolution verification.
- Out of scope: schema, entity/property/enum shape, mappings, migrations/model snapshot, rewriting accepted P2-10 tests/evidence, P1-11 Admin reset, project membership, commit/merge/push/deploy/publication.
- Architecture: Services will later verify credentials/password policy and generate password/token material; repository methods accept only hashes/identifiers plus expected rowversions, own the SQL transaction, and never return or persist plaintext credentials. Replay audit contains no token/hash. Existing scoped `IIdentityRepository -> IdentityRepository` registration is preserved unless a failing resolver test proves a change is needed.
- Lean TDD order: add isolated P1-10 SQL negative/concurrency tests, record compilation-only setup failures separately, establish behavioral RED against scaffolded contracts, implement the smallest transaction to GREEN, then run P1-10 SQL, all affected SQL/API/unit checks, full submission gate, self-review and independent-review handoff.

### Round-3 persistence files and behavior

| Change | File | Purpose |
|---|---|---|
| Modified | `RoadGuardSystem.Repositories/Identity/IIdentityRepository.cs` | Add result/status contracts for atomic initial issuance, forced password change, and replay-family revocation. |
| Modified | `RoadGuardSystem.Repositories/Identity/IdentityRepository.cs` | Implement SQL transactions, authoritative rowversion checks, rollback classification, active credential revocation, sanitized audit, and idempotency/concurrency resolution. |
| Added | `tests/RoadGuardSystem.IntegrationTests/Identity/P110AuthenticationPersistenceTests.cs` | Fourteen real-SQL P1-10 cases for rollback, eligibility, state, secret-free audit, retries, deterministic races, and DI resolution. |
| Modified | `tests/RoadGuardSystem.UnitTests/Authentication/AuthoritativeSessionValidatorTests.cs` | Keep its repository test double aligned with the extended contract; no validator behavior change. |
| Inspected, unchanged | `RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs` | Existing scoped `IIdentityRepository -> IdentityRepository` registration is correct; DI resolution test passed, so no production DI churn was added. |
| Metadata | both person plans and P1-10/P2-10 worklogs | Record the owner-authorized exception, exclusive paths, sequencing, evidence, and Person 2 visibility without reopening historical P2-10 acceptance. |

No entity/property/enum, EF mapping, migration, model snapshot, package, or accepted P2-10 test was changed.

### Round-3 RED/GREEN chronology

| Slice | RED / diagnostic | GREEN |
|---|---|---|
| AC-02 atomic initial issuance | First run exited 1 at compile because the new interface did not exist; recorded as setup, not behavioral RED. After type/signature scaffolding, 3/3 tests failed with `NotImplementedException`, proving the absent transaction. | Implemented authoritative user/rowversion/eligibility checks and one transaction for `LastLoginAt` + Session + refresh hash. Focused slice passed 3/3; later matrix added pending/suspended/must-change rejections and the full class passed. |
| AC-03 forced-password-change persistence | First run exited 1 at compile; after signature scaffolding, 2/2 tests failed at `NotImplementedException`. | Atomic password hash/security stamp/flag update, active credential revocation, sanitized audit and idempotency record passed. Self-review added changed-payload and controlled concurrent duplicate regressions; one success/one replay and exactly one audit are proven. |
| AC-07 replay response | First run exited 1 at compile; after signature scaffolding, 3/3 failed at `NotImplementedException`. Initial implementation then had a genuine behavioral RED: stale token rowversion returned `Success` because a tracked entity hid the newer SQL rowversion. | Authoritative `AsNoTracking` rowversion read fixed the stale-state bug. Stale input preserves the family, valid replay revokes active family credentials, sequential/concurrent retries append exactly one secret-free `auth_token_replay_detected` audit; focused slice passed 3/3. |
| Persistence DI | Existing registration was inspected before modification. | Resolver test passed without changing `RoadGuardPersistenceExtensions.cs`; no unnecessary abstraction or registration churn was introduced. |

### Round-3 checks

Environment/time: Windows PowerShell, SDK 10.0.401 targeting net8.0, local pinned SQL Server fixture, completed `2026-09-19T10:51:30+07:00`.

| Command/check | Exit | Result |
|---|---:|---|
| `dotnet test tests/RoadGuardSystem.IntegrationTests --no-restore --filter "FullyQualifiedName~P110AuthenticationPersistenceTests"` | 0 | 14 passed, 0 failed/skipped on SQL Server. |
| `dotnet test tests/RoadGuardSystem.IntegrationTests --no-restore --filter "TaskId=P1-10"` before later self-review cases | 0 | 9 passed, 0 failed/skipped; superseded by the final 14-case class run. |
| `dotnet test tests/RoadGuardSystem.IntegrationTests --no-restore --filter "TaskId=P2-10"` | 0 | 39 passed, 0 failed/skipped; accepted P2-10 behavior did not regress. |
| `dotnet test tests/RoadGuardSystem.UnitTests --no-restore --filter "FullyQualifiedName~RoadGuardSystem.UnitTests.Authentication"` | 0 | 25 passed, 0 failed/skipped. |
| `dotnet restore RoadGuardSystem.slnx` | 0 | All projects up to date. |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | 9 projects, 0 warnings/errors. |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | Initial run identified only new-test indentation; scoped formatter corrected it and final verification passed. |
| `dotnet test RoadGuardSystem.slnx --no-build --no-restore` | 0 | 289 passed, 0 failed/skipped: Unit 110, API 26, Integration 153. |
| EF `has-pending-model-changes` with a non-secret syntactically valid design-time connection setting | 0 | No changes have been made to the model since the last migration. The first attempt without the required environment setting exited 1 before model inspection and is not claimed as evidence. |
| Repository dependency-security verifier | 0 | No High/Critical vulnerable dependency. |
| P1 documentation and P2 planning verifiers | 0 | Documentation contracts passed; planning scenarios 9/9 passed. |
| `git diff --check` | 0 | No whitespace errors. |

### Round-3 self-review and disposition

- Authorization/current state: initial issuance rechecks current persisted status, forced-change flag and user rowversion inside the transaction; it never trusts JWT/project claims. Project membership remains P1-12.
- State/immutability: only `LastLoginAt`, password/security stamp/forced-change flag, and active credential revocation timestamps mutate. Session identity/device/issued-at, expired/revoked history, entities, mappings and migrations remain unchanged.
- Idempotency/concurrency: forced-change same-payload retries resolve to replay, changed payload to conflict, and controlled races commit/audit once. Replay handling uses token ID scope, authoritative token rowversion, session rowversion enforcement and exactly-one idempotency/audit outcome. Initial issuance rolls back partial effects on stale/unique conflicts.
- Audit/secrets: audit snapshots expose only `must_change_password` booleans; replay audit has no snapshots, token/hash or attributed actor. Plaintext credentials never cross the repository contract; password hash is fingerprinted again before idempotency persistence.
- Test adequacy: real SQL proves rollback, persisted state, append-only audit counts and deterministic concurrency barriers. Existing P2-10 and full suites remain green. Hosted CI was not run because CI files were unchanged and no publication authority exists.
- Findings/gaps: `VG-01`/`VG-02` remain addressed awaiting independent verification. The persistence ownership blocker is resolved for the approved paths. Services/API composition and AC-01/04/05/06/08/09/10 completion remain future P1-10 work, so this is not a full-task submission.
- Status: `In Progress`; Codex Implementer does not mark `Ready for review` or `Done` for this intermediate persistence slice. Person 2 has been notified in `planning/RoadGuard_Plan_Person_2.md` and `docs/worklogs/P2-10-completion.md`; P2-10 remains historical `Done`.

### Round-3 exact working-tree identity

Implementation/plan manifest SHA-256: `788b76991ea65b8616b70897593e388e7bdf54c513375db36b84197850e68d3f`, calculated from the following LF-joined entries as `<file-sha256><two spaces><path>`. Mutable P1-10/P2-10 worklogs are excluded; no staged files, commit, merge, push, deployment or publication exists.

```text
92e0e51ee73550cca287c844b8ca0d1252aaa00ed93589ac571f3681ec829b52  RoadGuardSystem.API/Constants/ApiErrorCodes.cs
59c29f945dd3e2921f1a8728a2496630d7a6364c58da60fa2531e32807c27cbf  RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj
62b8a4bbe539ee1905ea4b9cbb51522edb7601b848d286d0ed15c46cc58306c9  RoadGuardSystem.Repositories/Identity/IIdentityRepository.cs
d7088ed4f2f12e45af27d1ece2d91d157b5f8f3335290d0b9403a87e9d1fd49d  RoadGuardSystem.Repositories/Identity/IdentityRepository.cs
dc41170d6c092b806ef1f5eb35e252e54a806d39355aa8263676626a261a7cd7  RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj
e2858882114a92469dbc5f7c0d8b0b5e7c41cd3e88c0c74a7f0385dae7a1068d  RoadGuardSystem.Services/Authentication/AccessTokenFactory.cs
b14fa8f74dd8c25706cc01de9135b279ce3fd4fa7a33121c55b1160fd5889949  RoadGuardSystem.Services/Authentication/AuthoritativeSessionValidator.cs
3f35ac5340ae6df73fd704eae380d872d327f0378e3530b5709039e0de729aec  RoadGuardSystem.Services/Authentication/JwtOptions.cs
d870cb2512481e4535ec77608fd0e699b5f7a2c3dea6ef79a98fb8eae2e8e26b  RoadGuardSystem.Services/Authentication/RefreshTokenGenerator.cs
fe21058ad7a559ba4dd005deca530358191038591ea5f9898215b73fd9bdd348  docs/api-errors.md
39627767274e365fb785b1bf36ba394f254c225b53aca7d72338862398aa2721  planning/RoadGuard_Plan_Person_1.md
1d37bffed131af4fd1093cdefd293c357c7ad331872c07fdf89cf1ec77ea9c3b  planning/RoadGuard_Plan_Person_2.md
7d9f320cadd975f2de4189b93a1182ede06aeaecea7350bdff4986e74b9f0e6b  tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj
a0fbc9c267f5184822068cb37a7e4f03f7348041b4bccedaddbba96ed2f70f0a  tests/RoadGuardSystem.UnitTests/Authentication/AccessTokenFactoryTests.cs
f29fac90026024d1c51cc3a0de47f5b1ce7317ddb4b6fd2a9cc2bddf59b3f5ed  tests/RoadGuardSystem.UnitTests/Authentication/AuthoritativeSessionValidatorTests.cs
0ad39badf5f80057b19cb6416316c0417401d7cf5c08be28663cd31b05e56803  tests/RoadGuardSystem.UnitTests/Authentication/JwtOptionsTests.cs
4667904f149b7eab55259aa36663cf98530a54e3cdc6ba2141769fdac9d322c0  tests/RoadGuardSystem.UnitTests/Authentication/RefreshTokenGeneratorTests.cs
416c20c62c3ae0fa481a20465c021a4b3a51f21dfd309e97aea176b55a54b4a7  tests/RoadGuardSystem.IntegrationTests/Identity/P110AuthenticationPersistenceTests.cs
```

### Round-3 maintainability refactor - IdentityRepository split

- Owner-approved bounded refactor: the 782-line repository implementation was split into partial files without changing `IIdentityRepository`, DI registration, SQL behavior, status mappings, transaction boundaries, audit payloads, or tests.
- Resulting sizes: `IdentityRepository.cs` 11 lines; `Reads` 89; `SessionIssuance` 88; `RefreshTokens` 220; `PasswordChanges` 166; `RoleChanges` 206; `Helpers` 51.
- No new behavioral RED applies because this is a move-only refactor. The already-green P1-10/P2-10 SQL suites were the characterization baseline and were rerun after the split.
- Post-refactor checks: P1-10 SQL 14/14, P2-10 SQL 39/39, non-incremental build 9 projects with 0 warnings/errors, format verification pass, full solution 289/289 with 0 failed/skipped, and no pending EF model changes.
- Self-review: capability ownership is explicit, helpers remain private across the partial type, no new service/DI abstraction was introduced, and the temporary Person 2 ownership notice now covers `IdentityRepository*.cs`.
- Concurrent verifier limitation: dependency security, `git diff --check`, method inventory and manifest reproduction pass. The final documentation/planning rerun exits 1 because the concurrent P1-08 owner added a current status row without its assignment-table definition (`PLAN_STATUS: undefined current task P1-08`). P1-08 has explicit non-overlapping ownership; P1-10 does not modify or claim that tooling artifact. Earlier documentation/planning evidence predates this unrelated drift and is not reused as a current pass.
- The round-3 manifest below supersedes the pre-refactor `788b7699...` identity; mutable worklogs remain excluded.
- Concurrent unrelated content: the current P1 plan hash includes the owner-approved P1-08 tooling row written by its separate owner. P1-08 owns only skill/worklog/its plan-row paths and does not overlap this refactor; no P1-08 artifact is attributed to or included in P1-10 scope.

Post-refactor implementation/plan manifest SHA-256: `6129544861d27a13f1e2216b45d049216047d26e41a1be3f0e3f9f0070ac9283`.

```text
92e0e51ee73550cca287c844b8ca0d1252aaa00ed93589ac571f3681ec829b52  RoadGuardSystem.API/Constants/ApiErrorCodes.cs
59c29f945dd3e2921f1a8728a2496630d7a6364c58da60fa2531e32807c27cbf  RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj
62b8a4bbe539ee1905ea4b9cbb51522edb7601b848d286d0ed15c46cc58306c9  RoadGuardSystem.Repositories/Identity/IIdentityRepository.cs
ddc85705e32719040e3f07d7d1b259dc6eb13222360b5f71c9a0b44dfafa799d  RoadGuardSystem.Repositories/Identity/IdentityRepository.cs
4f64a478155ce54876357ab9424971f28f7a35d20ec94d5d0a8d00a8286eda86  RoadGuardSystem.Repositories/Identity/IdentityRepository.Helpers.cs
385a93d10b77c005af582292ea8b488df0f5a940a79cf5637ac618ab0bc5e892  RoadGuardSystem.Repositories/Identity/IdentityRepository.PasswordChanges.cs
6df2e03ca55b1cec01a4e3dbe0e25bd477755d017120f17a887a2523800a6763  RoadGuardSystem.Repositories/Identity/IdentityRepository.Reads.cs
7b34019474327deb2d75c25f4fb5b7ce566f42301e5437e8d2c74fefbafd61f8  RoadGuardSystem.Repositories/Identity/IdentityRepository.RefreshTokens.cs
dc29e8de11d249e23f036972bea65bf4314a86d7f5abd1435674eddfd159bd0e  RoadGuardSystem.Repositories/Identity/IdentityRepository.RoleChanges.cs
161a262145f7c5101f7348f4cf5b203e65a75880daa497378c76ed8c0eea03e0  RoadGuardSystem.Repositories/Identity/IdentityRepository.SessionIssuance.cs
dc41170d6c092b806ef1f5eb35e252e54a806d39355aa8263676626a261a7cd7  RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj
e2858882114a92469dbc5f7c0d8b0b5e7c41cd3e88c0c74a7f0385dae7a1068d  RoadGuardSystem.Services/Authentication/AccessTokenFactory.cs
b14fa8f74dd8c25706cc01de9135b279ce3fd4fa7a33121c55b1160fd5889949  RoadGuardSystem.Services/Authentication/AuthoritativeSessionValidator.cs
3f35ac5340ae6df73fd704eae380d872d327f0378e3530b5709039e0de729aec  RoadGuardSystem.Services/Authentication/JwtOptions.cs
d870cb2512481e4535ec77608fd0e699b5f7a2c3dea6ef79a98fb8eae2e8e26b  RoadGuardSystem.Services/Authentication/RefreshTokenGenerator.cs
fe21058ad7a559ba4dd005deca530358191038591ea5f9898215b73fd9bdd348  docs/api-errors.md
bf1284d171e2586829b837519cd60ce047fa501d47716466311fecf65797e0aa  planning/RoadGuard_Plan_Person_1.md
1d37bffed131af4fd1093cdefd293c357c7ad331872c07fdf89cf1ec77ea9c3b  planning/RoadGuard_Plan_Person_2.md
7d9f320cadd975f2de4189b93a1182ede06aeaecea7350bdff4986e74b9f0e6b  tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj
a0fbc9c267f5184822068cb37a7e4f03f7348041b4bccedaddbba96ed2f70f0a  tests/RoadGuardSystem.UnitTests/Authentication/AccessTokenFactoryTests.cs
f29fac90026024d1c51cc3a0de47f5b1ce7317ddb4b6fd2a9cc2bddf59b3f5ed  tests/RoadGuardSystem.UnitTests/Authentication/AuthoritativeSessionValidatorTests.cs
0ad39badf5f80057b19cb6416316c0417401d7cf5c08be28663cd31b05e56803  tests/RoadGuardSystem.UnitTests/Authentication/JwtOptionsTests.cs
4667904f149b7eab55259aa36663cf98530a54e3cdc6ba2141769fdac9d322c0  tests/RoadGuardSystem.UnitTests/Authentication/RefreshTokenGeneratorTests.cs
416c20c62c3ae0fa481a20465c021a4b3a51f21dfd309e97aea176b55a54b4a7  tests/RoadGuardSystem.IntegrationTests/Identity/P110AuthenticationPersistenceTests.cs
```

## Codex acceptance review Round 2 - current persistence slice after the partial-file refactor

- Reviewer: independent Codex task `01a0b7df-0838-7c31-8c59-d648c7c24f98`, Person 1 acceptance, `2026-09-19T11:20+07:00`. This task did not author production/test artifacts. This is acceptance-review Round 2, distinct from Implementer rounds 2 and 3 preserved above.
- Review request/handoff: the owner requested review of current P1-10. The implementation task `01a0b79c-f740-7410-82e2-99953a452e91` has completed its last refactor turn and is idle; the independent P1-08 writer is also idle. The latest implementation explicitly remains a partial `In Progress` slice, not a full-task `Ready for review` submission. Review/status ownership is limited to this worklog and the P1-10 row; production/tests and P1-08/P2 metadata remain untouched.
- Baseline: branch `anh`, HEAD `7513d576dac2b0d470059973aff70b1043828c9e`, no staged changes. Reviewed all 22 implementation/test/error-documentation entries in the post-refactor manifest, including untracked files. Every one matches its recorded SHA-256. Excluding the two mutable plan files, the LF-joined implementation manifest hash is `e964f5b0d7532991957b25d55a96faa69271dbb9469a47e50f93501ed2719b46`. Local reproduction is in `artifacts/p110-review/implementation-manifest.txt`; the complete per-file entries remain in the post-refactor manifest above.
- Manifest drift: the original 24-entry post-refactor aggregate `61295448...` no longer matches only because P1-08 subsequently completed its entries in the shared P1 plan. Before reviewer metadata writes, the current 24-entry aggregate was `e91d84a5eed063138855a9600726494b02895e359c7836e0af1c799180aa44f7`; P2 plan and all 22 covered artifacts match. Do not attribute unrelated skill/worklog changes to P1-10 or reuse the old aggregate as the current whole-plan identity.
- Dependencies/ownership: P1-01/P1-02 and P2-10 artifacts are present at this HEAD. The recorded no-schema P1 persistence exception resolves the earlier ownership blocker. P2-10 historical Done is preserved. No schema/migration/model change is introduced by this diff.

### Mandatory code findings

1. **F-01 / [P1] / Open / Person 1, P1-10 persistence exception - rebuild transaction state before retrying a rolled-back attempt.** Location: `RoadGuardSystem.Repositories/Identity/IdentityRepository.RefreshTokens.cs:156-157,189-192`; same root cause in `IdentityRepository.PasswordChanges.cs:59-74,159-162`. `SaveChangesAsync` accepts tracked changes before the explicit commit; generic failure handling rolls back SQL but keeps that tracked state, and the execution strategy reuses the same context. Concrete failure path: a retryable failure between successful SaveChanges and a commit that did not persist the replay transaction leaves Session/token revocations marked Unchanged in memory. On retry, the tracking query returns those objects, both revocation guards skip them, and the new audit/idempotency rows can commit with a Success result while the persisted Session and active sibling remain unrevoked. The idempotency receipt then suppresses later revocation attempts. For forced password change, a retryable failure in the session query after setting MustChangePassword=false similarly causes the next attempt to return NotRequired although the database still requires the change. Production DI enables retries (MaxRetryCount defaults to 3), whereas the SQL test fixture does not enable them. This violates AC-03/AC-07 atomic state/audit and fail-closed retry requirements. Evidence: code-path analysis, not an executed fault-injection reproduction. Closure: isolate/reconstruct tracked state for each attempt, resolve ambiguous commits from durable outcome evidence, and add real-SQL regression checks with retries enabled for failures before SaveChanges and between SaveChanges/commit; Success/replay must imply durable credential revocation and exactly one audit. Inspect the same retry boundary in initial issuance when fixing this root cause.
2. **F-02 / [P2] / Open / Person 1, P1-10 persistence exception - resolve a concurrent winner before returning NotRequired.** Location: `RoadGuardSystem.Repositories/Identity/IdentityRepository.PasswordChanges.cs:59-62` (initial idempotency lookup at lines 34-41). Two identical operations can both observe no idempotency record; the first then commits before the second reads the User. The second sees MustChangePassword=false and returns NotRequired without resolving the now-persisted matching operation. It never reaches the concurrency/unique-conflict handlers that map a winner to IdempotentReplay. This makes duplicate delivery timing-dependent and violates AC-03 and the round-3 same-payload retry contract. The existing barrier test synchronizes at UPDATE Users, so it only covers the ordering where both requests already read the old user state. Evidence: deterministic interleaving traced through code, not a newly executed race test. Closure: resolve the matching winner on the early no-longer-required path while retaining changed-payload conflict behavior; add a two-context SQL regression pausing the second request after the initial idempotency miss until the first has committed, and assert one Success, one IdempotentReplay, and one audit.

EF behavior was cross-checked against official [connection resiliency guidance](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency) and [tracking-query semantics](https://learn.microsoft.com/en-us/ef/core/querying/tracking), using direct read-only HTTP retrieval. Microsoft Learn/Context7 tools were not exposed; the web tool returned a provider error, so official pages were retrieved directly. This research does not substitute for the missing fault-injection tests.

### Existing gaps and AC disposition

- `VG-01`: **Verified closed**. Exact issued-at/not-before/configured expiry assertions and distinct parseable JWT IDs are present and passed in the 25-test authentication rerun.
- `VG-02`: **Unit portion verified; end-to-end portion open**. Empty claims, Unknown role, absent user/session, wrong owner, revoked session, expiry boundary, inactive account, stale-role revocation and store failure are covered. The original closure also requires bearer/API requests against authoritative SQL state and proof that a rejected request cannot invoke the protected action; those artifacts do not yet exist.
- `VG-03`: **Open - incomplete full-task deliverables, Person 1/P1-10.** Auth DTOs, credential/password-policy orchestration, AuthController, JWT bearer registration, per-request validator wiring, and authentication API/SQL tests are absent. Program.cs still contains only the platform foundation. This is the implementer's acknowledged unfinished scope, not a fabricated regression in unchanged startup code. Complete the assigned flows and full review packet before resubmission for Done.

| AC | Current disposition |
|---|---|
| AC-01 | Open: no credential-validation application/API flow or four-role API matrix. |
| AC-02 | Partial: initial issuance SQL primitive and existing negative/positive cases pass; login orchestration, hash-only API proof, configured expiries and failure/retry proof remain. |
| AC-03 | Partial, Changes requested: persistence/audit and existing rollback/duplicate cases pass; F-01/F-02 remain; policy/current-credential/forced-change API flow is absent. |
| AC-04 | Partial: generation/options primitives and VG-01 checks pass; bearer validation, startup integration and full configuration failure matrix remain. |
| AC-05 | Partial: validator unit matrix passes; bearer/HTTP/SQL next-request enforcement remains (VG-02). |
| AC-06 | Partial: existing rotation dependency regression passes; composed refresh flow and API/SQL token-pair behavior remain. |
| AC-07 | Partial, Changes requested: ordinary replay/duplicate SQL cases pass; F-01 blocks safe retry acceptance and public replay handling remains absent. |
| AC-08 | Open: logout application/API behavior and repeated-logout proof remain. |
| AC-09 | Partial: stable error constants/docs exist; auth DTO/OpenAPI/ProblemDetails and sensitive-output/log tests remain. |
| AC-10 | Partial: Google package removals are present; auth/persistence/Identity composition and production-like startup proof remain. |

### Verification evidence

Environment: Windows, PowerShell 7.6.5, SDK 10.0.401, net8.0; existing configured SQL Server fixture creates and drops isolated GUID-named test databases. No shared application database was targeted. All commands below exited 0; no required test was skipped.

| Reviewer command | Time (+07:00) | Result |
|---|---|---|
| `dotnet test tests/RoadGuardSystem.UnitTests --no-restore --filter "FullyQualifiedName~RoadGuardSystem.UnitTests.Authentication" --logger "trx;LogFileName=p110-review-unit.trx" --results-directory artifacts/p110-review` | 2026-09-19 11:15:20 | 25 passed, 0 failed/skipped; normal build included. |
| `dotnet test tests/RoadGuardSystem.IntegrationTests --no-restore --filter "TaskId=P1-10\|TaskId=P2-10" --logger "trx;LogFileName=p110-review-sql.trx" --results-directory artifacts/p110-review` | 2026-09-19 11:15:39-11:15:50 | 53 passed: P1-10 14, P2-10 39; 0 failed/skipped; normal build included. The filter contains a literal pipe (Markdown escaping only). |
| `pwsh -NoProfile -File tests/Documentation/Verify-P102Docs.ps1` | 2026-09-19 11:17-11:18 | Passed. Earlier P1-08 undefined-row failure is no longer present. |
| `pwsh -NoProfile -File tests/Documentation/Test-P203Planning.ps1` | 2026-09-19 11:19 | 9/9 scenarios passed. |
| `git diff --check`; per-file SHA-256 comparison | 2026-09-19 11:17 | Passed; covered implementation content matches as recorded above. |

- Inspected, not rerun: implementer RED/GREEN chronology, self-review, post-refactor restore/non-incremental build/format/full-suite 289 results, model-drift and dependency-security evidence. Runtime inputs match the recorded manifest and SDK, so these remain historical regression evidence for this slice; they do not prove absent auth endpoints or F-01/F-02 interleavings. No full-task acceptance claim is made, and no production/test fix or new test was authored by the reviewer.
- Verdict: **Changes requested**, not Done. Record F-01/F-02 as mandatory fixes and keep VG-02/VG-03 open. The previous ownership blocker is resolved; remaining issues are implementation and verification work within the approved scope.
- Bounded next action: Codex Implementer resumes P1-10 as In Progress, reproduces/fixes F-01/F-02 with the specified regression cases, completes the already assigned application/API ACs, refreshes affected SQL/security/submission evidence, self-reviews and freezes a new exact artifact identity for independent review. No schema/ownership expansion is requested.
- Metadata disposition: update only the P1-10 current row from In Progress to Changes requested after recording this review. Preserve other task entries and historical evidence; no commit, merge, push or publication.

## Codex Implementer round 4 - F-01/F-02 and full authentication completion

### Start state and bounded scope

- Implementer / Person / branch: Codex Implementer / Person 1 / `anh`.
- Baseline: HEAD `7513d576dac2b0d470059973aff70b1043828c9e`; working tree contains the preserved P1-08 tooling submission and the existing uncommitted P1-10 artifacts enumerated by `git status --short --branch`.
- Status transition: `Changes requested -> In Progress` at `2026-09-19T11:31:51+07:00`.
- Stable findings: fix `F-01` and `F-02` exactly as opened in independent review Round 2; only an independent reviewer may verify closure.
- In scope: retry-safe atomic repository operations without schema changes; login, refresh, logout, forced password change, JWT bearer validation, authoritative Session/User checks, DTOs, ProblemDetails mapping, DI/OpenAPI composition, and required unit/API/real-SQL evidence for AC-01 through AC-10.
- Out of scope remains unchanged: profile/Admin reset, project membership authorization, account/role administration, schema/migrations/entity shape, SSO/MFA/cache, FE, deployment, Git integration, and publication.
- Exclusive paths remain those declared in the assignment plus the owner-approved P1-10 persistence exception recorded in the P2-10 worklog. Shared plan writes are limited to the P1-10 row; P1-08 entries and artifacts are preserved.
- Planned RED order: retry rollback state (`F-01`), post-idempotency-miss duplicate ordering (`F-02`), application decision matrices, then bearer/API/SQL end-to-end contracts. Submission evidence will be recorded once after final content stabilizes.

### Round-4 implementation and finding dispositions

- Supersession notice: the 49-file manifest and Ready-for-review wording below were drafted before final self-review completed. They are not the final submission identity. Final evidence and manifest are appended after the additional AC-08/F-01 regressions.

- `F-01` addressed, awaiting independent verification: each execution-strategy attempt clears stale tracked state; initial issuance reconstructs Session/RefreshToken entities for every attempt; forced-change and replay attempts reload durable idempotency outcomes. Real-SQL pre-commit transient failures now prove successful issuance/change/replay implies durable state, credential revocation and exactly one audit.
- `F-02` addressed, awaiting independent verification: the early `MustChangePassword == false` path clears tracked state and resolves the durable winner before returning `NotRequired`. The controlled two-context ordering now returns one `Success`, one `IdempotentReplay`, and one audit.
- Application/API completed: ASP.NET Identity credential verification/password policy, HMAC JWT/opaque refresh issuance, login, rotation/replay, logout, forced change, authoritative bearer Session/User/role checks, versioned DTO-only endpoints, ProblemDetails, bearer OpenAPI metadata and fail-fast DI/options composition.
- Additional self-review fix: forced-change semantic fingerprints are HMAC-derived from a purpose-separated key and the user/new-password payload, so randomized Identity hashes/security stamps do not turn a same-payload retry into a conflict. Plaintext is neither stored nor returned in the fingerprint.

### Round-4 RED/GREEN chronology

| Slice | Behavioral RED | GREEN |
|---|---|---|
| `F-01` retry state and `F-02` post-miss race | Focused SQL command exited 1 with 4/4 failures: replay returned success while Session stayed active; forced change returned `NotRequired`; issuance returned `StaleConcurrency`; the ordered duplicate returned `NotRequired`. | Tracker reset, per-attempt entity reconstruction and durable-winner resolution; the same filter passed 4/4. |
| Auth application orchestration | Type-first run failed compilation and is not behavioral RED. After minimal contract scaffolding, 7/7 service tests failed on `InvalidInput`, proving absent login/refresh/logout/forced-change decisions. | Implemented orchestration; service tests passed 7/7, and final authentication unit filter passed 33/33. |
| HTTP/SQL auth flow | Initial API filter ran 4/4 failures with 404 because auth routes were absent. The first post-wiring run failed at test-host configuration before endpoint execution and is not behavioral RED. | Added DTO/controller/DI/JWT bearer/SQL-backed factory; initial flow filter passed 4/4 and final authentication API matrix passed 16/16. |
| Forced-change public replay | Repeating the same operation after a successful change returned 401 because only the old current password was checked. | Replacement-as-current verification plus durable semantic fingerprint replay returns 204 and retains exactly one audit. |
| OpenAPI bearer contract | Logout operation lacked operation-level bearer security metadata. | Attribute-aware operation filter leaves anonymous auth endpoints unsecured in OpenAPI and marks logout with Bearer; focused test passed 1/1. |

### Round-4 AC coverage

| AC | Evidence |
|---|---|
| AC-01 | SQL-backed API rejects blank/unknown/wrong/inactive credentials generically and allows all four canonical roles; service/controller never return password state. |
| AC-02 | Login API persists one Session and only the 64-character refresh hash; initial issuance rollback, duplicate credential and retry regressions prove atomicity and configured expiration. |
| AC-03 | Forced account receives 403/no tokens; wrong/reused/weak/mismatched inputs fail; successful change updates Identity hash, clears the flag, revokes credentials, audits once, replays idempotently and requires fresh login. F-01/F-02 regressions cover rollback and concurrency. |
| AC-04 | JWT unit tests cover required claims, canonical role, unique `jti`, exact configured times and key id; startup/options and OpenAPI tests cover fail-fast key/config composition. |
| AC-05 | Unit decision matrix plus real-SQL bearer tests cover missing/revoked/expired Session, inactive User and stale role. Stale role revokes the family; protected action is not reached; current matching state succeeds. `VG-02` end-to-end portion is addressed, awaiting review. |
| AC-06 | Input is hashed before lookup; SQL-backed API proves rotation and hash-only storage. Concurrent duplicate refresh produces exactly one credential response and one rejected replay, then revokes the family. |
| AC-07 | Repository retry/concurrency tests and API concurrent replay prove entire-family revocation, exactly one sanitized `auth_token_replay_detected` audit and no credential response to the loser. |
| AC-08 | Authenticated logout returns 204, revokes Session/family, and immediately rejects the old access and refresh credentials. Repository operation is idempotent. |
| AC-09 | Versioned DTO-only endpoints, model validation, 400/401/403/409 ProblemDetails codes/correlation IDs, OpenAPI boundary, response scans and production-source secret/log scans pass. |
| AC-10 | Deferred Google packages remain removed; production-like startup, auth/persistence/Identity DI, Swagger, dependency-vulnerability checks and full solution gate pass. |

### Round-4 files and exact artifact identity

- Baseline/HEAD: `7513d576dac2b0d470059973aff70b1043828c9e` on `anh`; no commit, merge, push or publication; staged files: none.
- Submitted implementation identity: 49 files, aggregate SHA-256 `1662f44dcca1591666909809c1a2a5aab2b6f9a15eb905a6bc7b931a44f569a7`, captured `2026-09-19T12:02:32+07:00`. Recompute by sorting the exact paths below, formatting each line as `<lowercase-file-sha256><two spaces><forward-slash-path>`, joining with LF and no final LF, then SHA-256 hashing the UTF-8 payload.
- Production/docs paths: `RoadGuardSystem.API/Authentication/JwtBearerConfiguration.cs`; `Constants/ApiErrorCodes.cs`; `Controllers/AuthController.cs`; `Extensions/AuthorizeOperationFilter.cs`; `Extensions/ConfigureSwaggerOptions.cs`; `Extensions/ServiceCollectionExtensions.cs`; `Program.cs`; API csproj; four `RoadGuardSystem.DTOs/Authentication/*.cs` files; `IIdentityRepository.cs`; `IdentityRepository.cs` plus six capability partials; nine `RoadGuardSystem.Services/Authentication/*.cs` files; Services csproj; `docs/api-errors.md`.
- Test paths: two API Authentication test files; API `ProbeController.cs`; three API factories/fixtures; API test csproj and startup test; `P110AuthenticationPersistenceTests.cs`; `FailFirstCommitInterceptor.cs`; `IdentitySqlServerFixture.cs`; `PauseOnceCommandInterceptor.cs`; five authentication unit-test files; UnitTests csproj.
- Mutable metadata excluded from the implementation aggregate: this worklog, the P1-10 plan row, and historical P2-10/P1-10 ownership notices. Unrelated P1-08 skill/worklog artifacts are explicitly outside this submission. `git status --short` and the path groups above identify tracked modifications and relevant untracked files; no file is staged.

### Round-4 commands and results

Environment: Windows/PowerShell, SDK 10.0.401 targeting net8.0, `ROADGUARD_TEST_SQL_SERVER_CONNECTION_STRING` set to an authorized configured SQL Server; isolated GUID-named databases were created/dropped by fixtures. Evidence completed `2026-09-19T12:03:57+07:00`.

| Command/check | Exit | Result |
|---|---:|---|
| Focused `F-01`/`F-02` SQL filter before fix | 1 | Behavioral RED: 0 passed, 4 failed, 0 skipped with the four expected state/outcome failures. |
| Same focused SQL filter after fix | 0 | 4 passed, 0 failed/skipped. |
| `dotnet test tests/RoadGuardSystem.UnitTests/... --filter FullyQualifiedName~RoadGuardSystem.UnitTests.Authentication` | 0 | 33 passed, 0 failed/skipped. |
| `dotnet test tests/RoadGuardSystem.ApiTests/...` | 0 | 43 passed, 0 failed/skipped; includes 16 SQL-backed auth-flow cases and OpenAPI coverage. |
| `dotnet test tests/RoadGuardSystem.IntegrationTests/... --filter "TaskId=P1-10|TaskId=P2-10"` | 0 | 57 passed, 0 failed/skipped: P1-10 18 and accepted P2-10 39. |
| `dotnet restore RoadGuardSystem.slnx` | 0 | All projects restored/up to date. |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | 9 projects; 0 warnings, 0 errors. |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | No formatting changes required. |
| `dotnet test RoadGuardSystem.slnx --no-build --no-restore` | 0 | 318 passed, 0 failed/skipped: Unit 118, API 43, SQL Integration 157. |
| API, Services, Repositories and API-test dependency vulnerability checks | 0 | No vulnerable packages from configured sources. |
| EF `has-pending-model-changes` with a non-secret design-time setting | 0 | No model changes since the accepted migration; no schema/migration artifact changed. |
| `Verify-P102Docs.ps1` | 0 | Final rerun passed. An earlier run found only the changed compatibility heading token; documentation was corrected and no runtime evidence was invalidated. |
| `Test-P203Planning.ps1` | 0 | 9/9 scenarios passed. |
| `git diff --check`; production secret/log regex scans | 0 / expected rg 1 | No whitespace errors; scans found no embedded credential/private key/connection string and no auth-layer logging/console sink. The first scan command had a PowerShell quoting parser error and is diagnostic only; corrected commands produced no matches. |

### Round-4 owner self-review and handoff

- Authorization: JWT role is only a snapshot; every protected request loads authoritative Session and User state. Stale roles revoke the family. Project membership remains P1-12 and is neither trusted nor implemented here.
- State/transitions: only active, non-forced users receive credentials; forced change requires current/replacement credential proof and fresh login; refresh has one winner; logout/replay revoke credentials immediately.
- Immutability/versioning: device metadata is validated/write-once, refresh plaintext is response-only, rowversions participate in persistence decisions, and no entity shape/schema/migration changed.
- Idempotency/concurrency/retry: F-01/F-02 regressions cover stale tracker rollback, durable winner lookup and two-context ordering. Forced-change semantic fingerprints distinguish changed payloads despite randomized Identity hashes; replay/logout effects do not duplicate audit or credentials.
- Audit/secrets: replay and forced-change audits are append-only and sanitized; no password/token/hash/signing key/device metadata is logged or returned outside the one-time credential response. Signing/database secrets remain external configuration.
- Missing tests/gaps: no required local gate is missing, zero-discovered or skipped. Hosted CI, deployment/key provisioning and FE integration were not run because no push/deployment authority was granted; they do not replace or weaken the completed local API/SQL/security gates.
- Self-review disposition: no unresolved in-scope finding or blocker. `F-01`, `F-02`, `VG-02` end-to-end and `VG-03` deliverables are addressed but only the independent reviewer may verify closure. Status is `Ready for review`; submitted implementation/test artifacts are frozen.

### Ready-to-run independent reviewer prompt - Round 3

```text
Bạn là Codex Reviewer độc lập cho RoadGuard P1-10, Person 1, branch anh.
Baseline: 7513d576dac2b0d470059973aff70b1043828c9e. Submission: 49-file working-tree implementation manifest SHA-256 1662f44dcca1591666909809c1a2a5aab2b6f9a15eb905a6bc7b931a44f569a7; no staged files; include relevant untracked files. Worklog: docs/worklogs/P1-10-completion.md.

Phiên này phải độc lập và không được là phiên đã author artifacts. Đọc AGENTS.md, dùng roadguard-review và roadguard-review-p1. Kiểm tra status/HEAD, assignment AC-01..AC-10, P2-10 dependency/no-schema exception, exact 49-file path set/aggregate, self-review và các review rounds trước. Không sửa production/tests; chỉ ghi review/status metadata đúng task.

Xác minh F-01 bằng các retry-enabled real-SQL cases cho initial issuance, forced change và replay: rollback phải dựng lại tracker/object graph; Success/replay phải đi kèm durable credential state và đúng một audit. Xác minh F-02 bằng interleaving request thứ hai miss idempotency, winner commit, rồi loser đọc User: kết quả phải là IdempotentReplay, không NotRequired. Kiểm tra semantic forced-change fingerprint và public duplicate retry.

Đối chiếu từng AC với login/refresh/logout/forced-change, bearer authoritative User/Session/role validation, ProblemDetails, DTO/OpenAPI, hash-only persistence, concurrent refresh/replay/audit và fail-fast configuration. Rerun regression mới/high-risk; chỉ reuse evidence khi manifest/environment khớp. Required SQL/security evidence không được waive; phân biệt rerun và inspected evidence.

Giữ IDs F-01/F-02/VG-02/VG-03. Finding mới phải có severity, owner, file/dòng, trigger/impact/AC/closure. Nếu mọi AC, checks, self-review và findings đạt, append review round và update P1-10 Ready for review -> Done; nếu không, ghi Changes requested/Blocked đúng bằng chứng. Không commit/merge/push/deploy.
```

### Round-4 final superseding submission after self-review reopen

- Pre-handoff review supersession: the 50-file identity below was independently recomputed but was not accepted. Read-only pre-handoff review left F-01 ambiguous commits open and raised F-03/F-04/F-05 below. Its Ready-for-review wording and 50-file identity are superseded by the next fix-round submission.

- Self-review reopen result: added an exact transient failure in the Session query after User mutation but before `SaveChanges`; it passes and proves tracker reconstruction at the pre-save boundary. Added an AC-08 duplicate-logout regression that initially failed because `RevokedAt`/rowversion were rewritten, then changed the repository operation to a no-op for an already revoked Session and to accept a concurrency loser only after observing durable revocation. API retry returns 401 `auth_session_revoked` and cannot invoke another logout write.
- Final status: `Ready for review`. This section supersedes the earlier 49-file draft identity and its 318-test count. All submitted production/test artifacts are now frozen; only task-scoped reviewer metadata may change.
- Final exact implementation identity: 50 sorted production/test/docs files, aggregate SHA-256 `8b1821603968cca8d2fd2c34ab57b694c6bf40b174436afbb60fedba43c9ffca`, captured `2026-09-19T12:12:06+07:00`. It uses the same line/UTF-8/LF algorithm documented above, with the prior 49 paths plus `tests/RoadGuardSystem.IntegrationTests/Infrastructure/ThrowOnceCommandInterceptor.cs`. Changed final file hashes include `IdentityRepository.RefreshTokens.cs` `6c83ab80029d6f1411a4205fa276eb7865f16241ce42fe66a5b242a0028e0b07`, `P110AuthenticationPersistenceTests.cs` `248910a039d46817590c3865674a45e01914ddc7cddec2cf03296d50f1721804`, `AuthenticationFlowTests.cs` `cc8eb794287f5e66084a04105ac8edde89cb11f6fb052569327af7d5e88821ed`, and `ThrowOnceCommandInterceptor.cs` `4a529d93ecba1020cf89012424de673e51638633fcb68ff662a3b9d5c6e61563`.
- Final gate after those changes: restore exit 0; non-incremental build exit 0 with 0 warnings/errors; format verify exit 0; full solution exit 0 with **320 passed, 0 failed/skipped** (Unit 118, API 43, SQL Integration 159); P1-10/P2-10 SQL filter exit 0 with **59 passed, 0 failed/skipped**; `git diff --check` exit 0. Earlier package-vulnerability, EF model-drift, documentation, planning and corrected secret/log scans remain reusable because their covered package/model/docs/production-secret inputs did not change during the reopen.
- Findings/gaps final disposition: `F-01` addressed by both pre-save and pre-commit retry regressions; `F-02` addressed by the controlled post-idempotency-miss interleaving; `VG-02` end-to-end and `VG-03` assigned deliverables addressed. All await independent verification. No implementation blocker remains. Hosted CI/deployment/FE were not run and remain external/non-authorized.

### Ready-to-run independent reviewer prompt - final Round 3 submission

```text
Bạn là Codex Reviewer độc lập cho RoadGuard P1-10, Person 1, branch anh.
Baseline: 7513d576dac2b0d470059973aff70b1043828c9e. Final submission: 50-file working-tree implementation manifest SHA-256 8b1821603968cca8d2fd2c34ab57b694c6bf40b174436afbb60fedba43c9ffca; no staged files; include relevant untracked files. Worklog: docs/worklogs/P1-10-completion.md. Ignore the explicitly superseded 49-file draft identity.

Phiên này phải độc lập và không được là phiên đã author artifacts. Đọc AGENTS.md, dùng roadguard-review và roadguard-review-p1. Kiểm tra status/HEAD, assignment AC-01..AC-10, P2-10 dependency/no-schema exception, exact 50-file path set/aggregate, self-review và các review rounds trước. Không sửa production/tests; chỉ ghi review/status metadata đúng task.

Xác minh F-01 bằng retry-enabled real-SQL cases trước SaveChanges và giữa SaveChanges/commit cho initial issuance, forced change và replay: mọi attempt phải dựng lại tracker/object graph; Success/replay phải đi kèm durable credential state và đúng một audit. Xác minh F-02 bằng interleaving request thứ hai miss idempotency, winner commit, rồi loser đọc User: kết quả phải là IdempotentReplay, không NotRequired. Kiểm tra semantic forced-change fingerprint, public duplicate retry và AC-08 duplicate logout không rewrite revocation/rowversion.

Đối chiếu từng AC với login/refresh/logout/forced-change, bearer authoritative User/Session/role validation, ProblemDetails, DTO/OpenAPI, hash-only persistence, concurrent refresh/replay/audit và fail-fast configuration. Rerun regression mới/high-risk; chỉ reuse evidence khi manifest/environment khớp. Required SQL/security evidence không được waive; phân biệt rerun và inspected evidence.

Giữ IDs F-01/F-02/VG-02/VG-03. Finding mới phải có severity, owner, file/dòng, trigger/impact/AC/closure. Nếu mọi AC, checks, self-review và findings đạt, append review round và update P1-10 Ready for review -> Done; nếu không, ghi Changes requested/Blocked đúng bằng chứng. Không commit/merge/push/deploy.
```

### Round-4 final submission after read-only pre-handoff review

- Reviewer separation: read-only pre-handoff code reviewer `/root/p110_pre_handoff_review` did not author or modify artifacts and did not perform acceptance. It verified the exact 50-file draft, found F-01 still partial plus F-03/F-04/F-05 and adjacent F-06, then verified F-01/F-03/F-04/F-05 closed by the fix round. F-06 was subsequently reproduced and fixed. Mandatory repository acceptance remains assigned to a new independent Codex task.
- Stable dispositions awaiting acceptance verification:
  - `F-01` addressed: all four credential writes use `ExecuteInTransactionAsync` with durable `verifySucceeded`; real-SQL tests inject transient failures before SaveChanges, before commit, and after durable commit acknowledgment for issuance, forced change, replay and rotation.
  - `F-02` addressed: controlled post-idempotency-miss interleaving returns `IdempotentReplay`, not `NotRequired`.
  - `F-03` addressed: forced-change fingerprint uses a required dedicated external key, independent of JWT signing-key rotation; unit config validation and a two-host SQL/API rotation retry pass with one audit.
  - `F-04` addressed: AuthService and DbContext consume the same configured `SessionDeviceMetadataOptions`; strict-host input now returns 400 `validation_error` and persists no Session.
  - `F-05` addressed: every auth action publishes success and ProblemDetails response metadata; generated OpenAPI asserts DTO/schema/status/security contracts.
  - `F-06` addressed: rotation normalizes the returned replacement SessionId to the authoritative parent, matching SQL persistence; focused regression observed RED then GREEN.
  - `VG-02` end-to-end and `VG-03` assigned deliverables remain addressed. Only the independent acceptance reviewer may mark any finding Verified or task Done.

#### Final RED/GREEN additions

| Finding | RED | GREEN |
|---|---|---|
| F-01 ambiguous commit | `TransactionCommittedAsync` post-commit injection: 0/4 pass. Explicit rollback masked three transient outcomes with completed-transaction errors; rotation returned stale after durable commit. | Durable verification via `ExecuteInTransactionAsync`: 4/4 pass; state and audit counts are durable and non-duplicated. |
| F-03 key rotation | Same user/password fingerprint differed when `ActiveKeyId` changed; 0/1 pass. | Dedicated fingerprint key unit tests pass; SQL/API retry after active JWT key A -> B returns 204 twice with one audit. |
| F-04 configured metadata | Strict host accepted service validation then persistence threw; API returned 500. | Shared options validation returns 400 `validation_error`; no Session persists. |
| F-05 OpenAPI | Generated login operation lacked typed response content/status schemas. | OpenAPI exposes `AuthTokenResponseDto`, ProblemDetails, 200/400/401/403/409 and logout 204 with operation-level Bearer. |
| F-06 rotation result | Returned replacement kept the caller's wrong SessionId while SQL used the parent Session; 0/1 pass. | Returned and persisted replacement both use the authoritative old-token SessionId; 1/1 pass. |

#### Final artifact identity and verification

- Branch/baseline: `anh`, HEAD `7513d576dac2b0d470059973aff70b1043828c9e`; no staged files; no commit/merge/push/publication.
- Exact implementation/test/docs identity: 52 sorted files, aggregate SHA-256 `64c85ebc0a3a07421be672139a46e357551c3cfa66a0f0b094d6f327c75dad56`, captured `2026-09-19T12:43:48+07:00`. Use the previously documented lowercase SHA-256/two-space/path/LF/no-final-LF algorithm. This is the prior 50-file set plus `RoadGuardSystem.Services/Authentication/PasswordChangeFingerprintOptions.cs` and `tests/RoadGuardSystem.IntegrationTests/Infrastructure/FailFirstCommittedInterceptor.cs`. Mutable plan/worklog records and unrelated P1-08 artifacts remain excluded.
- Environment: Windows/PowerShell, SDK 10.0.401/net8.0, authorized configured SQL Server with isolated fixture databases.

| Final command/check | Exit | Result |
|---|---:|---|
| `dotnet restore RoadGuardSystem.slnx` | 0 | All projects restored/up to date. |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | 9 projects; 0 warnings/errors. |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | No formatting changes required. |
| `dotnet test RoadGuardSystem.slnx --no-build --no-restore` | 0 | **331 passed, 0 failed/skipped**: Unit 122, API 45, SQL Integration 164. |
| SQL filter `TaskId=P1-10|TaskId=P2-10` | 0 | **64 passed, 0 failed/skipped**: P1-10 25, accepted P2-10 39. |
| Vulnerability checks: API, Services, Repositories, API tests | 0 | No vulnerable packages from configured sources. |
| EF pending-model check | 0 | No model changes since accepted migration. |
| P1 docs / P2 planning verifiers | 0 | Passed; planning 9/9. |
| `git diff --check`; production secret/log scans | 0 / expected rg 1 | No whitespace error or embedded secret/log sink match. Dedicated fingerprint/JWT/database keys remain external configuration; test-only deterministic keys are synthetic fixtures. |

- Final self-review: authorization is authoritative per request; transitions and forced-change gating are explicit; Session metadata is write-once and consistently validated; plaintext credentials are response-only; audits are append-only/sanitized; retries cover known rollback and unknown commit; idempotency survives JWT key rotation; refresh/logout concurrency has one durable effect; DTO/OpenAPI boundaries expose no persistence entities. No required local gate is missing or skipped.
- Gaps/risks: hosted CI, deployment secret provisioning and FE integration did not run because no publication/deployment authority exists. No in-scope implementation blocker remains. Status is `Ready for review`; implementation/test artifacts are frozen.

### Ready-to-run independent reviewer prompt - authoritative final submission

```text
Bạn là Codex Reviewer độc lập cho RoadGuard P1-10, Person 1, branch anh.
Baseline: 7513d576dac2b0d470059973aff70b1043828c9e. Authoritative final submission: 52-file working-tree implementation manifest SHA-256 64c85ebc0a3a07421be672139a46e357551c3cfa66a0f0b094d6f327c75dad56; no staged files; include relevant untracked files. Worklog: docs/worklogs/P1-10-completion.md. Ignore the explicitly superseded 49-file and 50-file draft identities.

Phiên này phải là task/session độc lập, không phải implementer hoặc pre-handoff reviewer. Đọc AGENTS.md, dùng roadguard-review và roadguard-review-p1. Kiểm tra status/HEAD, AC-01..AC-10, P2-10 dependency/no-schema exception, exact 52-file path set/aggregate, self-review và mọi review/fix round. Không sửa production/tests; chỉ ghi task-scoped review/status metadata.

Xác minh F-01 trước-SaveChanges, trước-commit và post-durable-commit recovery cho issuance/forced change/replay/rotation; F-02 post-idempotency-miss race; F-03 retry qua JWT signing-key rotation với dedicated fingerprint key; F-04 configured metadata 400/no write; F-05 generated OpenAPI response schemas/statuses; F-06 returned-vs-persisted rotation SessionId. Giữ IDs F-01/F-02/F-03/F-04/F-05/F-06/VG-02/VG-03.

Đối chiếu từng AC với login/refresh/logout/forced-change, authoritative bearer User/Session/role validation, ProblemDetails/correlation, DTO/OpenAPI, hash-only persistence, concurrent refresh/replay/audit, retry/idempotency and fail-fast external secret configuration. Rerun regression mới/high-risk; chỉ reuse evidence khi manifest/environment khớp. Required SQL/security proof không được waive.

Finding mới phải có severity, owner, file/dòng, trigger/impact/AC/closure. Nếu mọi AC, checks, self-review và findings đạt, append acceptance round và update P1-10 Ready for review -> Done; nếu không, ghi Changes requested/Blocked đúng bằng chứng. Không commit/merge/push/deploy.
```

## Codex acceptance review Round 3 - authoritative 52-file submission

- Reviewer/time: independent Codex Reviewer task/session, Person 1 acceptance, `2026-09-19T13:01:21+07:00`. This reviewer did not author the submitted production/test artifacts and did not act as the pre-handoff reviewer.
- Reviewed artifact: branch `anh`, HEAD/baseline `7513d576dac2b0d470059973aff70b1043828c9e`, no staged files. The reviewer reconstructed the exact 52 sorted implementation/test/docs paths, including relevant untracked files, and reproduced aggregate SHA-256 `64c85ebc0a3a07421be672139a46e357551c3cfa66a0f0b094d6f327c75dad56`. The superseded 49/50-file identities were not used. P1-08 artifacts and mutable plan/worklog records remain outside the implementation identity.
- Dependencies/ownership: P1-01, P1-02 and accepted P2-10 artifacts are present in this checkout. The owner-authorized no-schema P1-10 persistence exception covers the submitted repository/SQL-test paths; no entity shape, mapping, migration or model-snapshot change exists. P2-01 and P1-08 own non-overlapping paths, so task-scoped reviewer metadata could be serialized without an active hotspot conflict.

### Finding and verification gap

1. **F-07 / [P2] / Open / Person 1, P1-10 - equalize credential-verification work for unknown and known usernames.** Location: `RoadGuardSystem.Services/Authentication/IdentityCredentialVerifier.cs:27-30`. Trigger: send repeated login attempts using a nonexistent username and an existing username with the same incorrect password. The nonexistent-user branch returns immediately after `FindByNameAsync`, while the existing-user branch additionally performs the expensive Identity password-hash verification. Impact: an attacker can distinguish provisioned internal usernames by response timing despite the generic 401 body, violating AC-01's no-account-enumeration requirement and increasing targeted credential-attack risk. Closure: make the unknown-user path perform equivalent framework password-verification work using a non-secret dummy Identity hash (or an equivalently reviewed constant-work design), retain identical public errors, and add a deterministic regression that proves both branches execute the password-verification boundary without asserting fragile wall-clock thresholds.
2. **VG-04 / Open / Person 1, P1-10 - complete the explicit API/security regression matrix before acceptance.** Source inspection and test-name inventory show no direct regression for login with an existing username plus wrong password; Session/refresh expiry values bound to configured lifetimes; mismatched forced-change confirmation; inactive authoritative User and missing/invalid `sid` through the real bearer pipeline; expired/unknown/malformed refresh through the public endpoint; controlled concurrent logout and logout recovery after an ambiguous durable commit; a runtime 409 `auth_concurrency_conflict` ProblemDetails response; or auth response/log capture covering token hash, signing key and device metadata. These are named requirements in AC-01/02/03/05/06/08/09 and the requested high-risk logout/unknown-commit review, not optional broadening. Closure: add the smallest focused unit/API/real-SQL cases for those branches, with non-zero discovery and assertions for status/code, no write/action/secret leakage, configured expiry, durable idempotent logout, and persisted state where applicable; rerun affected projects and refresh invalidated submission evidence.

### Prior finding dispositions

- `F-01` **Verified**: retry-enabled SQL regressions passed for pre-save/pre-commit rollback reconstruction and post-durable-commit verification across initial issuance, forced change, replay revocation and refresh rotation; durable state/audit counts match the returned outcome.
- `F-02` **Verified**: the controlled post-idempotency-miss ordering returns one Success, one IdempotentReplay and one audit.
- `F-03` **Verified**: forced-change idempotency uses the dedicated fingerprint key and the two-host JWT active-key rotation retry returns 204 twice with one audit.
- `F-04` **Verified**: configured Session metadata limits are shared by service and persistence; the strict-host case returns 400 `validation_error` with no Session write.
- `F-05` **Verified** for its original OpenAPI metadata finding: generated operations publish DTO/ProblemDetails schemas, declared statuses and Bearer only on protected logout. VG-04 separately records missing runtime matrix evidence.
- `F-06` **Verified**: refresh rotation returns the authoritative parent SessionId and the SQL regression passed.
- `VG-02` **Verified**: the unit fail-closed matrix plus SQL-backed bearer current-session, expired-session, stale-role and request-after-logout cases prove authoritative Session/User/role lookup and protected-action rejection. VG-04 separately retains the explicitly required inactive-User and invalid-claim HTTP cases.
- `VG-03` **Verified**: DTOs, AuthService/password policy, controller, JWT bearer wiring, per-request validator, DI/startup, OpenAPI and API/SQL deliverables are present in the 52-file artifact.

### Reviewer verification

Environment: Windows 11 `10.0.26100`, PowerShell, SDK `10.0.401` targeting net8.0, configured authorized SQL Server with isolated fixture databases; reviewed `2026-09-19` (+07:00).

| Reviewer command/check | Exit | Result |
|---|---:|---|
| Recomputed 52-file SHA-256 manifest; `git diff --cached --name-status` | 0 | Aggregate exactly `64c85ebc...dad56`; no staged file. |
| Authentication unit filter | 0 | 37 passed, 0 failed/skipped. |
| Entire API test project | 0 | 45 passed, 0 failed/skipped, including SQL-backed auth flow and generated OpenAPI. |
| SQL filter `TaskId=P1-10|TaskId=P2-10` | 0 | 64 passed, 0 failed/skipped: P1-10 25 and accepted P2-10 39. |
| Restore; non-incremental build; format verify; full solution test | 0 | Restore current; 9-project build with 0 warnings/errors; format clean; 331 passed, 0 failed/skipped (Unit 122, API 45, SQL Integration 164). |
| API, Services, Repositories and API-test vulnerability checks | 0 | No vulnerable direct/transitive package from configured sources. |
| EF `has-pending-model-changes` | 0 | No model drift from the accepted migration. |
| P1 docs verifier; P2 planning verifier | 0 | Passed; planning scenarios 9/9. |
| `git diff --check`; corrected production secret scan; auth logging-sink scan | 0 | No whitespace error, embedded private key/connection credential, or auth-layer logging/console sink. An initial broad secret regex matched the identifier `PasswordHash` and was discarded as a false-positive diagnostic before the corrected scan. |

- Evidence reuse: the implementer RED chronology and exact-manifest package/model/docs evidence were inspected; required SQL/security/build/format/full-suite gates were also rerun in this review, so no required local gate was waived. Hosted CI/deployment/FE remain outside this local acceptance and do not replace the open in-scope AC evidence.
- Verdict: **Changes requested**. AC-04, AC-07 and AC-10 pass; the implemented behavior for F-01 through F-06 and the prior VG-02/VG-03 deliverables is verified. AC-01 is blocked by open `F-07`; AC-01/02/03/05/06/08/09 cannot pass the agreed evidence gate while `VG-04` remains open. This is not an infrastructure blocker and does not authorize production/test fixes in the reviewer session.
- Bounded next action: Codex Implementer resumes P1-10 as `In Progress`, fixes `F-07`, adds only the focused `VG-04` regressions, reruns affected/full submission checks invalidated by those changes, self-reviews, and freezes a new exact artifact identity for independent review. No schema change, unrelated refactor, commit, merge, push, deployment or publication is authorized by this verdict.

## Codex Implementer Round 5 - F-07 and VG-04 fix round

- Resumed/time/status: Codex Implementer for Person 1 resumed the reviewed 52-file working-tree submission on `anh` at baseline HEAD `7513d576dac2b0d470059973aff70b1043828c9e` on 2026-09-19; status changed from `Changes requested` to `In Progress` before implementation edits.
- Stable scope: acceptance criteria AC-01 through AC-10, dependency decisions, public error contracts and schema remain unchanged. This round is limited to closing open `F-07` and `VG-04`; prior verified finding dispositions remain historical evidence and are not reopened without a regression.
- Exclusive implementation paths: `RoadGuardSystem.Services/Authentication/IdentityCredentialVerifier.cs`, focused files under `tests/RoadGuardSystem.UnitTests/Authentication/**` and `tests/RoadGuardSystem.ApiTests/Authentication/**`, and the owner-approved P1-10 repository/SQL-test exception only where controlled logout concurrency or ambiguous-commit proof requires it. Task-scoped metadata writes are limited to this worklog and the P1-10 plan row. P1-08 paths and plan entries remain untouched.
- Planned Lean TDD slices: (1) deterministic credential-hasher boundary regression for unknown username versus existing-user wrong password, then minimal constant-work implementation; (2) configured Session/refresh expiry and forced-change confirmation regressions; (3) real bearer inactive-User and missing/invalid `sid`, public refresh negative matrix, runtime 409, and response/log secret-capture regressions; (4) controlled concurrent logout and ambiguous durable-commit recovery on real SQL. Each slice records behavioral RED before production changes where a defect exists and GREEN after the smallest fix; evidence-only gaps are recorded honestly when the new regression is already green.
- Required submission gate: focused RED/GREEN filters, affected Unit/API/SQL projects, restore, non-incremental solution build, format verification, full solution tests, task SQL/security checks, `git diff --check`, artifact manifest, task-owner self-review and a filled independent reviewer Prompt C. No commit, merge, push, deployment or publication is authorized.

### Round-5 implementation and finding dispositions

- `F-07` addressed, awaiting independent verification: `IdentityCredentialVerifier` now performs `UserManager.CheckPasswordAsync` against a process-local, non-secret ASP.NET Core Identity dummy hash when username lookup returns no User. Unknown-user and existing-user/wrong-password branches retain the same public result and each execute exactly one real password-hasher verification boundary; no wall-clock threshold is asserted.
- `VG-04` addressed, awaiting independent verification: focused regressions now cover existing-user wrong password; configured access/Session/refresh expiry; mismatched password confirmation; inactive authoritative User and missing/invalid `sid` through the real bearer pipeline; expired/unknown/malformed refresh through the public endpoint; controlled concurrent logout and post-durable-commit acknowledgment failure; runtime 409 `auth_concurrency_conflict` ProblemDetails; and response/log capture excluding password, refresh plaintext/hash, signing key and device metadata.
- Production change is limited to `RoadGuardSystem.Services/Authentication/IdentityCredentialVerifier.cs`. Test/harness changes are limited to `AuthServiceTests.cs`, new `IdentityCredentialVerifierTests.cs`, `AuthenticationFlowTests.cs`, `AuthenticationWebApplicationFactory.cs`, new `InMemoryLogProvider.cs`, and `P110AuthenticationPersistenceTests.cs`. Metadata changes are this worklog and only the P1-10 plan row. No P1-08 file/entry, schema, migration, entity shape, package, public DTO or error code changed.

### Round-5 Lean TDD and verification evidence

Environment: Windows 11 `10.0.26100`, PowerShell, SDK `10.0.401` targeting net8.0, configured authorized SQL Server with isolated fixture databases, 2026-09-19 (+07:00).

| Slice / command | Exit | Result |
|---|---:|---|
| F-07 focused unit test before production fix | 1 expected | Behavioral RED: 0 passed, 1 failed, 0 skipped; existing-user/wrong-password called the hasher once while unknown username called it zero times. |
| Same F-07 focused unit test after dummy-hash implementation | 0 | GREEN: 1 passed, 0 failed/skipped. |
| Configured lifetime focused unit test | 0 | Evidence-gap regression was GREEN on first run: 1 passed, 0 failed/skipped; exact access/Session/refresh expiries use configured values. |
| `AuthenticationFlowTests` first matrix run | 1 diagnostic | 28 passed, 1 setup error, 0 skipped; test SQL incorrectly wrote string `SUSPENDED` to the mapped `tinyint` status. This was not behavioral RED. Fixture setup was corrected to persist `UserStatus.Suspended` through EF. |
| `AuthenticationFlowTests` after fixture correction | 0 | 29 passed, 0 failed/skipped, including all added VG-04 public/bearer/secret-capture cases and prior auth flows. |
| Focused controlled logout SQL tests | 0 | 2 passed, 0 failed/skipped: concurrent writers converge on durable Session/token revocation; post-commit acknowledgment failure returns with durable revocation. |
| Unit filter `TaskId=P1-10` | 0 | 39 passed, 0 failed/skipped. |
| Entire API test project | 0 | 56 passed, 0 failed/skipped. |
| SQL filter `TaskId=P1-10|TaskId=P2-10` | 0 | 66 passed, 0 failed/skipped: P1-10 27 and accepted P2-10 39. |
| `dotnet restore RoadGuardSystem.slnx` at `2026-09-19T13:24:30+07:00` | 0 | All projects up to date. |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` at `2026-09-19T13:24:37+07:00` | 0 | 9 projects; 0 warnings, 0 errors. |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` at `2026-09-19T13:24:54+07:00` | 0 | No formatting changes required. |
| `dotnet test RoadGuardSystem.slnx --no-build` at `2026-09-19T13:25:14+07:00` | 0 | **346 passed, 0 failed/skipped**: Unit 124, API 56, SQL Integration 166. |
| Dependency security verifier for Services, API, Repositories and API tests | 0 | No High/Critical vulnerable direct or transitive dependency from configured sources. |
| EF `has-pending-model-changes` with a non-secret design-time setting | 0 | No model drift from the accepted migration. |
| P1 documentation verifier; P2 planning verifier | 0 | Documentation contracts passed; planning scenarios 9/9 passed. |
| `git diff --check` | 0 | No whitespace errors. |
| Production embedded-secret scan; authentication logging-sink scan | 1 expected | No matches. Dedicated production keys remain external configuration; test keys/markers are synthetic. |

### Round-5 AC coverage and self-review

- AC-01 / `F-07`: the deterministic hasher-boundary unit test proves unknown and known-wrong branches both execute one ASP.NET Core Identity verification, while the SQL-backed public test proves an existing username plus wrong password receives generic `auth_invalid_credentials` and creates no Session.
- AC-02: unit and SQL-backed API regressions prove configured access/Session/refresh lifetimes, returned expiry values, and persisted Session/refresh expiry. Prior accepted atomic/hash-only evidence remains unchanged and was rerun by the full/API/SQL gates.
- AC-03: mismatched confirmation now returns 400 `validation_error` through API model validation before password/audit state changes. Existing wrong/reused/weak/current-password and positive flow regressions remain green.
- AC-05: inactive authoritative User and missing/invalid `sid` now traverse the actual signed bearer pipeline, return 401 with stable code and never reach the protected action. Existing expired/revoked/current/stale-role/store-failure matrices remain green.
- AC-06: expired, unknown and malformed refresh inputs now have direct endpoint regressions with stable 400/401 codes, no replacement on expiry, and no plaintext/hash leakage. Existing rotation/concurrent/replay SQL evidence remains green.
- AC-08: controlled two-context logout proves concurrency loser recovery, and retry-enabled SQL proves ambiguous durable commit recovery; persisted Session and active token family remain revoked. Sequential idempotency and request-after-logout remain green.
- AC-09: runtime 409 mapping includes `auth_concurrency_conflict`, `application/problem+json` and a correlation ID. Captured error bodies plus in-memory server logs exclude supplied password, refresh plaintext/hash, configured signing key and device metadata marker.
- Authorization/current state: no authorization policy changed. Bearer regressions prove current User/Session authority; project membership remains P1-12 and is not inferred from this task.
- State/immutability/versioning: no entity/schema/migration or immutable Session metadata behavior changed. Added SQL tests observe existing rowversion convergence and durable revocation only.
- Idempotency/concurrency/audit: logout race and unknown-commit paths are now directly covered; no new audit event is introduced and prior exactly-once replay/password-change audit evidence remains green.
- Secrets/missing tests: the dummy password material is random, discarded immediately after producing a non-secret hash, never persisted/returned/logged; the regression asserts the real hasher boundary rather than timing. Every explicit `VG-04` case has direct non-zero test execution. No in-scope self-review finding or blocker remains.
- External gaps: hosted CI, deployment secret provisioning, FE integration and publication were not run because no push/deployment authority was granted. These do not replace the completed local security/API/SQL gates.

### Round-5 exact artifact identity and handoff

- Status: `Ready for review`; implementation/test artifacts are frozen for a separate Codex Reviewer. Codex Implementer does not mark `Done`.
- Branch/baseline: `anh`, HEAD `7513d576dac2b0d470059973aff70b1043828c9e`; no staged files, commit, merge, push, deployment or publication.
- Exact implementation/test/docs identity: 54 sorted files, aggregate SHA-256 `1dd5cc458a6ae9bf1206689ed7108ff3f2b7537ba860b63c249ee602bd5fb9ab`, captured `2026-09-19T13:29:38+07:00`. This is the independently reviewed 52-path set plus `tests/RoadGuardSystem.UnitTests/Authentication/IdentityCredentialVerifierTests.cs` and `tests/RoadGuardSystem.ApiTests/Infrastructure/InMemoryLogProvider.cs`; it contains 15 tracked-modified and 39 relevant untracked paths. Recompute by sorting those exact paths, formatting each line as `<lowercase-file-sha256><two spaces><forward-slash-path>`, joining with LF/no final LF, and hashing the UTF-8 payload. Mutable plan/worklog records and unrelated P1-08 artifacts are excluded.
- Round-5 changed artifact hashes: `IdentityCredentialVerifier.cs` `474f95d73b3960d170182c583be23248470109d0ac66833a20877a6edefb09d9`; `AuthServiceTests.cs` `2e0f588938a02bd00d857b0a9820d38bc8754c0add85d599fd7832dfbfdf3e85`; `IdentityCredentialVerifierTests.cs` `8be6cf168ee9f30c5ce559baf6ef00c816ad7a40e9f28d8a28af776858de77a5`; `AuthenticationFlowTests.cs` `cd02a8c37484fffefc88cd8fdc7acc73faede8532c0485e5b79ccbf88aefa5a1`; `AuthenticationWebApplicationFactory.cs` `f7e8d6cef1f420640182d942896c939c8e0e6c4819a1e1fda839f80942721260`; `InMemoryLogProvider.cs` `00a6fce0c9e22cdcb8bcb01725d6fe00604ae40367b629636e5af172f705184`; `P110AuthenticationPersistenceTests.cs` `792c46bca0d076b67bb06efff79301569e45bde5771b7f1804d8afe547a6445a`.
- Finding dispositions submitted for verification: `F-07` fixed; `VG-04` evidence completed. `F-01` through `F-06`, `VG-02` and `VG-03` retain the independent Round-3 `Verified` disposition unless the reviewer finds regression evidence.

### Ready-to-run independent reviewer prompt - Round 5

```text
Bạn là Codex Reviewer độc lập cho RoadGuard P1-10, Person 1, branch anh.
Baseline: 7513d576dac2b0d470059973aff70b1043828c9e. Submission: exact 54-file working-tree implementation/test/docs manifest SHA-256 1dd5cc458a6ae9bf1206689ed7108ff3f2b7537ba860b63c249ee602bd5fb9ab; no staged files; include the 15 tracked-modified and 39 relevant untracked paths described in the Round-5 worklog. Worklog: docs/worklogs/P1-10-completion.md.

Phiên này phải là task/session độc lập, không phải implementer hoặc reviewer Round 3 đã author artifacts. Không sửa production code/tests. Bạn được ghi review/status đúng P1-10 theo AGENTS.md.

Đọc AGENTS.md, dùng roadguard-review và roadguard-review-p1. Kiểm tra status/HEAD, assignment AC-01..AC-10, P2-10 dependency/no-schema exception, exact 54-path aggregate, Round-5 self-review và các review/fix round trước. Giữ nguyên các disposition đã Verified cho F-01..F-06, VG-02 và VG-03 trừ khi có bằng chứng regression.

Xác minh `F-07`: unknown username và existing-user/wrong-password đều chạy đúng một ASP.NET Core Identity password-verification boundary bằng dummy hash không chứa credential thật, trả cùng generic public error và không dùng wall-clock assertion. Xác minh `VG-04`: wrong-password login; configured access/Session/refresh expiry; mismatched confirmation; inactive User và missing/invalid sid qua real bearer pipeline; expired/unknown/malformed refresh endpoint; controlled concurrent và ambiguous-commit logout; runtime 409 ProblemDetails; response/log capture không lộ password, refresh plaintext/hash, signing key hoặc device metadata.

Rerun regression mới/high-risk và required SQL/security checks khi evidence/content/environment yêu cầu; phân biệt rerun với inspected evidence. Đối chiếu từng AC với login/refresh/logout/forced-change, authoritative bearer state, ProblemDetails/correlation, hash-only persistence, retry/idempotency/concurrency/audit và external secret configuration. Zero-discovered/skipped required tests không phải pass.

Finding mới phải có ID ổn định, severity, owner, file/dòng, trigger/impact/AC/closure. Nếu mọi AC, dependency, required gate, self-review và findings đạt, append acceptance round và update P1-10 `Ready for review -> Done`; nếu không, ghi `Changes requested` hoặc `Blocked` đúng bằng chứng. Không commit/merge/push/deploy.

Trả findings/gaps, checks thực chạy hoặc evidence đã đối chiếu, verdict/status thực ghi, worklog link và yêu cầu sửa ngắn theo finding IDs nếu cần.
```

## Codex acceptance review Round 4 - exact 54-file submission

- Reviewer/time: independent Codex Reviewer task/session for Person 1 acceptance, `2026-09-19T13:42:04+07:00`. This reviewer did not author the submitted production/test artifacts and was not the Round-3 reviewer.
- Reviewed artifact: branch `anh`, HEAD/baseline `7513d576dac2b0d470059973aff70b1043828c9e`, no staged files. The reviewer reconstructed the 15 tracked-modified and 39 relevant untracked paths, including the Round-5 additions, and reproduced the exact 54-file SHA-256 manifest `1dd5cc458a6ae9bf1206689ed7108ff3f2b7537ba860b63c249ee602bd5fb9ab` both before verification and immediately before this metadata write. Mutable plan/worklog records and unrelated P1-08 artifacts remain excluded.
- Dependencies/ownership: P1-01, P1-02 and accepted P2-10 artifacts are present at the reviewed HEAD. The repository-owner-approved P1-10 consumer extension covers the repository and SQL-test paths. No BusinessObjects entity/property/enum shape, EF mapping, migration or model snapshot changed; EF model comparison is clean. P2-01 and P1-08 own non-overlapping paths. The implementer froze the artifacts at `Ready for review` and yielded only P1-10 review/status metadata.

### Findings and dispositions

- No new code finding or verification gap remains.
- `F-07` **Verified**: source inspection and the fresh deterministic unit regression prove that unknown username and existing-user/wrong-password each call `UserManager.CheckPasswordAsync` once through the configured ASP.NET Core Identity password-hasher boundary. The unknown-user branch uses a process-local hash of a randomly generated, immediately discarded dummy password; it contains no real credential. Both branches return the same invalid-credential result, public API cases return generic `auth_invalid_credentials`, and no wall-clock assertion or delay is used.
- `VG-04` **Verified**: fresh API/SQL runs cover existing-user wrong password; configured access/Session/refresh expiry; mismatched forced-change confirmation; inactive authoritative User and missing/invalid `sid` through the signed bearer pipeline; expired/unknown/malformed refresh; controlled concurrent logout; ambiguous durable-commit logout recovery; runtime 409 `auth_concurrency_conflict` ProblemDetails with correlation ID; and response/log capture excluding supplied password, refresh plaintext/hash, signing key and device metadata.
- `F-01` through `F-06`, `VG-02` and `VG-03` remain **Verified**. Their production inputs are unchanged except the bounded F-07 verifier fix, and the fresh affected/full regressions found no contrary evidence.

### AC disposition

| AC | Round-4 acceptance result |
|---|---|
| AC-01 | Accepted: standard Identity credential verification, active-account gates, all four roles, generic unknown/wrong-password response, F-07 equal-boundary behavior and no credential leakage are verified. |
| AC-02 | Accepted: atomic initial Session/refresh issuance, hash-only persistence, configured expiries, metadata validation and retry/rollback behavior are verified on real SQL/API paths. |
| AC-03 | Accepted: forced-change gating, mismatch/wrong/reused/weak negatives, fresh-login requirement, credential revocation, idempotency and sanitized exactly-once audit are verified. |
| AC-04 | Accepted: required JWT identity/temporal claims, configured HMAC key ring and fail-fast external configuration remain verified. |
| AC-05 | Accepted: each protected request uses authoritative User/Session/role state; inactive, expired/revoked, stale-role, store-failure and missing/invalid claim paths fail closed before the protected result. |
| AC-06 | Accepted: refresh inputs are hash-looked up; malformed/unknown/expired/replayed and controlled concurrent paths fail safely; rotation has one winner and persists no plaintext. |
| AC-07 | Accepted: replay revokes the family and appends one sanitized `auth_token_replay_detected` audit effect across retry/concurrency/unknown-commit paths. |
| AC-08 | Accepted: logout revokes Session/token state, duplicate/concurrent delivery is durable and idempotent, ambiguous commit is recovered, and old credentials are rejected. |
| AC-09 | Accepted: DTO/thin-controller/OpenAPI boundaries, runtime 400/401/403/409 ProblemDetails, correlation IDs and response/log secret suppression are verified. |
| AC-10 | Accepted: deferred Google packages are removed without SSO/package upgrade; DI, startup, Swagger, build, dependency-security and full regression gates pass. |

### Reviewer verification

Environment: Windows 11 `10.0.26100`, PowerShell, SDK `10.0.401` targeting net8.0, configured authorized SQL Server with fixture-created isolated databases; reviewed 2026-09-19 (+07:00).

| Reviewer command/check | Exit | Result |
|---|---:|---|
| Recompute exact submission manifest; `git diff --cached --name-only` | 0 | 54 files; digest exactly `1dd5cc45...fb9ab`; 0 staged files. |
| `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-restore --filter "TaskId=P1-10"` | 0 | 39 passed, 0 failed/skipped, including deterministic F-07 and configured-lifetime cases. |
| `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-restore` | 0 | 56 passed, 0 failed/skipped, including all VG-04 public/bearer/ProblemDetails/log-capture cases. |
| `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-restore --filter "TaskId=P1-10\|TaskId=P2-10"` | 0 | 66 passed, 0 failed/skipped: P1-10 27 and P2-10 39; controlled logout race and unknown-commit recovery passed. |
| `dotnet restore RoadGuardSystem.slnx` | 0 | All projects up to date. |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | 9 projects; 0 warnings, 0 errors. |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | No formatting change required. |
| `dotnet test RoadGuardSystem.slnx --no-build --no-restore` | 0 | 346 passed, 0 failed/skipped: Unit 124, API 56, SQL Integration 166. |
| Dependency-security verifier: API, Services, Repositories and API tests | 0 | No High/Critical vulnerable direct or transitive dependency from configured sources. |
| EF `has-pending-model-changes` with a non-secret design-time setting | 0 | No model drift from the accepted migration. |
| P1 documentation verifier; P2 planning verifier | 0 | Documentation contracts passed; planning scenarios 9/9 passed. |
| `git diff --check`; production secret/config/log-sink and wall-clock-assertion scans | 0 / expected `rg` 1 | No whitespace error, embedded private key/credential/connection secret, tracked JWT/fingerprint key, auth logging sink or timing assertion. Identifier-only matches from the initial broad password scan were inspected and rejected as false positives. |

- Inspected evidence: the implementer Round-5 RED/GREEN chronology, self-review, exact artifact hashes, prior review rounds and stable finding dispositions were checked against the matching content and environment. Fresh reviewer runs cover all affected projects, SQL/security gates and the complete solution; no required evidence was waived.
- Verdict / recorded status: **Done**. AC-01 through AC-10, dependency integration, no-schema boundary, self-review, all prior mandatory findings and the Round-5 F-07/VG-04 closure are verified for the exact submission. P1-10 plan status changed `Ready for review -> Done` by Codex Reviewer.
- Final boundary: Done records local task acceptance only. No production/test fix, commit, merge, push, deployment, publication or next-task assignment was performed.
