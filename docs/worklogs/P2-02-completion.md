# Antigravity completion log - P2-02

## Assignment history

### Assignment 1 - prepared 2026-09-18T04:50:46+07:00

- Assignment status: `Blocked`. The implementation assignment is complete, but implementation has not started and this preparation is not implementation evidence.
- Task ID / title: `P2-02` / Audit, outbox, idempotency, concurrency primitives and transaction conventions.
- Person / branch: Person 2 / `huy`.
- Baseline: local `huy` HEAD `2890d29ab2bb7030f475b8fb902ee8d43ea7026e`; worktree was clean before this assignment; local branch was one commit ahead of `origin/huy`.
- Objective: establish reusable SQL Server persistence primitives that later RoadGuard commands can use to commit one domain change, its append-only audit event, and its outbox intent atomically; support scoped idempotent replay, deduplicated at-least-once effects, and optimistic concurrency without implementing a business workflow.
- Assignment write authority: repository-owner request authorizes this task-scoped worklog and P2-02 plan metadata only. `docs/worklogs/P2-02-completion.md` did not exist, `planning/RoadGuard_Plan_Person_2.md` was writable, and no other Person 2 row was in `In Progress`, `Ready for review`, `Changes requested`, or `Blocked` before this assignment.

#### Dependency and baseline evidence

| Dependency | Checkout evidence | Assessment |
|---|---|---|
| `P2-00` | Commit `b2662fe` is an ancestor of HEAD. `RoadGuardDbContext`, SQL Server/NetTopologySuite registration, isolated SQL fixture, and P2-00 worklog are present. | Satisfied in this checkout. |
| `P2-01` | Commits through `77505f2` are ancestors of HEAD; local finalization commit is `2890d29`. The worklog records hosted CI run `35277820417` passing for `77505f2`; Compose, CI, seeder, security gate, and SQL test infrastructure are present. | Satisfied in this checkout. |
| `P2-03` | Commit `4c40433` is an ancestor of HEAD. ADR 003, corrected plans/specifications, verifier changes, and `docs/worklogs/P2-03-completion.md` are present. | Satisfied in this checkout. |
| Current Antigravity/Codex workflow | Current policy commit `b50b86f` is present on local `anh`/`develop` but is not an ancestor of `huy`. This checkout's `AGENTS.md`, completion template, and plan still state that the owner may self-mark `Done`, contrary to the current mandatory Codex acceptance contract supplied by the repository owner. | Blocking process dependency. Owner-approved integration is required; no merge, cherry-pick, rebase, pull, or push is authorized by this assignment. |

Blocker resolution: the repository owner must authorize integration of `b50b86f` or a verified successor into `huy`, preserving the accepted P2-00/P2-01/P2-03 artifacts and resolving shared plan/template changes explicitly. After integration, Antigravity must re-run status/HEAD/dependency checks and confirm this assignment still matches the resulting diff before changing P2-02 to `In Progress`.

#### Trace and source precedence

- Plan trace: `TE-05`, `TE-07`; these labels occur in the P2-02 plan row but no standalone definitions were found in the five canonical `docs/diagram` specifications. This is a trace-label gap, not permission to invent additional business scope.
- Concrete requirements, in repository precedence order:
  - `docs/diagram/RoadGuard_Data_Dictionary_v1.md`: `AuditLog` fields and no-secret rule (lines 740-756); restricted audit data, allow-list snapshots, redaction, and append-only storage (lines 878-900); application logging and UTC requirements (lines 917-921).
  - `docs/diagram/RoadGuard_ERD_v1.md`: `AUDIT_LOG.entity_id` is intentionally polymorphic rather than a physical FK (line 29); user/audit relationship and core fields (lines 888-923).
  - `docs/diagram/RoadGuard_Domain_Model_v1.md`: `AuditLog` is a common event sink, not a business aggregate; it is append-only/read-only and does not replace specialized identity logs (lines 245-252).
  - `docs/diagram/Dac_ta_UseCase_v2.md`: project scope and authorization are cross-cutting preconditions; retry, immutable history, read-only audit, SQL Server types, and UTC rules apply (lines 65-88).
  - `docs/diagram/User_Stories_Acceptance_Criteria_v2.md`: cross-cutting audit and offline/retry quality requirements; P2-02 is foundation work and does not complete a user story by itself.
  - `docs/adr/003-backend-delivery-and-ai-boundary.md`: offline synchronization contract for scoped idempotency, fingerprint/outcome replay, atomic domain/audit/outbox commit, at-least-once delivery with deduplicated effects, and expected-version conflicts (lines 40-46). ADR 003 also fixes the dependency edge `P2-02 -> P2-10` (line 58).
- No specification conflict was found for the agreed behavior. The plan-only `TE-05`/`TE-07` labels and the missing current workflow baseline must remain visible verification gaps until corrected; they must not be silently redefined.

## Acceptance criteria

The IDs below are stable across implementation, self-review, fix, and Codex acceptance rounds.

- [ ] **P2-02-AC-01 - SQL schema and migration.** Add production mappings and a new migration for `AuditLog`, durable outbox messages, scoped idempotency records, and consumer-effect deduplication. Map UTC instants as `datetimeoffset`; map JSON payload/snapshot columns as `nvarchar(max)` with `ISJSON` checks when non-null; add bounded lengths and required indexes/unique constraints. `AuditLog` contains the Data Dictionary fields, remains append-only at application and database level, and keeps its polymorphic `entity_type`/`entity_id` without a physical entity FK. The nullable `actor_user_id` FK is deliberately deferred to a new P2-10 migration because `User` does not exist yet; project scope is stored without creating a placeholder `Project` or dependency cycle. Include empty-database apply plus downgrade/reapply or equivalent recovery evidence; never rewrite an already shared migration.
- [ ] **P2-02-AC-02 - Atomic transaction convention.** Provide one reusable repository-owned transaction boundary, using an explicit service or interceptor pattern, in which a representative domain write, its audit entry, its idempotency outcome, and its outbox intent commit together. A forced failure after staging any subset rolls back every member; no audit/outbox/idempotency success record may survive without the domain write, and no domain write may survive without its required audit/outbox records.
- [ ] **P2-02-AC-03 - Scoped idempotency and race behavior.** Uniqueness is scoped by actor, project, operation, and caller-supplied idempotency key. Persist a validated request fingerprint and a stable outcome/operation identity. Same scope/key plus the same fingerprint returns the recorded identity without repeating the domain change or side effect; a changed fingerprint conflicts; concurrent first submissions produce exactly one committed outcome. Unique-insert races are handled separately from optimistic-concurrency failures. The persistence contract must retain enough scope for the P1 service layer to recheck current authorization before returning replayed data.
- [ ] **P2-02-AC-04 - Optimistic concurrency primitive.** Add a reusable SQL Server `rowversion` convention/marker for mutable aggregates. A stale update is detected as `DbUpdateConcurrencyException` (or an equally explicit repository result) and never overwrites the winning value. P2-02 does not define HTTP status/error mapping; that remains with the owning P1 service/API task.
- [ ] **P2-02-AC-05 - Audit minimization, redaction, and immutability.** Audit snapshots are built from an explicit allow-list and redact case-insensitive sensitive keys including `password`, `token`, `secret`, `authorization`, `cookie`, and `connectionString`, including nested objects/arrays. Persisted snapshots are valid JSON or null. Audit rows cannot be updated or deleted through the application path or direct SQL path used by the production runtime convention; corrections append a new event. Tests prove no supplied sensitive value appears in persisted audit, outbox, exception, or application-log output.
- [ ] **P2-02-AC-06 - At-least-once outbox contract with deduplicated effects.** Persist a stable message/event identity, type, UTC occurrence time, correlation identity, and valid JSON payload in the same transaction as the write. Re-delivery may occur; recording the same message for the same consumer twice produces one committed effect/receipt. A broker, polling worker, leasing, retry schedule, and domain-specific handlers remain P2-31 or later work; P2-02 must not claim universal exactly-once delivery.
- [ ] **P2-02-AC-07 - SQL Server and regression evidence.** Negative tests are authored and observed failing for the intended behavioral reason before positive contracts and implementation. Run P2-02 integration tests on real SQL Server/Testcontainers with zero skipped/zero-discovered ambiguity, then restore, non-incremental build, format verification, the affected/full test suites, dependency-security gate, migration apply/recovery checks, `git diff --check`, and database/container cleanup checks. Record exact commands, exit codes, counts, environment, and timestamps.
- [ ] **P2-02-AC-08 - Handoff and acceptance.** Antigravity records all changed files, migration recovery notes, expected RED/GREEN chronology, conflict warnings, and owner self-review for authorization/scope, transitions, immutability, idempotency, concurrency, audit, secrets, and missing tests. Antigravity may submit `Ready for review` only after AC-01 through AC-07 pass on the exact submitted diff. Only Codex may record `Done` after mandatory acceptance verifies the submitted artifacts and all blockers/findings are closed.

Completing a test slice or one primitive does not complete P2-02. All eight ACs remain required for the task verdict.

## Scope and design boundary

### In scope

- Entity/property shape owned by Person 2 for the four generic persistence concerns: common audit, outbox, idempotency/outcome, and consumer-effect deduplication.
- EF Core SQL Server mappings, indexes, constraints, migration, recovery note, and `RoadGuardDbContext` integration.
- Repository-facing interfaces/services or interceptors needed to create the atomic unit and expose explicit duplicate/stale outcomes to later P1 orchestration.
- A reusable rowversion marker/convention for future mutable aggregates.
- Allow-list audit snapshot serialization/redaction and SQL/app append-only enforcement.
- SQL Server integration probes and tests that demonstrate transaction, rollback, race, replay, JSON, append-only, redaction, and stale-version behavior without inventing a RoadGuard business workflow.

### Explicitly out of scope

- Login/session/refresh-token/account tables and specialized audit logs (`P2-10`); project/membership schema (`P2-20`/`P2-11`). Do not create placeholder `User` or `Project` tables. Add their FKs later through new migrations.
- Business authorization policy, current-membership lookup, public DTOs, stable HTTP conflict/error mapping, controllers, or API tests (`P1-10`/`P1-12` and later P1 tasks).
- Domain-specific events, aggregate transitions, or audit emission for future survey, defect, repair, retention, or research tasks.
- Notification persistence (`P2-07`), background dispatch/lease/retry worker infrastructure (`P2-31`), a message broker, cloud dependency, or universal exactly-once guarantees.
- Changes to API, Services, DTOs, existing P1 unit/API tests, `BaseEntity`, SDK/package versions, CI/Compose/seeder behavior, product specifications, or unrelated backlog items.
- Git commit, merge, cherry-pick, rebase, pull, push, integration into `develop`, publication, or deployment under this preparation request.

## Actor, scope, preconditions, transitions, and audit

- Authorized actor: P2-02 is infrastructure, not an end-user command. Integration probes use an explicit synthetic actor/project/correlation identity. A nullable actor is permitted only for an identified backend/system source; anonymous business mutation is not introduced.
- Project scope: persist actor + project + operation in the idempotency scope. Global/system operations may use a documented null project; business commands must supply their project when their owning task is implemented. Current authorization must be rechecked by the later service before replay data is returned; P2-02 does not implement or bypass that guard.
- Preconditions: current workflow policy is integrated onto `huy`; dependencies above remain ancestors/present; SQL Server/Testcontainers is available; the implementation file list is declared in this log; no active task owns a listed hotspot.
- Business transition: N/A because this task adds generic persistence primitives and no RoadGuard aggregate transition. The test probe demonstrates transaction state only and must not be presented as product behavior.
- Internal state: an idempotent operation has no committed successful outcome until the whole transaction commits; an outbox message begins pending and may later be marked/receipted by a consumer without changing the immutable event identity/payload. Worker lease/retry transitions are deferred to P2-31.
- Failure behavior: malformed/empty/oversize keys, operation names, fingerprints, event types, JSON, or outcomes are rejected; changed-payload replay conflicts; stale rowversion conflicts; duplicate consumer delivery returns the prior effect; forced DB failure rolls back the complete unit.
- Audit event for the integration probe: `p2_02.transaction_probe_committed`, source `integration_test`, UTC timestamp and correlation ID, with allow-listed before/after data only. This event name is test evidence, not a public product event contract.
- Stable API error codes: N/A for P2-02 because no API contract is added. Repository results must distinguish replay, key/fingerprint conflict, unique-insert race, and stale concurrency so the owning P1 task can map stable errors later.

## Exclusive file ownership and shared hotspots

Person 2/Antigravity may create or modify only the following implementation paths after the blocker is resolved and the task is moved to `In Progress`:

- `RoadGuardSystem.BusinessObjects/Auditing/**`
- `RoadGuardSystem.BusinessObjects/Concurrency/**`
- `RoadGuardSystem.BusinessObjects/Idempotency/**`
- `RoadGuardSystem.BusinessObjects/Messaging/**`
- `RoadGuardSystem.Repositories/Auditing/**`
- `RoadGuardSystem.Repositories/Concurrency/**`
- `RoadGuardSystem.Repositories/Idempotency/**`
- `RoadGuardSystem.Repositories/Messaging/**`
- `RoadGuardSystem.Repositories/Transactions/**`
- `RoadGuardSystem.Repositories/Configurations/**` for P2-02 mappings only
- `RoadGuardSystem.Repositories/Migrations/**` for new P2-02 migration artifacts only
- `RoadGuardSystem.Repositories/RoadGuardDbContext.cs`
- `RoadGuardSystem.Repositories/Extensions/RoadGuardPersistenceExtensions.cs`
- `tests/RoadGuardSystem.IntegrationTests/Persistence/P202*`
- `tests/RoadGuardSystem.IntegrationTests/Infrastructure/P202*`
- `docs/worklogs/P2-02-completion.md`
- P2-02 status/note cells only in `planning/RoadGuard_Plan_Person_2.md`

Shared hotspots requiring a conflict check immediately before edits: `RoadGuardDbContext.cs`, persistence DI registration, both project files if a reference is truly required, `RoadGuardSystem.slnx`, and the Person 2 plan. No new package is expected for this task. `RoadGuardSystem.slnx`, `global.json`, `Directory.Build.props`, API/Services/DTO files, specifications, ADRs, and P1 tests are not assigned; stop and record a conflict warning before touching them. Directory ownership above does not authorize changes outside P2-02 behavior.

Current conflict warning: workflow metadata is divergent across branches. `huy` owns the P2-02 implementation artifacts, but current P1-06 policy lives at `b50b86f` on `anh`/`develop`. Required sequence is owner-approved policy integration first, explicit conflict resolution for shared plans/template, then P2-02 implementation. No production-file overlap is currently observed.

## Required negative-first checks

Write the negative/edge tests first and capture a RED caused by missing P2-02 behavior, not by compile failure, unavailable Docker/SQL, or malformed test setup.

| Order | Check | Required assertion |
|---:|---|---|
| 1 | Empty/malformed/oversize idempotency key, operation, fingerprint, event metadata, or JSON | Rejected before persistence with an explicit repository result/exception; no partial rows. |
| 2 | Same key/scope with changed fingerprint | Conflict; original outcome remains unchanged; no second domain write/audit/outbox effect. |
| 3 | Concurrent first request with the same scoped key | Exactly one outcome commits; loser resolves deterministically as replay/race, not a second effect. |
| 4 | Same key under different actor, project, or operation | Distinct scope is allowed and cannot read/overwrite another scope's result. |
| 5 | Stale rowversion update | Stale writer fails and winning value remains unchanged. |
| 6 | Unique-key insertion conflict vs stale update | Two cases are distinguishable; neither is silently converted into success. |
| 7 | Forced failure after domain/audit/outbox/idempotency staging | Entire transaction rolls back and retry can succeed once. |
| 8 | Sensitive keys/values at multiple JSON depths and casing variants | Persisted audit/outbox/log/exception evidence contains none of the supplied sensitive values. |
| 9 | Audit update/delete through EF and direct SQL/runtime convention | Rejected; correction requires a new row. |
| 10 | Invalid JSON written to audit/outbox/outcome payload | SQL `ISJSON` constraint rejects it; valid object/array contract is documented per column. |
| 11 | Re-delivery to the same consumer | One effect/receipt; retry returns the prior result. |
| 12 | SQL/container interruption | Failure is surfaced, no false success is recorded, cleanup leaves no P2-02 test database/container. |

Authorization/forbidden-role API tests and business invalid-transition tests are N/A here because P2-02 exposes no endpoint or business state machine. Their replacement is explicit scope-isolation and transaction tests; later P1 tasks must test current membership/role before using replayed outcomes.

## Required positive checks

| Order | Check | Required assertion |
|---:|---|---|
| 1 | Smallest valid scoped idempotent write | Domain probe, sanitized audit, outcome identity, and outbox message commit atomically on SQL Server. |
| 2 | Same key + same fingerprint replay | Returns the identical outcome/operation identity with unchanged row/effect counts. |
| 3 | Distinct scoped commands | Independent records are persisted without cross-scope leakage. |
| 4 | Fresh expected rowversion update | Commits once and advances the database-generated token. |
| 5 | Outbox re-delivery with consumer deduplication | At-least-once delivery attempt produces one durable consumer effect. |
| 6 | Migration lifecycle | Empty database applies; downgrade/reapply or documented recovery path succeeds; schema/index/check/append-only behavior is verified from SQL metadata and DML. |

## Implementation sequence and evidence gate

1. Resolve and record the workflow synchronization blocker; re-read HEAD/status/plan/dependencies and confirm exclusive paths.
2. Change only P2-02 to `In Progress` when Antigravity actually starts implementation.
3. Add negative/edge integration tests and capture valid behavioral RED in the command log.
4. Add the positive contracts before production implementation.
5. Implement the minimum entity shapes, mappings, migration, transaction boundary, redactor, idempotency/race handling, outbox deduplication, and concurrency convention required by AC-01 through AC-06.
6. Run narrow P2-02 SQL tests, repair to GREEN, then run migration lifecycle and full repository gates.
7. Self-review the exact diff, update the sections below with facts rather than planned results, and submit `Ready for review`. Stop editing submitted artifacts during Codex review.

### Ready for review gate

AC-01 through AC-07 are backed by implementation/evidence, and AC-08 is complete through the Antigravity handoff fields. Negative-first chronology is recorded; real SQL Server tests have non-zero discovered counts and no unexplained skips; migration recovery is proven; restore/build/format/affected/full tests/security/diff checks pass; cleanup is verified; changed files stay inside ownership; self-review has no unresolved mandatory finding; dependency and workflow conflicts are resolved. Antigravity records `Ready for review` but not `Done`.

### Done gate

Codex reviews the exact submitted commit or identified staged/unstaged/untracked diff, verifies dependencies are present in that checkout, reruns applicable checks, closes all mandatory findings and verification gaps, records reviewer/time/revision/checks/verdict in this log, and only then changes P2-02 to `Done`. Done does not authorize merge, push, publication, deployment, or starting P2-10.

## Implementation evidence

No implementation, tests, migration, production edits, commit, integration, or publication were performed by this assignment. Antigravity must append evidence here without replacing the assignment history.

### Files changed by implementation

Not applicable at assignment time. Record the exact implementation diff during the implementation phase.

### Negative-first evidence

Not executed at assignment time. Planned checks are fixed above; observed RED/GREEN evidence belongs to Antigravity.

### Positive evidence

Not executed at assignment time.

### Commands run by assignment preparer

| Command/check | Exit | Result | Time |
|---|---:|---|---|
| `git status --short --branch`; `git rev-parse HEAD` on initial checkout | 0 | Initial branch `anh`, clean, HEAD `b50b86f`; identified wrong owner branch for P2-02. | 2026-09-18 |
| Read current rules/skills, both plans, task row, template, and shared workflow prompt | 0 | Confirmed Person 2 ownership, `huy` branch, mandatory assignment fields, and no production authorization. | 2026-09-18 |
| Read-only `git show huy:...`, ancestry checks, branch/log inspection | 0 | Verified P2-00/P2-01/P2-03 artifacts at `huy`; found current workflow commit absent from its ancestry. | 2026-09-18 |
| `git switch huy`; `git status --short --branch`; `git rev-parse HEAD` | 0 | Switched under standing branch policy; clean `huy`, HEAD `2890d29`, ahead of `origin/huy` by one. | 2026-09-18 |
| Read canonical specs/ADR in precedence order and inspect repository/test structure | 0 | Fixed P2-02 behavior, scope, tests, staged FK constraints, and exclusive paths; found plan-only `TE-05`/`TE-07` labels. | 2026-09-18 |
| Writable-path and active-task checks | 0 | Plan writable, worklog absent, no other active unfinished P2 assignment status before this update. | 2026-09-18T04:50:46+07:00 |
| `pwsh -NoProfile -File tests/Documentation/Verify-P102Docs.ps1`; same verifier under Windows PowerShell | 0 / 0 | Current documentation, task status, trace, and dependency contracts passed in both shells. | 2026-09-18 |
| `pwsh -NoProfile -File tests/Documentation/Test-P203Planning.ps1`; same regression suite under Windows PowerShell | 0 / 0 | All nine planning/status/dependency regression cases passed in both shells. | 2026-09-18 |
| Assignment structure check for required sections and unique `P2-02-AC-*` IDs | 0 | Eight unique AC IDs, no missing required assignment field, and no `In Progress` assignment claim. | 2026-09-18 |
| `git diff --check`; final scoped status/diff inspection | 0 | No whitespace error; only the P2-02 plan row and new P2-02 worklog are modified/untracked. | 2026-09-18 |

## Antigravity self-review and handoff

Not started. Antigravity must append its self-review and handoff; it must not overwrite this assignment or self-mark `Done`.

## Codex acceptance review history

No acceptance round exists. Preserve each future review round with stable finding IDs and disposition history.
