# RoadGuard execution plan — Person 1

Owner: Person 1 (`anh`, endpoint/API primary). Baseline: 16/09/2026; lightweight endpoint workflow adopted 20/09/2026.

This is one of exactly two execution plans. Historical `Done` rows and their worklogs are immutable evidence. New work follows `AGENTS.md`: show scope first, wait for the owner's approval in the same session, then implement one small slice. A blocked product task may coexist with an owner-approved tooling task only when their exclusive files do not overlap.

## Backend delivery boundary — owner clarification, 2026-09-18

The two-week target covers backend MVP and backend Research Validation software. Android and Web Dashboard belong to FE; real AI training/inference/DSM generation and field data collection are external follow-ups. Backend processing uses a deterministic mock behind the existing adapter boundary, and Research Validation is testable with controlled imported/synthetic pairs. Real AI accuracy and field-trial conclusions are not prerequisites for backend software acceptance and are not claimed by mock tests. See [ADR 003](../docs/adr/003-backend-delivery-and-ai-boundary.md) for contracts, unresolved product decisions and research sources.

Task IDs remain stable. Wave headings group features; explicit dependencies, not row order, determine execution. New P2-04–P2-07 tasks extract prerequisites previously buried in P2-30/P2-64/P2-65; their ownership is exclusive. Only completed schema tasks hand entities to Person 1. Each parent task may have several acceptance checkpoints, but is Done only when every checkpoint passes.

## Workflow for new and resumed tasks

Before editing, Codex posts a scope card containing task/owner, goal, In scope, Out of scope, files, dependencies, verification tier and side effects. Work starts only after explicit owner approval: `Dong y <TASK-ID>` or an equally clear reply. An endpoint gets a 5-8 line contract, a project build, a real `.http` smoke call, and only risk-based tests. Choose one test breadth before running tests: focused, affected-project, or full-solution; do not run them as a sequence. Reuse unchanged verification evidence; rerun only checks invalidated by later edits. A commit alone does not trigger more tests. Full-solution tests are integration/release gates or an explicit owner request. Larger work is split into 3-5 approved slices before code. See [shared prompts](../docs/prompts/RoadGuard_Task_Workflow.md).

The old Negative-First, mandatory self-review and independent acceptance workflow is historical from 20/09/2026 onward. Do not rewrite its `Done` evidence. Existing task-row negative/positive examples are a risk catalogue, not a required order or test matrix; the approved scope card selects the cheapest sufficient verification.

## Current status and ownership

| Task | Status | Branch | Note |
|---|---|---|---|
| `P1-00` | `Done` | `anh` | Historical cross-review evidence remains unchanged. |
| `P1-01` | `Done` | `anh` | Historical cross-review evidence remains unchanged. |
| `P1-02` | `Done` | `anh` | Historical cross-review evidence remains unchanged. |
| `P1-03` | `Done` | `anh` | Align self-review ownership, conflict controls, and the P2-01 branch-synchronization gate. |
| `P1-04` | `Done` | `anh` | Rules/skill corrected; Microsoft Learn and Context7 configured in workspace and live queries verified. See P1-04 completion log; existing IDE may need refresh. |
| `P1-05` | `Done` | `anh` | Codex review suite for both plans; metadata, 20 links, existing verifiers and six independent scenarios pass. Exclusive paths: three new `.agents/skills/roadguard-review*` folders, this plan's P1-05 entries, and `docs/worklogs/P1-05-completion.md`. Existing P1-04 changes preserved; local artifacts only, no integration claimed. |
| `P1-06` | `Done` | `anh` | Owner-requested workflow migration and reusable prompts accepted by separate Codex review; applicable checks and ten scenarios pass. Codex-authored bootstrap exception recorded in P1-06 log; future implementation/self-review belongs to Antigravity. Local acceptance only; no integration/publication. |
| `P1-07` | `Done` | `anh` | Independent Codex acceptance round 1 recorded 2026-09-19 for the manifest-bound working-tree submission at baseline `f6d4628`; AC-01 through AC-09 and required documentation/tooling checks passed with no findings or blockers. Local acceptance only; no commit, integration or publication performed. |
| `P1-08` | `Done` | `anh` | Independent Codex acceptance Round 1, 2026-09-19: AC-01..06 verified for all 12 skill files and submitted scoped plan rows. Four skill validators, 71 local links, docs/setup checks and six source-backed scenarios pass; no open findings. See [P1-08 worklog](../docs/worklogs/P1-08-completion.md). Source manifest preserved; local acceptance only, no commit/integration/publication. |
| `P1-09` | `Done` | `anh` | Independent Codex acceptance round 1 on 2026-09-19 verified AC-01..04 for source digest `daed134e...4108b`; documentation/setup checks, 9/9 planning scenarios, mirror/hash/link checks passed with no findings or blockers. See [worklog](../docs/worklogs/P1-09-completion.md). P1-08/P1-10 artifacts and entries preserved; local acceptance only, no commit/integration/publication. |
| `P1-10` | `Done` | `anh` | Independent Codex acceptance Round 4 recorded 2026-09-19 for exact 54-file working-tree manifest `1dd5cc45...fb9ab`. F-07 and VG-04 are Verified; prior F-01..F-06 and VG-02/VG-03 remain Verified. Fresh reviewer gates passed: Unit 39/39, API 56/56, SQL P1-10/P2-10 66/66, full solution 346/346, build/format/security/model/docs checks, with no failed/skipped tests or open finding. Local acceptance only; no commit, integration, push, deployment or publication. See [P1-10 worklog](../docs/worklogs/P1-10-completion.md). |
| `P1-11` | `Done` | `anh` | Independent Codex acceptance recorded 2026-09-21 for the 23-file implementation/test manifest `c80f0251...90b827e` (review metadata excluded). Profile/reset API, SQL persistence, affected builds, docs verifier and full solution `401/401` passed with zero failures/skips and no open findings. Schema-free; no commit, merge, push, deployment or publication. See [P1-11 worklog](../docs/worklogs/P1-11-completion.md). |
| `P1-70` | `Done` | `anh` | Lightweight workflow reset completed 2026-09-20. One endpoint-delivery skill, compact rules, scope-first prompts/plans, retired `.antigravity`, compiler gates and replacement verifiers pass. Approved baseline analyzer debt remains visible as warnings, with five additional rule IDs scoped only to `*Tests` projects. See [P1-70 worklog](../docs/worklogs/P1-70-completion.md). |

Rows completed before P1-70 retain their original acceptance meaning. P1-70 supersedes only prospective workflow rules; it does not reopen, re-review or weaken any completed business or tooling task.

## Owner-approved lightweight workflow reset - P1-70

| ID / status / branch | Trace and dependency | In scope / exclusive paths | Out of scope / checks |
|---|---|---|---|
| `P1-70` / `Done` / `anh` | TE-01/10; direct owner approval, 2026-09-20 | `AGENTS.md`, `.agents/**`, `.antigravity/**` removal, both plan policy sections, shared prompt, neutral task template, `Directory.Build.props`, agent/documentation verifiers and this worklog. Four slices completed: record transition; rebuild rules/skill; align plans/prompt/compiler; verify. | No controller/Minimal API conversion, feature endpoint, entity/schema/migration/package/runtime behavior, P1-11 implementation, P2-01 CI implementation, merge or push. CA1805/CA1512/CA1000/CA1861 are approved baseline warnings; all other Recommended diagnostics remain errors. |

## Owner-requested context reduction — P1-09

| ID / status / branch | Trace and dependency | Scope and exclusive paths | Required checks and gate |
|---|---|---|---|
| `P1-09` / `Done` / `anh` | TE-01/10; P1-07; owner request to apply compact prompts, 2026-09-19 | Bounded documentation task requested while P1-08 awaits review; preserve its frozen artifacts and entries. Exclusive paths: `AGENTS.md`, `.antigravity/AGENTS.md`, `docs/prompts/RoadGuard_Task_Workflow.md`, `docs/worklogs/P1-09-completion.md`, and only P1-09 entries in this plan. Reduce repeated context/output and stale handoffs; retain every existing acceptance/security/SQL gate. No production, tests, skills, P1-08/P1-10 records or Git integration edits. | Documentation/setup/planning verifiers, mirror/link checks, scenario self-review and whitespace. Prose-only: no runtime tests or artificial RED. Codex Implementer submits Ready for review; separate Codex Reviewer alone may mark Done. |

## Owner-approved tooling extension — P1-08

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-08` / tooling | TE-01/10; P1-04, P1-07 | Owner-requested RoadGuard C# reference suite: core conventions/source map, application/API contracts, EF Core SQL Server persistence and unit/API/SQL verification. Exclusive files are fixed in the P1-08 status row and worklog. Owner approved this bounded tooling task alongside P1-10 on 2026-09-19; only P1-08 entries may be edited in this shared plan. Existing business tasks and policies remain unchanged. | Validate four skill frontmatters/UI metadata, local links and discovery; run existing documentation/setup verifiers and independent retrieval/application scenarios; record self-review and exact artifact identity. No runtime behavior change, artificial RED or wording tests. Independent reviewer acceptance is required for Done. |

## Exclusive ownership and conflict control

- Person 1 has exclusive file ownership of API, Services, DTOs, unit tests, and API tests while a Person 1 task is active.
- Person 1 starts a paired domain/API task only after the Person 2 schema/persistence dependency is `Done`. Person 2 first establishes entity/property/enum shape; Person 1 then owns domain methods and invariants in `BusinessObjects` without changing the agreed schema.
- Person 1 must not edit Repositories, migrations, SQL integration fixtures, Docker, CI, or operations files owned by an active Person 2 task.
- Shared hotspots require a single declared owner. Any overlap or required schema reopening must be recorded as a `Conflict warning` with files, task IDs, sequence, and resolution before edits continue.

Default endpoint ladder (select the cheapest sufficient level in the approved scope):

```powershell
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly
dotnet watch --project RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj
# Choose exactly one test command below.
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~<Feature>" -v q
dotnet test tests/RoadGuardSystem.ApiTests -v q # shared API behavior affecting multiple features
dotnet test RoadGuardSystem.slnx -v q # integration/release or explicit owner request only
```

## Wave 0 — executable foundation

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-00` / 1d | TE-01, none | Verify/create solution references; enable nullable/analyzers; create `RoadGuardSystem.API`, `BusinessObjects`, `DTOs`, `Services`, unit/API test projects and one solution-level build entry point. Output: `.sln`, project files, `Directory.Build.props`, test skeletons. | Negative: architecture test rejects a forbidden BusinessObjects dependency. Positive: solution restore/build and a smoke unit test pass. |
| `P1-01` / 1d | TE-02, after P1-00 | Add API versioning, OpenAPI, ProblemDetails, stable error-code envelope, correlation middleware, health endpoint and DI composition. Output: API extensions/middleware plus API contract tests. | Negative: malformed request has no stack trace and includes stable code/correlation ID. Positive: health and OpenAPI endpoints respond; API factory boots. |
| `P1-02` / 0.5d | Architecture, after P1-00 | Create ADRs for backend boundary and authentication choice; define the API error-code naming policy; align the approved Session and authorization-authority contracts across Data Dictionary, ERD, Domain Model, Use Cases, and User Stories. Output: `docs/adr/001-backend-boundary.md`, `002-authentication.md`, `docs/api-errors.md`, and documentation verifier updates. | Negative review: verifier rejects `Session.created_at`, missing `device_metadata_json`/`ISJSON`, incorrect P2-10 ownership, missing `UserRoleChanged`, or missing `ProjectMember.role_code` authority. Positive: links and decisions are internally consistent; role changes revoke active credentials, membership changes take immediate effect, and documentation checks pass. |
| `P1-03` / 0.25d | Planning/integration readiness; after P1-02 and P2-00 | Align task-owner self-review, exclusive file ownership, conflict reporting, retired review-only tasks, and the P2-01 branch-synchronization gate. Output: synchronized agent rules, both person plans, completion-log template, and documentation verifier policy checks. | Negative: verifier fails when the self-review, ownership, status, or synchronization tokens are absent. Positive: PowerShell 7 and Windows PowerShell 5 verification pass; historical P1 Wave 0 evidence remains unchanged. |

## Wave 1 — identity and access

Agent tooling prerequisite (does not implement a business endpoint):

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-04` / 0.5d | TE-01, delivery tooling; P1-03 | Synchronize both AGENTS files, repair RoadGuard skill/references, add native Antigravity discovery entries and two workspace documentation MCP servers. Output: agent documents, `.agents/mcp_config.json`, setup verifier and completion log. | Negative: identify stale paths/policy contradictions before editing; malformed configuration/missing discovery cannot pass setup checks. Positive: mirrored rules, skill validation, existing doc checks, MCP initialize/tools/list and real public-document queries. |
| `P1-05` / tooling | TE-01/10, owner-requested review tooling; P1-04 | Create discoverable Codex review skills for shared review, P1 application/API tasks and P2 persistence/operations tasks. Centralize evidence/reporting and cross-owner handoff checks; derive task scope from current plans. Exclusive paths are listed above. | Negative: synthetic authorization/SQL/evidence scenarios and read-only scope checks before authoring. Positive: skill metadata and local references validate; existing documentation/setup checks pass; independent scenario review and owner self-review recorded. No production changes or mandatory new cross-review gate. |
| `P1-06` / tooling | TE-01/10; owner-requested workflow migration; P1-05 | Require Antigravity implementation/self-review followed by Codex acceptance and authorized Done updates; provide reusable assignment, implementation/fix and review prompts for either person. Exclusive ownership: both AGENTS files, current-policy sections of both existing plans, completion template, maintained delivery skill and its handoff/negative-first references, three review skills and affected references/UI metadata, `docs/prompts/RoadGuard_Task_Workflow.md`, `tests/Documentation/Verify-P102Docs.ps1`, this task log. Preserve historical logs/statuses and business task rows. Sequence: capture baseline; update policy/prompts; align existing verifier; validate; self-review; separate Codex review; record verdict. | Negative: expose old self-mark/optional-review contradictions and exercise existing verifier negative mode before changes. Positive: both-shell doc/setup checks, skill metadata/links, read-only and acceptance scenario evaluation, Codex final review. Runtime suites do not apply. |
| `P1-07` / tooling | TE-01/10; owner-requested workflow revision; P1-06 | Replace the prospective Antigravity implementation role with a Codex Implementer role while retaining an independent Codex Reviewer as the sole acceptance/Done authority. Require an exact review packet and ready-to-run reviewer prompt after implementation or each fix round. Harden `.gitignore` for local secrets, private HTTP environments, user-specific publish settings, package/test artifacts and SQL backup/dump files without ignoring tracked shared agent, CI or development configuration. Exclusive paths and checks are fixed in `docs/worklogs/P1-07-completion.md`. Preserve P1-06 and older evidence as history; apply migration rules and Lean TDD gates/evidence reuse under AC-08/09 in the task log. | Negative: same-session self-acceptance, missing artifact identity/check results, stale Antigravity-as-current-implementer policy, broad ignore rules that hide required shared configuration, and tracked-secret assumptions must fail verification. Positive: canonical/mirror policy, plans, prompts, template and skills agree; independent review handoff is complete; representative local files are ignored while required shared files remain visible; documentation/setup checks pass. Runtime suites do not apply. |

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-10` / 1.5d | US-01, CN01-CN03; P1-01, P1-02, P2-10 | Implement login/refresh/logout and forced password change application flow with hashed refresh tokens. Validate the JWT role snapshot against authoritative `User.role_code` on every request and fail closed on mismatch. Output: Auth DTOs, service interfaces/implementation, endpoints, error codes. | Negative: null/empty credentials, suspended/pending account, wrong password, expired/revoked/replayed refresh token, stale role claim after `UserRoleChanged`. Positive: login→refresh→logout; old token unusable; current role/session is accepted. |
| `P1-11` / 1d | US-01, CN02, CN10; P1-10, P2-10 | Implement profile update and Admin password reset/session revocation. Output: profile/reset commands, endpoints and audit events. | Negative: self-role change, reset suspended user, invalid fields, old session after reset. Positive: allowed profile fields update; reset forces password change and audit contains no secret. |
| `P1-12` / 1d | US-01, US-02, CN03, CN05, TE-03; P1-10, P2-11 | Add current-user/project authorization service and endpoint policies. For non-Supervisor operations, enforce active/effective membership and authoritative `ProjectMember.role_code`; Supervisor bypass requires current server-side `User.role_code`. Output: authorization handlers/policies and reusable project-scope guard. Define authorized work-package reads for FE offline preparation; CN06 local drafts and CN09 local deletion remain FE-owned. | Negative: unauthenticated 401, wrong role/project 403, mismatched `ProjectMember.role_code`, ended/expired membership, forged project claim. Positive: assigned member and current Supervisor paths; membership change applies on the next request; no protected query runs before scope check where measurable. |

## Wave 2 — project and survey lifecycle

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-20` / 2d | US-03, DA01, DA03-DA05; P1-12, P2-20, P2-21 | Implement Project/ProjectMember/Warranty domain rules and create/update/assign/reassign PM use cases. Output: aggregate methods, services, DTOs, endpoints. | Negative: missing handover data, invalid warranty dates, second active primary PM, outside-project caller, stale reassignment. Positive: create project and atomically reassign primary PM with audit. |
| `P1-21` / 1.5d | US-03, DA02; P1-20, P2-21 | Implement RoadSection version creation; project-close policy is extracted to P1-24 after open-work schemas exist. Output: versioning service, commands/queries and endpoints. | Negative: malformed geometry/SRID, in-place edit of referenced version, stale write. Positive: geometry change creates version N+1; old data remains anchored/queryable. |
| `P1-22` / 1.5d | US-04, DA06-DA09; P1-21, P2-22 | Implement survey plan/request creation and postpone rules. Output: domain transitions, services, DTOs/endpoints. | Negative: empty scope, wrong road version, invalid dates, unauthorized PM, invalid/postponed transition. Positive: baseline and periodic plan/request creation with reasoned postpone and audit. |
| `P1-23` / 1.5d | US-05, KS01-KS04, KS14; P1-22, P2-23, P2-30 | Implement assign/accept/reject/reassign/cancel survey transitions. Output: explicit transition methods and API commands. | Negative: reject after accept, cancel after server confirmation, missing reason, duplicate active assignment, stale version. Positive: assign→reject→reassign→accept and allowed cancel paths. |

## Wave 3 — upload, processing, and AI review

Project closure is a late slice even though its task ID groups it with projects:

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-24` / 0.5d | US-03, DA12; P1-42, P1-53, P2-53 | Complete project-close service/API policy once survey, defect, inspection and repair work can all be queried. Extracted from P1-21 to keep road versioning independent of late workflow schemas. | Negative: each category of open work, wrong actor/project, stale version and duplicate conflicting close; positive: eligible close preserves history, appends audit and replays idempotently. |

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-30` / 2d | US-02, US-06, US-07, KS05-KS12, CN07-CN09; P1-23, P2-30, P2-31 | Implement upload-session/chunk/complete/status contracts and supplementary-survey request/approval/submission orchestration for KS11-KS12. Reuse File/storage primitives from P2-04. Upload completion enqueues backend validation; only the worker may confirm the immutable dataset manifest after checksum, completeness and server quality checks. Output: upload and SupplementarySurveyRequest DTOs/services/endpoints plus FE acknowledgement contract. | Negative: empty/oversize/traversal input, changed chunk or manifest under the same retry key, checksum failure, missing file, unauthorized/reassigned caller, concurrent complete, timeout, invalid supplementary transition and overwriting old data. Positive: resume after lost response; replay returns the same upload/version; status remains pending until worker confirmation; supplementary round N+1 preserves all original files. |
| `P1-31` / 1.5d | US-07, KS10-KS13; P1-30, P2-31, P2-32 | Implement processing state machine and transport-neutral AI adapter contract with deterministic fake. Job request identifies project, RoadSectionVersion, SurveyDataVersion, model version, manifest and correlation/idempotency IDs; result preserves source/model provenance and schema version. Output: job policy, adapter request/result contracts and fake adapter; no Python/GPU/model development. | Negative: malformed/off-scope or wrong-version results, retry of permanent data failure, duplicate job/result, timeout/cancellation and late result from an expired lease. Positive: queued-to-completed through fake adapter, retryable failure recovery and contract tests reusable by a later real adapter without duplicate detection output. |
| `P1-32` / 1.5d | US-08, US-20, AI01, AI04-AI07, AI13; P1-31, P2-32 | Implement PM keep/edit/reject detection flow; retained detection creates `OPEN` defect plus required inspection task. Output: review commands/queries, transition policy, endpoints. | Negative: non-PM/outside project, already reviewed detection, invalid edit, direct VERIFIED state, missing reason. Positive: keep creates exactly one defect/task; reject retains immutable AI output/history. |

## Wave 4 — field verification and baseline

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-40` / 1.5d | US-02, US-20, AI13, TN01-TN04, TN12, CN07-CN09; P1-32, P2-40 | Implement inspection assignment/accept/reject/submit commands and idempotent offline client IDs. Output: task/session DTOs, services and endpoints. Use GroundTruthMeasurement.evidence_file_id from the Data Dictionary; do not invent an Evidence-to-measurement FK. Recheck current assignment before accepting or replaying synchronized work. | Negative: unassigned crew, reject after accept, malformed/off-scope measurement, duplicate client ID with different payload. Positive: assignment→accept→submit; same retry returns same result. |
| `P1-41` / 1.5d | US-20, TN05-TN06; P1-40, P2-41 | Implement PM review and explicit `OPEN -> VERIFIED/REJECTED` policy; supplements append records. Output: review service/transitions/endpoints. | Negative: task incomplete, no submitted measurement, wrong PM/project, in-place edit, stale review. Positive: confirm and no-defect decisions update state and append audit/verification log. |
| `P1-42` / 1d | US-04, DA10-DA11; P1-41, P2-42 | Implement baseline confirmation cross-aggregate policy and status query. Output: confirmation policy/service/API. | Negative: unconfirmed file, incomplete job, unreviewed detection/open task/defect. Positive: eligible survey confirms baseline once; replay is idempotent. |

## Wave 5 — repair lifecycle

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-50` / 2d | US-11, SC01-SC04; P1-41, P2-50 | Implement repair eligibility, draft batch/version/items, server-calculated estimated total and submit. Output: aggregate policies, DTOs/services/endpoints. | Negative: non-VERIFIED defect, incomplete inspection, duplicate active repair, negative/overflow cost, stale version. Positive: eligible defects create draft; total is deterministic; submit locks version. |
| `P1-51` / 1.5d | US-11, SC05-SC09/SC12; P1-50, P2-51 | Implement Supervisor approve/return/reject and PM new-version/resubmit. Output: approval/version transitions and history API. | Negative: self/incorrect role approval, missing reason, mutate approved version, stale decision. Positive: return whole batch, create N+1, preserve N, resubmit/approve. |
| `P1-52` / 1d | US-12, SC10-SC11; P1-51, P2-52 | Implement assignment/reassignment only from current approved version. Output: assignment commands and endpoints. | Negative: draft/old approved version, duplicate active assignment, crew outside scope. Positive: assign and reassign with immutable history/audit. |
| `P1-53` / 2d | US-02, US-13, US-14, HT01-HT15, CN07-CN09; P1-52, P2-53 | Implement crew progress/report, PM inspection/rework and Supervisor final confirmation. Output: per-item state machine, APIs and completion policy. Explicitly include accept/reject-before-accept, internal crew work notes without extra accounts, unplanned-defect reporting, scoped history HT14, and retry-safe evidence/progress synchronization. | Negative: missing/unconfirmed evidence, item outside batch, overwrite append-only progress, close with failed/pending item. Positive: submit→inspect→rework/resubmit; passed item does not regress; all-pass closes batch. |

## Wave 6 — query, export, retention, research

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P1-60` / 2d | US-09, US-15, AI08-AI12, BC01-BC05; P1-53, P2-60 | Implement defect match/merge decisions and authorized dashboard/drill-down contracts. Output: query services/DTOs/endpoints. Review separately: match/merge decisions with audit and concurrency, then dashboard calculations/drill-down. Parent task is Done only after both slices pass. | Negative: cross-project access, incompatible periods, double-counted versions, invalid filters. Positive: each metric drills to source; pending/approved/actual costs remain separate. |
| `P1-61` / 1.5d | US-16, BC06-BC10; P1-60, P2-61 | Implement export request/status/download authorization and provenance manifest contract. Output: export services/endpoints/manifest DTO. | Negative: oversized scope, outside-project download, unsupported format, duplicate key mismatch. Positive: idempotent request and completed manifest includes filters, versions and checksums. |
| `P1-62` / 2d | US-19, QT11-QT14; P1-61, P2-62 | Implement retention request/review policy, legal-hold gate and deletion dry run. Output: policy/services/endpoints. | Negative: active legal hold, premature retention, scope drift, requester approves own request if prohibited, stale decision. Positive: request→approve→dry-run with exact scope and audit. |
| `P1-63` / 2d | RS01-RS06; P1-12, P2-63 | Implement backend research import, identity-based pairing, reproducible error calculation and report API using imported or clearly labeled synthetic data; do not implement an AI/DSM pipeline or require a field campaign to test software. Calculate bias, MAE, RMSE and included sample count by measurement type; preserve exclusions, source versions, provenance and uncertainty value/method when supplied or calculated by an approved method. Keep RESEARCH_VALIDATION isolated from operational Defect/Warranty transitions. | Negative: wrong project, ambiguous/missing pair, duplicate import key with changed payload, incompatible units/type, all samples excluded, immutable-run mutation and missing uncertainty method when a value is supplied. Positive: controlled paired fixture gives independently calculated metrics, reproducible export and zero operational writes; unknown uncertainty stays explicitly unavailable, not zero or RMSE relabeled as uncertainty. |
| `P1-64` / 2d | US-01, US-04, US-17, CN04, DA07, QT01-QT05; P1-12, P1-53, P2-64 | Implement Admin account suspend/reactivate and global-role change with open-work handover list, notification inbox, defect catalog/severity-rule versioning and reminder-rule APIs. Global-role change emits `UserRoleChanged`, audits before/after state, and atomically revokes active sessions/refresh tokens. Output: admin/notification/config services, DTOs and endpoints. Separate review checkpoints for account/security handover, inbox/reminders, and catalog/rule administration; reuse early schemas from P2-05/P2-07. | Negative: non-Admin, suspend already suspended user, self-escalation, stale role token remains usable, partial role-change/session-revocation transaction, mutate active rule version, reminder auto-creates survey, sync draft transfer. Positive: suspend/role change revokes sessions, preserves history/drafts, lists reassignment work and publishes versioned rules/reminders. |
| `P1-65` / 1.5d | US-10, US-18, AI14, QT06-QT10; P1-31, P1-41, P2-65 | Implement AI model/job/device administration plus training-label approval/export workflow. Output: admin policies, services, DTOs/endpoints and versioned export contract. Backend metadata, job administration, device registry and approved-label export are in scope; training, GPU inference and real model evaluation are external. Synthetic mock model versions must be identifiable in API/report provenance. | Negative: activate invalid model, retry data failure, unauthorized device/job access, export unapproved labels, mutate approved label. Positive: activate one model version, inspect/retry eligible job, register device, approve labels and create reproducible dataset export. |

## Person 1 release obligations

### Execution sequence and acceptance checkpoints

The existing day sizes above are historical estimates. New schema prerequisite tasks redistribute work; do not add old and extracted estimates as if both were new work. Re-estimate remaining slices after the first measured handoff. No business task is marked Done by this documentation correction.

| Order | Person 1 task(s) | Start gate / concrete acceptance output |
|---|---|---|
| 1 | P1-10, P1-11 | P2-10 Done: login/refresh/logout/reset/profile with revoked/stale credentials rejected; publish authentication examples for FE. |
| 2 | P1-12 | P1-10 and P2-11 Done: project/resource authorization and scoped offline-preparation reads. |
| 3 | P1-20, P1-21 | P2-20/P2-21 Done and ADR 003 D-01 resolved: project/PM handover, warranty and road versions; publish first usable business API slice. |
| 4 | P1-22, P1-23 | Paired persistence Done: plan/request/assignment/rejection/reassignment/cancellation; preserve assignment history. |
| 5 | P1-30, P1-31, P1-32 | Paired persistence Done: resume upload, supplementary request, worker confirmation, mock processing and PM preliminary review. |
| 6 | P1-40, P1-41, P1-42 | Paired persistence Done: field measurement/review and baseline prerequisites; exercise forbidden/stale/retry paths. |
| 7 | P1-63 | Pull forward immediately after P2-63 and P1-12 Done; do not wait for dashboard or real AI. Review import, pairing/metrics, then publication/export as separate checkpoints. D-02 applies only to the unresolved uncertainty slice. |
| 8 | P1-50, P1-51, P1-52, P1-53 | Paired persistence Done: eligible repair draft -> submission/approval -> assignment -> evidence/rework -> final confirmation/history. |
| 8a | P1-24 | P1-42/P1-53/P2-53 Done: project closure reads all open-work categories and preserves history. |
| 9 | P1-60, P1-61, P1-62 | Paired persistence Done: match/merge and dashboards, export/download authorization, retention/legal hold. |
| 10 | P1-64, P1-65 | Start as soon as declared dependencies are Done; admin/inbox/reminder and model/job/device/label contracts. D-03 gates reminder-version implementation. |

Within every task, execute one AC slice at a time:

1. State actor, current membership/resource scope, preconditions, allowed transition, stable error code and audit event in that task's worklog; declare exact file ownership.
2. Add negative/edge tests and observe the intended RED; add positive contract/state tests next.
3. Implement the DTO/service/domain/API slice against the completed schema handoff. Build once, then choose either focused tests or the affected-project suite from the known impact; do not run both as a routine progression.
4. Review authorization, transitions, immutability, retries, concurrency and audit. Record file diff and actual command results; only then mark the parent Done when all its slices pass.
5. Provide FE with OpenAPI/request-response examples, conflict/retry semantics, seed identities and confirmation/status behavior. Android implementation remains external.

### Backend coverage map

This map assigns server responsibilities; it does not claim that FE-only acceptance criteria have been implemented by the backend.

| Source | Backend task owner(s) | Acceptance evidence |
|---|---|---|
| US-01 / CN01-CN04, CN10 | P1-10, P1-11, P1-12, P1-64 | Auth/profile/reset/scope and inbox; current role/membership and revocation tests. |
| US-02 / CN05-CN09 | P1-12, P1-30, P1-40, P1-53; P2-02 | Authorized downloads; resumable/retry-safe uploads and writes; acknowledgement before FE cleanup. CN06 local drafts and local queue/UI belong to FE. |
| US-03 / DA01-DA05, DA12 | P1-20, P1-21, P1-24 | Project/PM/warranty/road versions; closing a project must account for all later open-work aggregates in P1-24 and the final release gate. |
| US-04 / DA06-DA11 | P1-22, P1-42, P1-64 | Plan/postpone/reminders and cross-aggregate baseline confirmation. |
| US-05 / KS01-KS04, KS14 | P1-23 | Assignment/rejection/reassignment/cancellation state and audit. |
| US-06 / KS05-KS10 | P1-30, P1-31 | Flight/file metadata, required-file validation, worker integrity confirmation and admitted processing. |
| US-07 / KS11-KS13 | P1-30, P1-31 | Independent supplementary rounds and retry classification without source overwrite. |
| US-08 / AI01, AI04-AI07 | P1-32 | Keep/adjust/reject history; retained detection creates OPEN defect and task. |
| US-09 / AI08-AI12 | P1-60 | Scoped audited merge/match decisions and period comparisons. |
| US-10 / AI14 | P1-65 | Approved-label/dataset provenance and export, no model training. |
| US-11 / SC01-SC09, SC12 | P1-50, P1-51 | VERIFIED eligibility, immutable approval snapshots, totals and resubmission. |
| US-12 / SC10-SC11 | P1-52 | Assignment uses only current approved version; history preserved. |
| US-13 / HT01-HT08, HT14-HT15 | P1-53 | Accept/reject, crew notes, progress/evidence, unplanned report and scoped history. |
| US-14 / HT09-HT13 | P1-53 | Per-item inspection/rework and Supervisor final confirmation. |
| US-15 / BC01-BC05 | P1-60 | Golden-seed metrics drill to source without duplicate-version counts. |
| US-16 / BC06-BC10 | P1-61 | Export scope, status, manifest/checksum and authorized download. |
| US-17 / QT01-QT05 | P1-64 | Account administration, revocation/handover and versioned rules/reminders. |
| US-18 / QT06-QT10 | P1-65 | Backend model/job/device metadata and eligible retries using the mock adapter. |
| US-19 / QT11-QT14 | P1-62 | Approved immutable deletion scope and execution-time legal-hold protection. |
| US-20 / AI13, TN01-TN06, TN12 | P1-32, P1-40, P1-41 | Required field task, assignment, immutable measurement and PM verification. |
| RS01-RS06 | P1-63, P2-63 | Backend import/pair/metrics/provenance/export tests with controlled data; field collection and real AI validation are external. |

### Two-week operating cadence

Assumption for capacity assessment: two backend owners, ten working days; actual availability must be recorded at kickoff. This is a target and review cadence, not a promise that the original 72 person-days fit twenty person-days. AI was already mocked in the original estimates.

| Checkpoint | Required output / action |
|---|---|
| Day 1 | Close documentation correction, review P2-01 and establish working SQL/CI prerequisites; resolve D-01 before road/version migration and record ownership handoffs. |
| Day 2 | Measure completed schema/API slices and actual review/test time; publish a remaining-work forecast for every backend coverage-map row. If full acceptance does not fit, escalate capacity/date explicitly; do not silently drop features. |
| Days 3-5 | Follow dependency order; integrate each completed API slice and hand its contract to FE. Day 5 records working workflows, failed gates and revised forecast. |
| Days 6-8 | Continue eligible slices, prioritize research immediately when measurement persistence is ready, and close core/admin/export/retention gaps identified by the coverage map. No prerequisite is waived to match a calendar box. |
| Days 9-10 | Reserve verification time: full API/SQL scenario, research golden dataset, format/build/security, FE contract checks and P2-67 backup/restore evidence. Full acceptance requires all backend rows green; otherwise report exact unfinished tasks. |

### Planned code ownership and executable checks

Person 1 implementation stays in `RoadGuardSystem.API`, `RoadGuardSystem.Services`, `RoadGuardSystem.DTOs`, `tests/RoadGuardSystem.ApiTests` and `tests/RoadGuardSystem.UnitTests`. Domain methods in `RoadGuardSystem.BusinessObjects` require the paired schema handoff. Declare concrete filenames in each task worklog before that implementation starts; this repository-wide plan does not invent all future method signatures.

For each approved endpoint slice, build the changed project and run its real request from `Http/*.http`. Add focused tests only for the risk categories in `AGENTS.md`. If shared API behavior affects several features, run the API-test project. Reuse passing evidence while its inputs and environment are unchanged; commit and handoff do not trigger reruns. Use the full solution only for integration/release or an explicit owner request. Documentation/tooling changes run only their relevant verifier.

```powershell
dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly
# Choose exactly one test command below.
dotnet test tests/RoadGuardSystem.ApiTests --filter "FullyQualifiedName~<Feature>" -v q
dotnet test tests/RoadGuardSystem.ApiTests -v q # shared API behavior only
dotnet test RoadGuardSystem.slnx -v q # integration/release or owner request only
```

SQL-specific slices use a known SQL Server environment; skipped or zero-discovered required tests are not a pass. The endpoint contract records actor, expected response and persisted effect.

Before release, run the full seed scenario `project -> survey -> upload -> processing -> detection -> field measurement -> repair -> export` and the full relevant test set. Keep authorization/integrity checks, migrations and specification decisions explicit; this release gate does not force TDD or an independent Codex review.
