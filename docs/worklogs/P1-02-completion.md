# Antigravity completion log — P1-02

## Identity and scope

- **Task ID/title:** P1-02 / Architectural Decision Records & API Error Taxonomy
- **Owner / reviewer:** Person 1 (Antigravity) / Person 2
- **Date / branch or commit:** 2026-09-17 / `anh`
- **Trace:** Architecture / Task P1-02 (Foundation post P1-00 and P1-01). Downstream context: US-01, CN01-CN03, CN10, QT01, TE-01, TE-02.
- **Status:** Ready for re-review

### In-scope behavior
- Authored and updated [001-backend-boundary.md](../adr/001-backend-boundary.md):
  - Defined system scope: ASP.NET Core backend is the sole in-scope service; Android Mobile, Web Dashboard, and Python AI are separate external systems.
  - Codified exact project reference graph: `API -> Services -> Repositories; Repositories -> BusinessObjects và DTOs; DTOs -> BusinessObjects`.
  - Documented repository ownership policy from `AGENTS.md`: `Repositories` owns repository interfaces and implementations used by `Services`. External-boundary abstractions (AI, storage, clock, notifications) are introduced at real external boundaries.
  - Documented active DbContext as `RoadGuardDbContext` and EF Core Identity persistence under task P2-10 without asserting an unconfirmed class name.
  - Documented Phase 1 deterministic AI adapter contract to be formally defined in task P1-31 under US-07 and KS10–KS13.
  - Codified drone boundary (no direct drone control/telemetry in backend) and warranty boundary (no automatic legal warranty liability inferences).
  - Codified runtime baseline: target framework `net8.0`, toolchain pinned to SDK `10.0.401` in `global.json`, task P2-01 CI replication requirement, no default SDK downgrade, and time-bound LTS evaluation before .NET 8 EOL or release.
  - Updated verification test link to [DependencyGraphTests.cs](../../tests/RoadGuardSystem.UnitTests/Architecture/DependencyGraphTests.cs).
  - Documented rejected alternatives, consequences, and unresolved decisions.
- Authored and updated [002-authentication.md](../adr/002-authentication.md):
  - Defined authentication architecture: ASP.NET Core Identity with standard PBKDF2 password hashing (`PasswordHasher<TUser>`) and JWT bearer + rotating opaque refresh-token model.
  - Aligned exact use-case mappings: CN01 (Login / Logout), CN02 (Profile), CN03 (Assigned Project / Work Scope), CN10 (Password Reset), QT01 (Account Suspension), QT02 (Role and Project Access Management).
  - Defined machine-readable role codes from Data Dictionary Section 3.1: `SUPERVISOR`, `PM`, `DRONE_OPERATOR`, `REPAIR_CREW`. Display labels ("Supervisor", "PM", "Drone Operator", "Repair Crew") distinguished from serialized codes. Clarified that a `UserRole` enum is future work owned by P2-10.
  - Codified credentials policy: internally provisioned credentials (username + password; email is a PROP field in Data Dictionary Section 3.1 and is not an approved login identifier).
  - Codified configurable short-lived access tokens via ASP.NET Core Options pattern (no hard-coded duration), carrying standard session claim `sid`.
  - Defined authoritative per-request session and account validation ensuring instant revocation upon logout (CN01), password reset (CN10), account suspension (QT01), or replay attack. Positive cache may only be used with provable session-version/security-stamp mechanisms; baseline mandate for P1-10 requires querying authoritative session/user stores.
  - Clarified database session persistence: `Session.status` is not a persisted column in Data Dictionary Section 3.1; status is derived logically from temporal boundaries (`revoked_at`, `expires_at`). Schema additions belong exclusively to P2-10.
  - Codified refresh token rotation and token-family replay detection: high-entropy tokens, persisting only cryptographic hashes (`token_hash`), with replay attacks triggering immediate session revocation and security logging.
  - Adopted implementation-neutral terminology: "JWT signing credentials", moving algorithm selection (symmetric vs. asymmetric) to Unresolved Decisions.
  - Codified mandatory server-side project membership validation for all non-Supervisor queries and commands; client claims are untrusted hints.
  - Recorded the Product Owner authorization decision: `User.role_code` is the authoritative global role; JWT role is a snapshot; non-Supervisor `ProjectMember.role_code` is authoritative in-project and must match `User.role_code` for the MVP.
  - Added `UserRoleChanged` semantics: global role changes are audited and atomically revoke every active Session and RefreshToken; membership changes take effect on the next request through authoritative server-side checks.
  - Corrected task allocation table:
    - `P1-00`: Foundation `ApplicationUser` and `ApplicationRole` in `BusinessObjects`.
    - `P2-10`: User, Role, Session, RefreshToken, PasswordResetLog, AccountStatusChangeLog persistence entities, EF Core configurations, migrations, and role seeding (`SUPERVISOR`, `PM`, `DRONE_OPERATOR`, `REPAIR_CREW`).
    - `P1-10`: Authentication service orchestration (`IAuthService`), login/logout/refresh endpoints (`AuthController`), password hashing, and cleaning unused Google auth packages.
    - `P1-11`: Profile updates and Admin password-reset / forced session revocation flows.
    - `P1-12`: Current-user and project authorization service, project-scope policies and guards.
    - `P2-11`: Authoritative active/effective project-membership read model and security matrix.
    - `P1-64`: Admin global-role changes, `UserRoleChanged` audit, and atomic session/refresh-token revocation.
  - Defined secret handling, HTTPS/TLS mandate, and log sanitization (no passwords, tokens, or secrets in logs).
  - Documented Google OAuth/SSO as Deferred / Out of Scope for Phase 1; flagged existing Google package references as technical debt for P1-10 review.
  - Documented rejected alternatives, consequences, and unresolved decisions.
  - Replaced the conflicting `Session.created_at` wording with the canonical `issued_at` field and documented the Product Owner-approved nullable `device_metadata_json` extension.
  - Defined SQL Server `nvarchar(max)` + `ISJSON`, application-level schema validation, write-once handling, secret rejection, log/API redaction, and the rule that device metadata is never an authorization signal.
  - Assigned entity mapping, migration, downgrade/recovery notes, and SQL Server integration proof to P2-10.
- Authored and updated [api-errors.md](../api-errors.md):
  - Codified uniform RFC 7807 / RFC 9110 Problem Details standard (`application/problem+json`) for standard API 4xx/5xx error responses.
  - Distinguished current platform baseline (P1-01 enforces 400, 404, 405, 415, 500) from downstream target policy (401, 403, 409, 422 to be implemented in subsequent tasks).
  - Established stable error-code naming rules: strictly ASCII lowercase `snake_case`, preserving platform codes (`validation_error`, etc.) and governing domain codes via `<domain>_<reason>` and `<domain>_<action>_<reason>`.
  - Defined evolution policy: client applications must tolerate unknown error codes gracefully, and endpoint error set changes require explicit compatibility reviews.
  - Registered the 6 published normative platform error codes implemented in `RoadGuardSystem.API/Constants/ApiErrorCodes.cs` and `RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs`.
  - Mandated global suppression of internal exception types, stack traces, and database/server details across all environments.
  - Removed arbitrary deprecation timeframe, marking sunset duration as an unresolved policy requiring Product Owner decision prior to release.
- Added and strengthened reproducible documentation verifier script [Verify-P102Docs.ps1](../../tests/Documentation/Verify-P102Docs.ps1):
  - Validates required files and headings.
  - Validates portable relative links and strictly prohibits absolute `file://` links.
  - Validates exact use-case mappings (`CN01 (Login / Logout)`, `CN02 (Profile)`, `CN03 (Assigned Project / Work Scope)`, `CN10 (Password Reset)`, `QT01 (Account Suspension)`).
  - Explicitly rejects obsolete mappings (`CN02 (Logout)`, `CN03 (Change Password)`, `CN10 (Account Suspension)`).
  - Validates stable machine-readable role codes (`SUPERVISOR`, `PM`, `DRONE_OPERATOR`, `REPAIR_CREW`).
  - Validates task ownership and strictly rejects assigning session entities to `P1-00 / P1-11`.
  - Validates `issued_at` and `device_metadata_json` across Data Dictionary, ERD, Domain Model, and ADR 002; rejects the obsolete `Session.created_at` contract and missing P2-10 ownership.
  - Validates `User.role_code`, `ProjectMember.role_code`, `UserRoleChanged`, immediate authorization changes, and matching P1/P2 downstream ownership across all five product specifications and both plans.
  - Proves negative behavior via `-SelfTestNegative` mode and external negative fixture testing.
- Preserved the existing P1-01 middleware/API-test candidate behavior; this correction only aligns its completion status for re-review.

### Explicitly out of scope
- Implementing production authentication controllers, login/logout endpoints, JWT generation, or session middleware (assigned to P1-10).
- Implementing user management, registration, or profile endpoints (assigned to P1-11).
- Implementing project membership repository or authorization policy handlers (assigned to P1-12).
- Implementing EF Core Identity persistence, `IdentityDbContext`, `UserStore`, `RoleStore`, table mappings, and migrations (assigned to Person 2 in P2-10).
- Implementing the `device_metadata_json` column, `ISJSON` database constraint, application validator, or migration (assigned to Person 2 in P2-10).
- Modifying project dependencies or removing packages (e.g., Google authentication packages).
- Modifying or committing the existing P1-01 middleware/API-test candidate currently in the working tree.

---

## Preconditions and decisions

- **Actor and project-scope rule:** Not applicable for runtime execution (documentation task). Architecturally, ADR 002 codifies the mandatory rule that all non-Supervisor requests must be validated against server-side project memberships.
- **State before / allowed state after:**
  - Before: `docs/adr` directory did not exist; no formalized ADRs for backend boundaries or authentication; no central API error taxonomy document; working tree contained 4 modified files from P1-01.
  - After: `docs/adr/001-backend-boundary.md`, `docs/adr/002-authentication.md`, `docs/api-errors.md`, `tests/Documentation/Verify-P102Docs.ps1`, and `docs/worklogs/P1-02-completion.md` created, verified, and updated per review findings. P1-01 working tree changes remain intact and uncommitted.
- **Data/version/immutability rules:**
  - Codified in ADR 001 and ADR 002: Audit logs, submitted inspection measurements, original evidence files, and repair evidence are strictly append-only; update/delete-in-place is prohibited.
  - Error codes are immutable published contracts; deprecation must be documented with backward compatibility.
  - `Session.device_metadata_json` is nullable and write-once at session issuance; it cannot be updated in place or used as an authorization signal.
- **Audit event and stable error codes:**
  - Normative platform error codes verified: `validation_error`, `unsupported_api_version`, `not_found`, `internal_error`, `method_not_allowed`, `unsupported_media_type`.
  - Audit logging invariants established for authentication events: password resets (`PasswordResetLog`), account status changes (`AccountStatusChangeLog`), and token replay detection events (`auth_token_replay_detected`), with strict prohibition against logging plaintext credentials.
- **Idempotency/concurrency behavior:**
  - ADR 002 specifies that token refresh operations must be concurrency-safe within database transactions to ensure concurrent duplicate requests cannot both succeed.
- **Assumptions, ADRs, or specification conflicts:**
  - Clarification Gate conducted with Product Owner prior to writing deliverables. All decisions formally approved and aligned with review findings:
    1. Session validation: Short-lived access token configured via options, carrying `sid` claim; per-request authoritative session check enables instant revocation; positive cache restricted to provable invalidation/session-version schemes; no `Session.status` column added to schema (P2-10 scope).
    2. Google authentication: Marked Deferred / Out of Scope for Phase 1; existing packages recorded as technical debt for P1-10 cleanup; no package references modified in P1-02.
    3. Refresh token rotation: High-entropy tokens; only cryptographic hash stored in DB; rotation on every refresh; replay triggers full session revocation; concurrency-safe transactions.
    4. Runtime baseline: `net8.0` target with SDK `10.0.401` accepted as baseline from P1-00; P2-01 mandates CI replication; no default downgrade; time-bound LTS evaluation before .NET 8 EOL or release.
    5. Error taxonomy: ASCII lowercase `snake_case`; 6 platform codes retained; domain codes use `<domain>_<reason>` and `<domain>_<action>_<reason>`; client resilience required.
    6. Role codes & credentials: Data Dictionary Section 3.1 establishes stable role codes `SUPERVISOR`, `PM`, `DRONE_OPERATOR`, `REPAIR_CREW`; internally provisioned credentials required (username + password; email is a PROP field).
    7. Session metadata schema (Product Owner decision, 2026-09-17): retain `Session.issued_at`; add nullable `device_metadata_json` as SQL Server `nvarchar(max)` with `ISJSON` and application-level schema validation; prohibit secret/token content, API/log disclosure, authorization use, and updates after issuance; assign persistence and migration to P2-10.
    8. Authorization authority (Product Owner decision, 2026-09-17): `User.role_code` is the authoritative global role; JWT role is a snapshot checked against the server on every request; non-Supervisor `ProjectMember.role_code` is authoritative in project and must match the User role for MVP; `UserRoleChanged` revokes active credentials atomically; membership changes apply immediately.

---

## Files changed

| Change | File | Purpose |
|---|---|---|
| Added | `docs/adr/001-backend-boundary.md` | ADR defining system boundaries, Clean Architecture layer responsibilities (`API -> Services -> Repositories`, `Repositories -> BusinessObjects và DTOs`, `DTOs -> BusinessObjects`), `RoadGuardDbContext`, AI adapter contract under P1-31, drone/warranty boundaries, and SDK 10.0.401 / net8.0 runtime baseline. |
| Added/Modified | `docs/adr/002-authentication.md` | ADR defining ASP.NET Core Identity + short-lived JWT (`sid` claim) + refresh token hash rotation and replay detection, instant session revocation, server-side project membership, stable role codes, and the approved Session metadata schema/security contract. |
| Added | `docs/api-errors.md` | Specification defining RFC 7807/9110 ProblemDetails schema for API 4xx/5xx responses, distinguishing P1-01 baseline (400, 404, 405, 415, 500) from target policies, lowercase `snake_case` error taxonomy, client tolerance, and deprecation policy. |
| Added/Modified | `tests/Documentation/Verify-P102Docs.ps1` | Reusable documentation test script validating required files, links, mappings, roles, task ownership, dependency graph, and Session schema consistency. Includes `-SelfTestNegative` mutation coverage. |
| Added | `docs/worklogs/P1-02-completion.md` | Completion worklog for Task P1-02 recording scope, decisions, verification commands, review findings resolution, and review handoff. |
| Modified | `docs/diagram/RoadGuard_Data_Dictionary_v1.md` | Adds the approved Session metadata contract plus authoritative User/project role and immediate revocation rules. |
| Modified | `docs/diagram/RoadGuard_ERD_v1.md` | Adds `device_metadata_json` to `SESSION` and records cross-aggregate authorization invariants. |
| Modified | `docs/diagram/RoadGuard_Domain_Model_v1.md` | Adds Session metadata invariants, `UserRoleChanged`, role-claim comparison, and active/effective membership rules. |
| Modified | `docs/diagram/Dac_ta_UseCase_v2.md` | Aligns CN01/CN03/QT02 with server-side role authority and immediate role/membership changes. |
| Modified | `docs/diagram/User_Stories_Acceptance_Criteria_v2.md` | Adds acceptance criteria for stale-role revocation and immediate project-scope changes. |
| Modified | `planning/RoadGuard_Plan_Person_1.md` | Aligns P1-02/P1-10/P1-12/P1-64 implementation and tests with the approved authorization contract. |
| Modified | `planning/RoadGuard_Plan_Person_2.md` | Assigns P2-10/P2-11 persistence and security-matrix proof for role/session/membership authority. |

---

## Negative-first evidence

List each negative/edge case before positive cases. If a standard case is irrelevant, state why.

| Test | Layer | Expected failure/code | Result | Rationale / Note |
|---|---|---|---|---|
| Injected obsolete use-case mapping / missing mapping | Automation verifier | Exit code 1; flags missing `CN01 (Login / Logout)` and flags rejected `CN02 (Logout)` | PASS (exit code 1) | Proved via `Verify-P102Docs.ps1 -SelfTestNegative` and external temporary fixture test. |
| Missing/mismatched Session metadata contract | Documentation | Exit code 1; flags `Session.created_at`, missing `device_metadata_json`, `ISJSON`, schema validation, or P2-10 ownership | RED reproduced: 13 contract errors before documentation changes -> PASS after alignment | Verifies the Product Owner decision across specifications, ADR, domain model, and both person plans. |
| Missing authorization authority contract | Documentation | Exit code 1; flags missing `User.role_code`, `ProjectMember.role_code`, `UserRoleChanged`, immediate-effect wording, or downstream plan ownership | RED reproduced: 17 errors before specification updates and 4 plan errors before plan updates -> PASS after alignment | Prevents stale Supervisor claims and membership-role ambiguity from reappearing. |
| Null/empty/malformed documentation | Documentation | RED: Missing files or sections fail contract check | RED reproduced (exit code 1) -> PASS (exit code 0) | Verified via `Verify-P102Docs.ps1` checking required headings, tokens, schemas, and prohibiting `file://` links. |
| Boundary/oversize | Unit/API | N/A | N/A (Documentation task) | No runtime data streaming or memory allocations in architecture documentation. |
| Unauthorized/wrong project | Service/API | N/A | N/A (Documentation task) | Runtime authorization policies are designed in ADR 002 and implemented in P1-10 / P1-12. |
| Invalid transition/prerequisite | Domain/service | N/A | N/A (Documentation task) | Domain state machines are documented in ADR 001/002 and implemented in domain tasks. |
| Duplicate retry/idempotency | Integration/worker | N/A | N/A (Documentation task) | Concurrency and idempotency policies for token refresh are specified in ADR 002. |
| Stale concurrency | Integration/API | N/A | N/A (Documentation task) | Concurrency behavior specified in ADR 002 and api-errors.md. |
| DB/storage/queue timeout | Component | N/A | N/A (Documentation task) | Fail-closed policy for session caching specified in ADR 002. |
| Integrity/checksum/immutable history | Documentation / Architecture | Missing rejected alternatives or unresolved decisions fails verification | RED reproduced -> PASS | Verified that both ADRs contain mandatory Rejected Alternatives and Unresolved Decisions sections. |

---

## Positive evidence

| Test | Layer | Expected state/output | Result |
|---|---|---|---|
| Documentation contract verification | Automation script | All required documents exist, contain mandatory sections, headings, tokens, exact use-case mappings, stable role codes, and portable relative links | PASS (exit code 0 via `Verify-P102Docs.ps1`) |
| Session schema compatibility verification | Automation script | `issued_at` retained; nullable `device_metadata_json`, `ISJSON`, application validation, security restrictions, and P2-10 ownership agree across documents | PASS |
| Authorization authority verification | Automation script | Global role, JWT snapshot, membership role, revocation, immediate effect, and task ownership agree across specifications/plans | PASS |
| Relative file link verification | Documentation | All markdown links between documents resolve to valid existing files; 0 broken links | PASS |
| Non-incremental solution build | Toolchain | Build succeeds with 0 warnings, 0 errors across 8 projects | PASS (exit code 0) |
| In-scope test suite execution | API / Unit | All in-scope automated tests pass: 35 UnitTests + 26 ApiTests = 61 passed, 0 failed | PASS (61 passed, 0 failed, exit code 0) |
| Full solution test suite | Unit/API/SQL Server integration | 35 Unit + 26 API + 43 Integration = 104 tests pass | PASS (104 passed, 0 failed, 0 skipped) |
| Code formatting verification | Toolchain | `dotnet format --verify-no-changes --no-restore` clean | PASS (exit code 0) |

---

## Commands run

| Command | Exit code | Result/coverage | Timestamp |
|---|---:|---|---|
| `powershell -ExecutionPolicy Bypass -File "tests/Documentation/Verify-P102Docs.ps1" -SelfTestNegative` | 1 | FAILURE (expected): Caught missing 'CN01 (Login / Logout)' and caught rejected 'CN02 (Logout)' | 2026-09-17T13:29:55+07:00 |
| `powershell -ExecutionPolicy Bypass -File "tests/Documentation/Verify-P102Docs.ps1"` | 0 | SUCCESS: All P1-02 documentation contracts, exact use-case mappings, stable role codes, and dependency checks passed | 2026-09-17T13:30:00+07:00 |
| `git diff --check` | 0 | Clean diff, no trailing whitespace or merge conflict markers | 2026-09-17T13:31:00+07:00 |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | 0 warnings, 0 errors across all 8 projects | 2026-09-17T13:31:15+07:00 |
| `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-build` | 0 | 33 passed, 0 failed, 0 skipped | 2026-09-17T13:31:30+07:00 |
| `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build` | 0 | 26 passed, 0 failed, 0 skipped | 2026-09-17T13:31:45+07:00 |
| `dotnet test RoadGuardSystem.slnx --no-build` | 1 | 81 passed, 21 failed (UnitTests: 33 passed; ApiTests: 26 passed; IntegrationTests: 22 passed, 21 failed due to Docker/SQL Server service inactive on workstation) | 2026-09-17T13:32:00+07:00 |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | Formatted code conforms perfectly, 0 changes required | 2026-09-17T13:32:15+07:00 |
| Reviewer: `powershell -ExecutionPolicy Bypass -File "tests/Documentation/Verify-P102Docs.ps1" -SelfTestNegative` | 1 | Expected RED: injected obsolete mapping detected | 2026-09-17T13:42:15+07:00 |
| Reviewer: `powershell -ExecutionPolicy Bypass -File "tests/Documentation/Verify-P102Docs.ps1"` | 0 | GREEN: documentation contract, mappings, roles, ownership, and links pass | 2026-09-17T13:42:15+07:00 |
| Reviewer: `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | 0 warnings, 0 errors across 8 projects | 2026-09-17T13:42:15+07:00 |
| Reviewer: affected unit/API tests and formatting verification | 0 | Unit 33/33, API 26/26, format clean | 2026-09-17T13:42:15+07:00 |
| `powershell -ExecutionPolicy Bypass -File "tests/Documentation/Verify-P102Docs.ps1"` before documentation alignment | 1 | Expected RED: 13 Session contract errors across ADR, specifications, domain model, and plans | 2026-09-17T14:27+07:00 |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | 0 warnings, 0 errors across 8 projects | 2026-09-17T14:29:41+07:00 |
| `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --no-build` | 0 | 33 passed, 0 failed, 0 skipped | 2026-09-17T14:30:03+07:00 |
| `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build` | 0 | 26 passed, 0 failed, 0 skipped | 2026-09-17T14:30:03+07:00 |
| `powershell -ExecutionPolicy Bypass -File "tests/Documentation/Verify-P102Docs.ps1" -SelfTestNegative` | 1 | Expected RED: catches obsolete use-case mapping and missing/replaced Session metadata contract | 2026-09-17T14:30:03+07:00 |
| `powershell -ExecutionPolicy Bypass -File "tests/Documentation/Verify-P102Docs.ps1"` | 0 | GREEN: all documentation, Session schema, ownership, link, mapping, and role checks pass | 2026-09-17T14:30:03+07:00 |
| `dotnet test RoadGuardSystem.slnx --no-build` | 0 | 102 passed: Unit 33, API 26, Integration 43; 0 failed, 0 skipped | 2026-09-17T14:30:21+07:00 |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | Formatting verification clean | 2026-09-17T14:31+07:00 |
| `git diff --check` | 0 | No whitespace errors; Git emitted only an LF-to-CRLF working-copy warning for the PowerShell verifier | 2026-09-17T14:31+07:00 |
| `tests\Documentation\Verify-P102Docs.ps1` after adding authorization checks, before document updates | 1 | Expected RED: 17 authorization contract errors | 2026-09-17T15:00+07:00 |
| `tests\Documentation\Verify-P102Docs.ps1` after document updates | 0 | GREEN: authorization and Session contracts aligned | 2026-09-17T15:06+07:00 |
| `tests\Documentation\Verify-P102Docs.ps1 -SelfTestNegative` | 1 | Expected RED: 5 injected errors including missing `UserRoleChanged` | 2026-09-17T15:06+07:00 |
| `tests\Documentation\Verify-P102Docs.ps1` after adding plan checks, before plan updates | 1 | Expected RED: 4 missing plan ownership tokens | 2026-09-17T15:08+07:00 |
| `tests\Documentation\Verify-P102Docs.ps1` after plan updates | 0 | GREEN: all specification and plan checks pass | 2026-09-17T15:10+07:00 |
| `dotnet restore RoadGuardSystem.slnx` | 0 | Restored all 8 projects after removing the legacy runtime dependency | 2026-09-17T15:11+07:00 |
| `dotnet list RoadGuardSystem.slnx package --vulnerable --include-transitive --no-restore` | 0 | P1 runtime projects clean; P2-00 IntegrationTests retain one transitive High advisory | 2026-09-17T15:12+07:00 |
| `dotnet build RoadGuardSystem.slnx --no-restore --no-incremental` | 0 | 8 projects; 0 warnings, 0 errors | 2026-09-17T15:14+07:00 |
| `dotnet test RoadGuardSystem.slnx --no-build` | 0 | 104 passed: Unit 35, API 26, Integration 43; 0 failed, 0 skipped | 2026-09-17T15:16+07:00 |
| `dotnet format RoadGuardSystem.slnx --verify-no-changes --no-restore` | 0 | Formatting verification clean | 2026-09-17T15:16+07:00 |
| `tests\Documentation\Verify-P102Docs.ps1` | 0 | Final GREEN: all documentation and plan contracts pass | 2026-09-17T15:16+07:00 |
| `tests\Documentation\Verify-P102Docs.ps1 -SelfTestNegative` | 1 | Expected RED: 5 injected failures including missing `UserRoleChanged` | 2026-09-17T15:16+07:00 |
| `tests\Documentation\Verify-P102Docs.ps1` after adding QT02/ownership checks | 1 | Expected RED: missing QT02 mapping plus P1-64/P2-11 ownership | 2026-09-17T15:18+07:00 |
| `tests\Documentation\Verify-P102Docs.ps1` after ADR ownership update | 0 | GREEN: QT02 mapping and downstream task ownership verified | 2026-09-17T15:19+07:00 |
| `tests\Documentation\Verify-P102Docs.ps1 -SelfTestNegative` after ADR ownership update | 1 | Expected RED: 5 injected contract failures | 2026-09-17T15:19+07:00 |
| Final `tests\Documentation\Verify-P102Docs.ps1` | 0 | All Session, authorization, ownership, mapping, plan, worklog, and link contracts pass | 2026-09-17T15:21+07:00 |
| Final `tests\Documentation\Verify-P102Docs.ps1 -SelfTestNegative` | 1 | Expected RED: 5 injected contract failures | 2026-09-17T15:21+07:00 |
| Final `git diff --check` | 0 | No whitespace errors; only configured CRLF/LF conversion warnings | 2026-09-17T15:21+07:00 |
| Final `git status --short --branch` | 0 | Branch `anh`; 18 modified files, no staged or untracked files; reviewed P1-01 candidate plus P1-00/P1-02 corrections remain uncommitted for cross-review | 2026-09-17T15:21+07:00 |
| Read-only scope inspection (`git log/diff/status`, `rg`, `Get-Content`, dependency graph queries) | 0 | Confirmed merge base `c48be5d`, task ownership, affected call paths, specification precedence, and final diff scope | 2026-09-17T14:35-15:21+07:00 |

---

## Review handoff

- **Observable demo/output:**
  - [001-backend-boundary.md](../adr/001-backend-boundary.md)
  - [002-authentication.md](../adr/002-authentication.md)
  - [api-errors.md](../api-errors.md)
  - [Verify-P102Docs.ps1](../../tests/Documentation/Verify-P102Docs.ps1)
  - [P1-02-completion.md](P1-02-completion.md)
- **Known gaps, skipped tests, and reason:**
  - Historical environment failure retained above for traceability: 21 integration tests failed at 13:32 because Docker/SQL Server was unavailable. The final full-suite run passed all 43 integration tests and all 104 solution tests, so that environment blocker is resolved for this review run.
  - Runtime integration tests for authentication (tokens, login, middleware) are scheduled for P1-10, P2-10, and P1-12.
  - The approved `device_metadata_json` column and constraints are documentation contracts only in P1-02; implementation, migration, downgrade/recovery note, and SQL Server tests remain required under P2-10.
- **Residual risks:**
  - SDK 10.0.401 runtime baseline: Documented in ADR 001 with assigned follow-up to P2-01 (CI reproduction proof) and pre-release evaluation.
  - Inactive Google authentication packages: Documented in ADR 002 as technical debt to be audited and cleaned in P1-10.
  - The P1 runtime High advisory was removed with negative-first architecture coverage. The remaining High advisory is transitive in the P2-00 IntegrationTests project and remains a separate pre-merge gate item under P2 ownership.
- **Reviewer findings and resolution:**
  - Addressed remaining re-review findings from Person 2:
    1. Corrected ADR 002 ownership table (P1-00 foundation, P2-10 persistence/seeding, P1-10 auth orchestration/API, P1-11 profile/Admin reset, P1-12 project auth); removed session entities from P1-11; clarified UserRole enum is future P2-10 work; defined machine-readable role codes `SUPERVISOR`, `PM`, `DRONE_OPERATOR`, `REPAIR_CREW`; updated Data Dictionary citation to Section 3.1; replaced username/email with internally provisioned credentials.
    2. Strengthened Verify-P102Docs.ps1: exact use-case mappings (`CN01 (Login / Logout)`, `CN02 (Profile)`, `CN03 (Assigned Project / Work Scope)`, `CN10 (Password Reset)`, `QT01 (Account Suspension)`), explicit rejection of obsolete mappings (`CN02 (Logout)`, `CN03 (Change Password)`, `CN10 (Account Suspension)`), role code verification, ownership check rejecting P1-00/P1-11 for sessions, removed status string lock, and added self-test negative mode.
    3. Proved verifier negative behavior: executed both `-SelfTestNegative` mode and external fixture test, proving exit code 1 on injected bad mappings, and exit code 0 on active repository.
    4. Updated worklog status to `Ready for review` without asserting reviewer approval; preserved disclosed integration test environment failure.
  - Final Person 2 re-review of commit `257c908`: no actionable findings. Documentation verifier negative/positive paths, build, affected unit/API tests, formatting, diff scope, authorization policy, session revocation, refresh-token concurrency, audit redaction, and task ownership were independently checked.
  - That review predates the Product Owner-approved Session metadata extension. The new Data Dictionary/ERD/Domain Model/ADR/plan/verifier changes require a fresh Person 2 cross-review before P1-02 can return to `Done`.
  - Wave 0 authorization review findings were resolved by documenting authoritative global/project roles, stale-claim revocation, immediate membership changes, stable 401/403 semantics, and downstream negative tests.
- **Exact next task/action:**
  - Person 2 re-reviews the complete P1 Wave 0 diff and accepts the P2-10/P2-11 handoff. Repository owner keeps the merge gate blocked until this cross-review, the P2-owned integration-test advisory remediation, and a focused commit of the reviewed workspace are complete.
- **Final status:** `Ready for re-review`
