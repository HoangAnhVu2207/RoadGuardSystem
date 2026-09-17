# RoadGuard execution plan — Person 1

Owner: Person 1 (domain/application/API primary). Self-review: task owner. Baseline: 16/09/2026.

This is one of exactly two execution plans. Person 1 takes only one `In Progress` task at a time. Every task follows `AGENTS.md` and the four Negative-First phases. Paths below are target locations; if the actual solution uses different names, preserve its structure and record the mapping in the completion log.

## Task completion contract

For each task: read the traced specification sections; write negative tests first and observe the expected failure; write positive tests; implement; run the narrow tests, affected suite, format, and build; then complete `Antigravity_Completion_Log_Template.md` and the mandatory owner self-review. The owner may self-mark `Done` only when every task gate is green and all self-review findings are resolved.

## Current status and ownership

| Task | Status | Branch | Note |
|---|---|---|---|
| `P1-00` | `Done` | `anh` | Historical cross-review evidence remains unchanged. |
| `P1-01` | `Done` | `anh` | Historical cross-review evidence remains unchanged. |
| `P1-02` | `Done` | `anh` | Historical cross-review evidence remains unchanged. |
| `P1-03` | `Done` | `anh` | Align self-review ownership, conflict controls, and the P2-01 branch-synchronization gate. |

The self-review policy applies to tasks after `P1-02`; it does not rewrite completed evidence.

## Exclusive ownership and conflict control

- Person 1 has exclusive file ownership of API, Services, DTOs, unit tests, and API tests while a Person 1 task is active.
- Person 1 starts a paired domain/API task only after the Person 2 schema/persistence dependency is `Done`. Person 2 first establishes entity/property/enum shape; Person 1 then owns domain methods and invariants in `BusinessObjects` without changing the agreed schema.
- Person 1 must not edit Repositories, migrations, SQL integration fixtures, Docker, CI, or operations files owned by an active Person 2 task.
- Shared hotspots require a single declared owner. Any overlap or required schema reopening must be recorded as a `Conflict warning` with files, task IDs, sequence, and resolution before edits continue.

Default commands once Sprint 0 exists:

```powershell
dotnet test tests/RoadGuardSystem.UnitTests --filter "TaskId=<TASK-ID>"
dotnet test tests/RoadGuardSystem.ApiTests --filter "TaskId=<TASK-ID>"
dotnet format --verify-no-changes
dotnet build --no-restore
dotnet test --no-build
```

## Wave 0 — executable foundation

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-00` / 1d | TE-01, none | Verify/create solution references; enable nullable/analyzers; create `RoadGuardSystem.API`, `BusinessObjects`, `DTOs`, `Services`, unit/API test projects and one solution-level build entry point. Output: `.sln`, project files, `Directory.Build.props`, test skeletons. | Negative: architecture test rejects a forbidden BusinessObjects dependency. Positive: solution restore/build and a smoke unit test pass. |
| `P1-01` / 1d | TE-02, after P1-00 | Add API versioning, OpenAPI, ProblemDetails, stable error-code envelope, correlation middleware, health endpoint and DI composition. Output: API extensions/middleware plus API contract tests. | Negative: malformed request has no stack trace and includes stable code/correlation ID. Positive: health and OpenAPI endpoints respond; API factory boots. |
| `P1-02` / 0.5d | Architecture, after P1-00 | Create ADRs for backend boundary and authentication choice; define the API error-code naming policy; align the approved Session and authorization-authority contracts across Data Dictionary, ERD, Domain Model, Use Cases, and User Stories. Output: `docs/adr/001-backend-boundary.md`, `002-authentication.md`, `docs/api-errors.md`, and documentation verifier updates. | Negative review: verifier rejects `Session.created_at`, missing `device_metadata_json`/`ISJSON`, incorrect P2-10 ownership, missing `UserRoleChanged`, or missing `ProjectMember.role_code` authority. Positive: links and decisions are internally consistent; role changes revoke active credentials, membership changes take immediate effect, and documentation checks pass. |
| `P1-03` / 0.25d | Planning/integration readiness; after P1-02 and P2-00 | Align task-owner self-review, exclusive file ownership, conflict reporting, retired review-only tasks, and the P2-01 branch-synchronization gate. Output: synchronized agent rules, both person plans, completion-log template, and documentation verifier policy checks. | Negative: verifier fails when the self-review, ownership, status, or synchronization tokens are absent. Positive: PowerShell 7 and Windows PowerShell 5 verification pass; historical P1 Wave 0 evidence remains unchanged. |

## Wave 1 — identity and access

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-10` / 1.5d | US-01, CN01-CN03; P2-10 | Implement login/refresh/logout and forced password change application flow with hashed refresh tokens. Validate the JWT role snapshot against authoritative `User.role_code` on every request and fail closed on mismatch. Output: Auth DTOs, service interfaces/implementation, endpoints, error codes. | Negative: null/empty credentials, suspended/pending account, wrong password, expired/revoked/replayed refresh token, stale role claim after `UserRoleChanged`. Positive: login→refresh→logout; old token unusable; current role/session is accepted. |
| `P1-11` / 1d | US-01, CN10; P1-10, P2-10 | Implement profile update and Admin password reset/session revocation. Output: profile/reset commands, endpoints and audit events. | Negative: self-role change, reset suspended user, invalid fields, old session after reset. Positive: allowed profile fields update; reset forces password change and audit contains no secret. |
| `P1-12` / 1d | US-01, TE-03; P2-11 | Add current-user/project authorization service and endpoint policies. For non-Supervisor operations, enforce active/effective membership and authoritative `ProjectMember.role_code`; Supervisor bypass requires current server-side `User.role_code`. Output: authorization handlers/policies and reusable project-scope guard. | Negative: unauthenticated 401, wrong role/project 403, mismatched `ProjectMember.role_code`, ended/expired membership, forged project claim. Positive: assigned member and current Supervisor paths; membership change applies on the next request; no protected query runs before scope check where measurable. |

## Wave 2 — project and survey lifecycle

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-20` / 2d | US-03, DA01, DA03-DA05; P1-12, P2-20 | Implement Project/ProjectMember/Warranty domain rules and create/update/assign/reassign PM use cases. Output: aggregate methods, services, DTOs, endpoints. | Negative: missing handover data, invalid warranty dates, second active primary PM, outside-project caller, stale reassignment. Positive: create project and atomically reassign primary PM with audit. |
| `P1-21` / 1.5d | US-03, DA02, DA12; P2-21 | Implement RoadSection version creation and project-close policy. Output: versioning service, commands/queries and endpoints. | Negative: malformed geometry/SRID, in-place edit of referenced version, close with open work, stale write. Positive: geometry change creates version N+1; old data remains anchored/queryable. |
| `P1-22` / 1.5d | US-04, DA06-DA09; P1-21, P2-22 | Implement survey plan/request creation and postpone rules. Output: domain transitions, services, DTOs/endpoints. | Negative: empty scope, wrong road version, invalid dates, unauthorized PM, invalid/postponed transition. Positive: baseline and periodic plan/request creation with reasoned postpone and audit. |
| `P1-23` / 1.5d | US-05, KS01-KS04, KS14; P1-22, P2-23 | Implement assign/accept/reject/reassign/cancel survey transitions. Output: explicit transition methods and API commands. | Negative: reject after accept, cancel after server confirmation, missing reason, duplicate active assignment, stale version. Positive: assign→reject→reassign→accept and allowed cancel paths. |

## Wave 3 — upload, processing, and AI review

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-30` / 2d | US-06/07, KS05-KS13, CN07-CN09; P2-30 | Implement upload-session/chunk/complete contracts, checksum orchestration and dataset confirmation policy. Output: upload DTOs/services/endpoints. | Negative: empty/oversize chunk, bad order/checksum, missing file, duplicate completion, timeout, caller outside assignment. Positive: resume multipart upload; only backend-confirmed data advances. |
| `P1-31` / 1.5d | US-07, KS10-KS13; P2-31 | Implement processing state machine and AI adapter contract with deterministic fake. Output: job policy, AI request/result DTOs, fake adapter. | Negative: retry permanent data failure, duplicate job key, malformed AI result, timeout/cancellation. Positive: queued→running→completed and retryable failure→retry without duplicate output. |
| `P1-32` / 1.5d | US-08, AI01, AI04-AI07; P1-31, P2-32 | Implement PM keep/edit/reject detection flow; retained detection creates `OPEN` defect plus required inspection task. Output: review commands/queries, transition policy, endpoints. | Negative: non-PM/outside project, already reviewed detection, invalid edit, direct VERIFIED state, missing reason. Positive: keep creates exactly one defect/task; reject retains immutable AI output/history. |

## Wave 4 — field verification and baseline

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-40` / 1.5d | US-20, TN01-TN04/TN12; P1-32, P2-40 | Implement inspection assignment/accept/reject/submit commands and idempotent offline client IDs. Output: task/session DTOs, services and endpoints. | Negative: unassigned crew, reject after accept, malformed/off-scope measurement, duplicate client ID with different payload. Positive: assignment→accept→submit; same retry returns same result. |
| `P1-41` / 1.5d | US-20, TN05-TN06; P1-40, P2-41 | Implement PM review and explicit `OPEN -> VERIFIED/REJECTED` policy; supplements append records. Output: review service/transitions/endpoints. | Negative: task incomplete, no submitted measurement, wrong PM/project, in-place edit, stale review. Positive: confirm and no-defect decisions update state and append audit/verification log. |
| `P1-42` / 1d | US-04, DA10-DA11; P1-41, P2-42 | Implement baseline confirmation cross-aggregate policy and status query. Output: confirmation policy/service/API. | Negative: unconfirmed file, incomplete job, unreviewed detection/open task/defect. Positive: eligible survey confirms baseline once; replay is idempotent. |

## Wave 5 — repair lifecycle

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-50` / 2d | US-11, SC01-SC04; P1-41, P2-50 | Implement repair eligibility, draft batch/version/items, server-calculated estimated total and submit. Output: aggregate policies, DTOs/services/endpoints. | Negative: non-VERIFIED defect, incomplete inspection, duplicate active repair, negative/overflow cost, stale version. Positive: eligible defects create draft; total is deterministic; submit locks version. |
| `P1-51` / 1.5d | US-11, SC05-SC09/SC12; P1-50, P2-51 | Implement Supervisor approve/return/reject and PM new-version/resubmit. Output: approval/version transitions and history API. | Negative: self/incorrect role approval, missing reason, mutate approved version, stale decision. Positive: return whole batch, create N+1, preserve N, resubmit/approve. |
| `P1-52` / 1d | US-12, SC10-SC11; P1-51, P2-52 | Implement assignment/reassignment only from current approved version. Output: assignment commands and endpoints. | Negative: draft/old approved version, duplicate active assignment, crew outside scope. Positive: assign and reassign with immutable history/audit. |
| `P1-53` / 2d | US-13/14, HT01-HT13/HT15; P1-52, P2-53 | Implement crew progress/report, PM inspection/rework and Supervisor final confirmation. Output: per-item state machine, APIs and completion policy. | Negative: missing/unconfirmed evidence, item outside batch, overwrite append-only progress, close with failed/pending item. Positive: submit→inspect→rework/resubmit; passed item does not regress; all-pass closes batch. |

## Wave 6 — query, export, retention, research

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-60` / 2d | US-09/15, AI08-AI12, BC01-BC05; P2-60 | Implement defect match/merge decisions and authorized dashboard/drill-down contracts. Output: query services/DTOs/endpoints. | Negative: cross-project access, incompatible periods, double-counted versions, invalid filters. Positive: each metric drills to source; pending/approved/actual costs remain separate. |
| `P1-61` / 1.5d | US-16, BC06-BC10; P2-61 | Implement export request/status/download authorization and provenance manifest contract. Output: export services/endpoints/manifest DTO. | Negative: oversized scope, outside-project download, unsupported format, duplicate key mismatch. Positive: idempotent request and completed manifest includes filters, versions and checksums. |
| `P1-62` / 2d | US-19, QT11-QT14; P2-62 | Implement retention request/review policy, legal-hold gate and deletion dry run. Output: policy/services/endpoints. | Negative: active legal hold, premature retention, scope drift, requester approves own request if prohibited, stale decision. Positive: request→approve→dry-run with exact scope and audit. |
| `P1-63` / 2d | RS01-RS06; P2-63 | Implement research import/pair/calculation contract (bias, MAE, RMSE, sample count) isolated from operational workflow. Output: import validation, calculation service and report API. | Negative: malformed row, duplicate sample, unit/type mismatch, missing pair, divide-by-zero/all excluded. Positive: golden dataset reproducibly calculates metrics and never creates Defect/Warranty transitions. |
| `P1-64` / 2d | US-17, CN04, QT01-QT05; P2-64 | Implement Admin account suspend/reactivate and global-role change with open-work handover list, notification inbox, defect catalog/severity-rule versioning and reminder-rule APIs. Global-role change emits `UserRoleChanged`, audits before/after state, and atomically revokes active sessions/refresh tokens. Output: admin/notification/config services, DTOs and endpoints. | Negative: non-Admin, suspend already suspended user, self-escalation, stale role token remains usable, partial role-change/session-revocation transaction, mutate active rule version, reminder auto-creates survey, sync draft transfer. Positive: suspend/role change revokes sessions, preserves history/drafts, lists reassignment work and publishes versioned rules/reminders. |
| `P1-65` / 1.5d | US-10/18, AI14, QT06-QT10; P2-65 | Implement AI model/job/device administration plus training-label approval/export workflow. Output: admin policies, services, DTOs/endpoints and versioned export contract. | Negative: activate invalid model, retry data failure, unauthorized device/job access, export unapproved labels, mutate approved label. Positive: activate one model version, inspect/retry eligible job, register device, approve labels and create reproducible dataset export. |

## Person 1 release obligations

After the final task, run the full seed scenario `project -> survey -> upload -> processing -> detection -> field measurement -> repair -> export`, complete owner self-review, resolve every conflict warning, and ensure every task has a completion log. Release is blocked by any skipped authorization/integrity test, unrecorded migration, mutable evidence/history, or undocumented specification conflict.
