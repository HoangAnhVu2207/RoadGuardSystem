# RoadGuard execution plan — Person 2

Owner: Person 2 (data/integration/test/operations primary). Implementation and self-review: Codex Implementer for the task owner. Completion: Codex Reviewer in a separate task/session that did not author the artifacts. Baseline: 16/09/2026; workflow updated 19/09/2026.

This is one of exactly two execution plans. Person 2 takes only one unfinished assigned task at a time, including `In Progress`, `Ready for review`, `Changes requested` and `Blocked`. Every task follows `AGENTS.md` and the four Negative-First phases. Person 2 owns production-like SQL Server proof, retry/idempotency evidence, and delivery infrastructure.

## Backend delivery boundary — owner clarification, 2026-09-18

The two-week target covers backend MVP and backend Research Validation software. Android and Web Dashboard belong to FE; real AI training/inference/DSM generation and field data collection are external follow-ups. Backend processing uses a deterministic mock behind the existing adapter boundary, and Research Validation is testable with controlled imported/synthetic pairs. Real AI accuracy and field-trial conclusions are not prerequisites for backend software acceptance and are not claimed by mock tests. See [ADR 003](../docs/adr/003-backend-delivery-and-ai-boundary.md) for contracts, unresolved product decisions and research sources.

Task IDs remain stable. Wave headings group features; explicit dependencies, not row order, determine execution. New P2-04–P2-07 tasks extract prerequisites previously buried in P2-30/P2-64/P2-65; their ownership is exclusive. Only completed schema tasks hand entities to Person 1. Each parent task may have several acceptance checkpoints, but is Done only when every checkpoint passes.

## Task completion contract

For each task, Codex's assignment records acceptance criteria, In scope, Out of scope, dependencies, exclusive files and required checks. Codex Implementer reads traced specifications; writes negative tests and observes the expected failure; writes positive tests; implements; runs applicable checks; completes `Antigravity_Completion_Log_Template.md` and mandatory owner self-review; then submits `Ready for review`. Codex reviews the exact artifacts and records `Changes requested`, `Blocked` or `Done`. Only Codex may mark `Done`, after every applicable task gate and mandatory finding is verified. Explicit report-only reviews do not modify status/logs. See [shared prompts](../docs/prompts/RoadGuard_Task_Workflow.md); Git permissions remain separate. This policy applies to new/reopened work from the P1-06 workflow migration onward; historical Done evidence is preserved.

Apply P1-07 migration and Lean TDD in [AGENTS.md](../AGENTS.md): narrow tests, affected checks, submission once per content/environment state and independent review. Preserve historical evidence and Ready for review artifacts; new roles apply to the next implementation/fix round. Only a separate reviewer task may accept.

## Current status and branch synchronization gate

| Task | Status | Branch | Note |
|---|---|---|---|
| `P2-00` | `Done` | `huy` | Product Owner-confirmed completion; latest observed local tip is `b2662fe`. |
| `P2-01` | `Done` | `huy` | Codex acceptance round 2 recorded 2026-09-18 for artifact `77505f2`; hosted CI run `35277820417` and fresh checkout gates passed, with no open findings or blockers. |
| `P2-02` | `Done` | `huy` | Codex acceptance round 3 recorded 2026-09-18 under the owner's task-specific self-review authorization; F-01/F-02 Verified. Accepted fix committed as `e2454e6` and fast-forwarded into local `develop`; fresh integration gate passed 163/163 with zero skips. Push pending correct remote/access information; no acceptance blocker or next-task assignment. |
| `P2-03` | `Done` | `huy` | Backend delivery-plan/documentation correction self-reviewed and verified locally; see P2-03 completion log. No P2-01 sign-off. |
| `P2-08` | `Done` | `huy` | Codex acceptance recorded 2026-09-18 for submitted commit `df692ff`; documentation, planning, tooling, Compose positive/negative, reference/secret and P2-00 SQL gates passed with no open findings. Runtime installation on Huy's machine remains an external execution step, not an acceptance blocker. |

Prior gate: owner-approved integration produced baseline `20ff1d3`, containing Person 1 tip `3e13ca6` and Person 2 tip `b2662fe`. The synchronization gate is historical; the current table above determines task status.

## Exclusive ownership and conflict control

- Person 2 has exclusive file ownership of Repositories, migrations, SQL integration tests, Docker/Compose, CI workflows, seed infrastructure, and operations files while a Person 2 task is active.
- Person 2 establishes entity/property/enum shape and persistence first. Only after Codex Implementer self-review and Codex acceptance mark that task `Done` may Person 1 begin the paired domain/API task and add domain methods/invariants. The dependency artifacts must also be present in the current checkout.
- Person 2 must not edit API, Services, DTOs, unit tests, or API tests owned by an active Person 1 task. Record a `Conflict warning` and hand the required contract change to Person 1 instead.
- Shared hotspots require a single declared owner. Every task log must list intended files before coding and record any overlap, sequencing constraint, or schema reopening.

Default commands once Sprint 0 exists:

```powershell
dotnet test tests/RoadGuardSystem.IntegrationTests --filter "TaskId=<TASK-ID>"
dotnet test tests/RoadGuardSystem.ApiTests --filter "TaskId=<TASK-ID>"
dotnet format --verify-no-changes
dotnet build --no-restore
dotnet test --no-build
```

## Wave 0 — data and CI foundation

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-03` / 0.5d | Planning; US-02, RS01-RS06; P1-03 | Correct both existing plans, clarify backend-only acceptance and later AI integration, repair documentation status/dependency checks, and record evidence in ADR 003 and a completion log. | Negative: historical status cannot replace the current table; missing/duplicate status, dependency cycles and missing ownership dependencies fail. Positive: valid progression and documentation contracts pass in PowerShell 7 and 5. |
| `P2-00` / 1.5d | TE-01/09, P1-00 project names | Configure EF Core SQL Server + NetTopologySuite, `DbContext`, configuration assembly and SQL Server integration-test fixture. Output: repository project, test fixture, initial connectivity test. | Negative: missing/invalid connection configuration fails fast; unsupported SRID mapping test fails. Positive: create/drop isolated test DB and round-trip geography/geometry. |
| `P2-01` / 1d | TE-01/10, P1-00 | Add local Docker Compose dependencies, seed framework and CI restore/format/build/test/coverage pipeline. Output: compose/config templates, CI YAML, seed entry point. | Negative: CI sample/failing test proves gate blocks; secrets absent; unhealthy DB fails readiness. Positive: clean checkout command sequence and deterministic seed pass. |
| `P2-02` / 1d | TE-05, TE-07; P2-00, P2-01, P2-03 | Add audit/outbox/idempotency/concurrency primitives and transaction conventions. Output: base mappings/interceptors or explicit services, interfaces, integration tests. Define key scope (actor/project/operation), request fingerprint, stored outcome and race behavior; changed payload under the same key conflicts. Domain write, audit and outbox commit atomically; worker delivery remains at-least-once with deduplicated effects. | Negative: duplicate key, stale row version, transaction rollback, sensitive-value redaction. Positive: atomic domain change+outbox/audit; replay returns prior outcome. |
| `P2-08` / 0.25d | TE-01/09/10; P2-00, P2-01 | Publish a Windows local setup guide for Huy covering native SQL Server, Docker Compose and automatic Testcontainers execution without changing accepted infrastructure artifacts. Output: `docs/setup/sql-server-docker-local-setup.md` plus task evidence. | Negative review: no real credential, machine-specific server name, tracked `.env`, remote shared-database requirement or destructive volume command. Positive: commands and variable names match the current repository; Compose verification and documentation consistency checks pass. Runtime installation on Huy's machine remains a separate execution step. |

## Early schema prerequisites — execute after P2-10, before their consumers

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-04` / 1d | US-03, US-06, DA01, KS09; P2-10 | Establish File metadata, immutable content/storage boundary and local test store before handover/measurement/repair FKs; provide streamed checksum/storage primitives. Public upload-session APIs remain P1-30. | Negative: path traversal, oversize stream, mismatched checksum, changed retry and missing owner; positive: immutable file round-trip and SQL mapping. |
| `P2-05` / 1d | US-08, US-17, AI04-AI06, QT03-QT04; P2-10 | Establish DefectType, CauseCategory and SeverityRuleVersion schemas/fixtures before Defect and review history; schema-level JSON and immutable rule-version checks. Admin APIs remain P1-64. | Negative: duplicate catalog key/version, invalid JSON and referenced catalog deletion; positive: versioned catalog references round-trip. Thresholds in fixtures are not approved engineering standards. |
| `P2-06` / 0.5d | US-06, US-18, KS05, QT10; P2-10 | Establish DroneDevice schema/seed before Flight.drone_device_id; retain registry administration APIs in P1-65. | Negative: duplicate serial and invalid status; positive: valid registry record can be referenced by later Flight migration without drone control. |
| `P2-07` / 0.5d | US-01, CN04; P2-10 | Establish Notification schema and deduplicated outbox-consumer persistence boundary before assignment workflows. Notification inbox/API and reminder generation remain P1-64/P2-64. | Negative: duplicate source/recipient event, missing recipient, sensitive payload; positive: retry commits one notification effect. |

## Wave 1 — identity storage and authorization proof

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-10` / 1.5d | US-01, CN01-CN03, CN10; P2-02 | Map/migrate User, Role, Session, RefreshToken, PasswordResetLog and AccountStatusChangeLog; map nullable `Session.device_metadata_json` as `nvarchar(max)` with `ISJSON`, application schema validation and write-once behavior; support atomic `UserRoleChanged` persistence/audit plus active-session/token revocation; seed four roles. Output: entities/configurations/migration/seed plus migration downgrade/recovery note. | Negative: duplicate username/token hash, invalid status/log transition, role change leaving an active credential, malformed/non-object/unknown-property metadata JSON, metadata containing secrets, metadata update after session creation, plaintext token/password scan. Positive: session issuance preserves `issued_at`, valid schema-v1 metadata round-trips on SQL Server, role change/session cascade and append-only logs persist atomically. |
| `P2-11` / 1d | US-01, US-02, CN03, CN05, TE-03; P2-10, P2-20 | Map the project-membership authorization read model using authoritative `ProjectMember.role_code` and add SQL integration fixtures only. Output: active/effective membership query plus reusable users/memberships for four roles. API policies, tokens, HTTP 401/403 behavior, and API tests remain exclusively in P1-12. | Negative: mismatched role, ended/expired membership, and cross-project IDs return no rows. Positive: each role query sees only assigned scope and membership changes are visible immediately. |

## Wave 2 — project and survey persistence

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-20` / 1.5d | US-03, DA01, DA03-DA05; P2-02, P2-10, P2-04 | Map/migrate Project, ProjectMember and HandoverDocument; filtered uniqueness for active primary PM and concurrency token. File schema is supplied by P2-04. Warranty schema belongs to P2-21 because its optional RoadSection FK requires that table. Output: complete project/membership/handover mapping and SQL fixtures for P2-11. | Negative: second active primary PM, invalid user/project/file FK, invalid dates, stale handover and hard-delete of referenced history. Positive: atomic PM handover and scoped membership query fixture; application exactly-one-primary rule remains P1-20. |
| `P2-21` / 1.5d | US-03, DA02-DA05, DA12; P2-20 | Map/migrate RoadSection/RoadSectionVersion and Warranty, including Warranty.road_section_id and source-document FKs; project UTM SRID rules and immutable version indexes. Document SQL insertion strategy for the RoadSection/current_version circular reference before migration. Output: complete schema handoff to P1-20/P1-21. | Negative: wrong SRID/zone, null geometry, duplicate version, mutation of referenced version, invalid warranty dates/FKs and cross-project road scope. Positive: N/N+1 coexist, old references remain valid, initial road/version creation is atomic and multiple warranties persist. |
| `P2-22` / 1d | US-04, DA06-DA09; P2-21 | Map/migrate SurveyPlan, SurveyRequest and postponement history with constraints/indexes. | Negative: invalid dates/status/version FK and duplicate active plan/request where prohibited. Positive: create and append postpone reason/history. |
| `P2-23` / 1d | US-05, KS01-KS04, KS14; P2-22, P2-07 | Map/migrate Survey/assignment/history; unique active assignment and audit/outbox events. | Negative: duplicate active assignment, invalid status byte, cancel transaction rollback. Positive: reassignment preserves old assignment and emits notification once. |

## Wave 3 — files, workers, and AI persistence

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-30` / 2d | US-02, US-06, US-07, KS05-KS12, CN07-CN09; P2-23, P2-04, P2-06 | Reuse P2-04 File/storage boundary; map Flight, SurveyFile, SurveyDataVersion, QualityCheck and independent SupplementarySurveyRequest with round uniqueness, JSON validation, exactly-one-target and checksum constraints. Persist resumable upload metadata and backend-validation work atomically. Upload HTTP contracts and confirmation policy remain P1-30. | Negative: traversal/oversize storage writes, changed retry payload/chunk, duplicate supplementary round, invalid checksum or QualityCheck stage/target, and false confirmation before quality checks. Positive: streamed resume/read, persisted confirmation status and supplementary N+1 without overwriting source files. |
| `P2-31` / 2d | US-07/18, KS10-KS13/QT06-QT10; P2-30 | Map ProcessingBlock/Job/Attempt/AIModelVersion and implement generic outbox/lease/retry worker infrastructure. Processing state rules, AI adapter contracts, and failure classification remain in P1-31. | Negative: duplicate delivery, lease race, infrastructure retry exhaustion, and DB/storage timeout. Positive: one leased execution under replay with persisted attempt count and no duplicate outbox side effect. |
| `P2-32` / 1.5d | US-08, US-20, AI01, AI04-AI07, AI13; P2-31, P2-05 | Map immutable AIDetection, Defect, verification history, and FieldInspectionTask; provide constraints and repository transaction support. Detection review decisions and the create-defect/task orchestration remain in P1-32. Establish DefectVerificationLog shape here for preliminary review; P2-41 adds measured-review constraints/transactions without remapping or mutating shared history. | Negative: mutate raw AI payload, duplicate retained-detection key, orphan persistence, and invalid status value. Positive: repository transaction persists the detection/defect/task/outbox set atomically when invoked by the service. |

## Wave 4 — measurements and baseline persistence

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-40` / 2d | US-02, US-20, AI13, TN01-TN04, TN12, CN07-CN09; P2-32 | Map FieldInspectionAssignment/Session and GroundTruthMeasurement with purpose gates, immutable submission, units, offline client uniqueness and direct evidence_file_id references to File. General polymorphic Evidence is mapped in P2-53 after RepairItem exists. Output: product/research-compatible measurement schema handoff; no additional target columns. | Negative: purpose/task mismatch, wrong assignment/project, duplicate client ID with changed payload, invalid unit/value/SRID, mutation after submission and unconfirmed evidence file. Positive: append session/measurement and retry returns prior result; research purpose neither creates nor changes a Defect. |
| `P2-41` / 1d | US-20, TN05-TN06; P2-40 | Extend existing DefectVerificationLog persistence from P2-32 with submitted-measurement review constraints, concurrency and audit transaction tests. Domain decision policy remains P1-41; no duplicate entity mapping or new evidence target. | Negative: verification without completed task/measurement, duplicate decision, stale transaction. Positive: decision+defect state+audit commit atomically; rollback leaves all unchanged. |
| `P2-42` / 1d | US-04, DA10-DA11; P2-41 | Add baseline eligibility query/indexes and SQL integration fixtures. | Negative: each missing prerequisite independently blocks confirmation. Positive: eligible baseline query remains project/version scoped and confirmation is concurrency-safe. |

## Wave 5 — repair persistence and evidence

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-50` / 1.5d | US-11, SC01-SC04; P2-41 | Map RepairBatch/Version/Item with immutable version snapshot, money precision and uniqueness against active repairs. | Negative: duplicate version/item/active repair, arithmetic overflow/rounding mismatch, mutate submitted version. Positive: server total and version snapshot round-trip exactly. |
| `P2-51` / 1d | US-11, SC05-SC09/SC12; P2-50 | Map approval decisions and current-version pointer with concurrency/transaction constraints. | Negative: stale simultaneous decisions, approval without items, change approved snapshot. Positive: return and N+1 submission preserve complete history. |
| `P2-52` / 1d | US-12, SC10-SC11; P2-51 | Map RepairAssignment and unique active assignment/outbox notification. | Negative: assignment to non-current/unapproved version and duplicate delivery. Positive: reassign ends prior record and emits exactly one notification. |
| `P2-53` / 2d | US-02, US-13, US-14, HT01-HT15, CN07-CN09; P2-52, P2-04, P2-40 | Map append-only RepairProgress/RepairEvidence/RepairInspectionResult/UnplannedDefectReport and general Evidence using exactly the Data Dictionary targets (Defect, RepairItem, HandoverDocument); implement server integrity queries and scoped history. Measurement evidence continues to reference File directly. | Negative: update/delete submitted evidence, missing before/after, cross-item/project evidence, zero/multiple targets, unconfirmed file and unplanned defect auto-added to an approved version. Positive: append progress/rework, scoped history HT14, independent item outcomes and retry without duplicate progress. |

## Wave 6 — read models, export, retention, research and release

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-60` / 2d | US-09, US-15, AI08-AI12, BC01-BC05; P2-53 | Map DefectMergeDecision and DefectMatch with audited transaction support before building scoped dashboard/read-model queries, indexes and representative performance seed. Output: mapping/migration, pair/decision uniqueness and read-model fixtures; merge decision policy remains P1-60. | Negative: self/cross-project match, duplicate decision or pair, stale merge, double-counted version snapshots and incompatible periods. Positive: matching decisions persist reproducibly, expected metrics drill to source IDs and query timing is recorded against the agreed dataset. |
| `P2-61` / 2d | US-16, BC06-BC10; P2-60 | Map ReportExport and implement background PDF/ZIP/CSV generation/storage with checksums and provenance manifest. Export request/status/download authorization and API contracts remain in P1-61. | Negative: worker replay, partial file, storage timeout, duplicate job key, and manifest checksum mismatch. Positive: deterministic manifest, completion/failure recovery, and idempotent file generation. |
| `P2-62` / 2d | US-19, QT11-QT14; P2-61 | Map retention/legal-hold/deletion log and implement a deletion executor that consumes an already approved immutable scope and rechecks legal hold at execution. Request/review policy, dry run, and APIs remain in P1-62. | Negative: active/racing legal hold, partial delete, immutable-scope mismatch, and executor replay. Positive: exact approved scope is applied and the append-only deletion log records the result. |
| `P2-63` / 2d | RS01-RS06; P2-40, P2-30 | Map DerivedMeasurement, MeasurementValidationRun and MeasurementValidationSample; reuse research-purpose sessions/ground truth from P2-40. Add import staging, provenance, immutable dataset/run snapshots and controlled paired fixtures without any AI service. Preserve uncertainty value/method and exclusion reasons. Arithmetic orchestration belongs to P1-63. | Negative: duplicate/ambiguous pair, missing source version, invalid JSON/unit, wrong project/purpose, mutation of a published run and missing method for supplied uncertainty. Positive: imported/synthetic paired data round-trips on SQL Server with stable provenance; P1-63 calculates known bias/MAE/RMSE and operational records remain unchanged. |
| `P2-64` / 1.5d | US-01, US-04, US-17, CN04, DA07, QT01-QT05; P2-10, P2-05, P2-07, P2-53 | Reuse early Notification and catalog/severity schemas; add reminder configuration persistence, account-handover queries and persistence/outbox handlers. Review account revocation/handover, notification delivery and configuration changes separately. Admin/inbox/configuration/handover orchestration APIs remain P1-64. | Negative: loss of open-work records, duplicate reminder, incomplete revocation and history mutation. Positive: complete handover query, append-only audit, one notification effect per event and reproducible versioned configuration; reminder-version schema requires the decision gate in ADR 003. |
| `P2-65` / 1.5d | US-10, US-18, AI14, QT06-QT10; P2-31, P2-32, P2-06 | Reuse DroneDevice/AIModelVersion schemas; map TrainingLabelApproval and TrainingDatasetExport with provenance, activation uniqueness and job admin indexes. Output: backend persistence and contract fixtures; no training algorithm, Python service, inference runtime or GPU deployment. | Negative: multiple active model versions, duplicate label approval/export, missing source/model/checksum, device cross-scope leak. Positive: activation switch is atomic; approved-label export is reproducible and traceable. |
| `P2-67` / 1d | TE-10/release; P1-11, P1-24, P1-42, P1-62, P1-63, P1-64, P1-65, P2-42, P2-62, P2-63, P2-64, P2-65 | Create deterministic end-to-end seed, backup/restore rehearsal, security/config scan and release test script/runbook. Output: seed scenario, `docs/runbook.md`, CI release job/report. Release evidence includes backend Research Validation fixtures/import/export, every US-01–US-20 backend AC, FE contract handoff and adapter failure/retry tests. Full BE acceptance does not certify AI accuracy or Android UX. | Negative: missing secret/config, failed restore, oversized upload and unauthorized seeded user. Positive: clean DB→migrate→seed→full workflow→backup/restore→tests. |

## Person 2 release obligations

### Execution sequence and schema handoff

| Order | Person 2 task(s) | Acceptance / Person 1 handoff |
|---|---|---|
| 1 | Finish P2-01 review, then P2-02 | Reproduce real SQL/CI prerequisites; atomic audit/outbox/idempotency/concurrency contract. Documentation-only P2-03 does not approve P2-01. |
| 2 | P2-10 | Identity schema/seed/revocation -> P1-10/P1-11. If the audit actor FK needs User, add it through a new migration here; do not create a dependency cycle or rewrite P2-02 history. |
| 3 | P2-04, P2-20, P2-11 | File/storage primitives -> project/handover/membership -> authorization query -> P1-12. Each is a separate completed task before its consumer starts. |
| 4 | P2-21 | Resolve ADR 003 D-01 first; road/version and Warranty schema -> P1-20/P1-21. |
| 5 | P2-05, P2-06, P2-07 | Catalog/rule, device and notification prerequisites; these may be pulled earlier after P2-10, one task at a time. |
| 6 | P2-22, P2-23, P2-30 | Plan/request/survey/assignment, then file/dataset/quality/supplementary persistence -> P1-22/P1-23/P1-30. |
| 7 | P2-31, P2-32, P2-40 | Worker leases and mock processing persistence -> detection/review/task -> field sessions/measurements. |
| 8 | P2-63 | Pull forward immediately after P2-40; hand controlled research fixtures/schema to P1-63 before waiting for repair/dashboard. D-02 gates only unresolved uncertainty semantics. |
| 9 | P2-41, P2-42, P2-50, P2-51, P2-52, P2-53 | Verification/baseline, repair snapshots/approval/assignment and evidence; give each completed schema task to Person 1. |
| 10 | P2-60, P2-61, P2-62, P2-64, P2-65 | Match/read models, exports, retention, admin/reminder and label provenance; P2-65 may be pulled forward once its explicit dependencies are Done. |
| 11 | P2-67 | Run complete backend and research acceptance with Person 1 after all backend ACs pass; include restore/migration/backup rehearsal and recorded CI proof. |

No two active Person 2 tasks own the same files. Update `RoadGuardSystem.Repositories/RoadGuardDbContext.cs`, migration/model snapshots, solution/project/DI files and shared entity shape under one declared owner per task. New P2-04–P2-07 carve out prerequisites from older tasks; they are not additional independent product scope. Original estimates require re-estimation after this split.

For each migration: write failing SQL negative cases, positive round-trips, then mapping/migration; verify empty-DB and previous-migration upgrade plus recovery/downgrade notes. For each handoff: publish mapped entities/enum values, repository signatures, transaction/version behavior, seed IDs, relevant tests and the accepted commit. Domain policy remains Person 1-owned.

### Two-week delivery checkpoints

Follow the same day-1/day-2/day-5/day-10 cadence in [Person 1's plan](RoadGuard_Plan_Person_1.md). At day 2, report actual SQL/schema throughput and CI/environment delays before forecasting full acceptance. Preserve the entire backend coverage map, including Research Validation, US-02 server contracts, retention, notifications and model/label administration. Do not shorten estimates or remove required tests merely because real AI development is external.

All task sizes above are historical or provisional sizing. Before correction, the remaining workload was 72 person-days plus P2-01 review under a mock-AI baseline; extracted work must not be double-counted. Two owners over ten working days supply about twenty person-days before integration overhead. A two-week full-acceptance claim therefore needs measured evidence or an explicit capacity/date adjustment.

### Release evidence and inherited statuses

- `P2-00`: current plan records owner-confirmed Done; the historical worklog ends at Ready for cross-review and is not edited to invent a newer review.
- `P1-03`: its old synchronization warning is superseded for scheduling by baseline `20ff1d3`; this does not fabricate a new integration test run.
- `P2-01`: Codex accepted artifact `77505f2` in review round 2 after verifying the recorded live-container and hosted-CI gates plus fresh checkout checks. P2-03 did not supply or substitute for this acceptance.
- P2-67 requires complete backend US-01–US-20 coverage, research import/pair/error-metric/export proof, no operational research writes, adapter failure/retry coverage, and SQL backup/restore evidence. Data fixtures clearly identify synthetic provenance. No real-model accuracy or Android implementation is claimed.
- ADR 003 D-01/D-02/D-03 must be resolved for their affected acceptance slices before those slices can be Done. Unrelated work can continue.

Execute documented task-filtered tests, solution restore/non-incremental build/format/all-tests, the dependency-security gate and documentation verification. Record the actual SQL environment, skipped cases, immutable commit/artifact references and self-review result. Do not infer readiness solely from static YAML or prose checks.

Keep SQL Server integration tests deterministic and isolated; publish the exact Docker/SQL Server prerequisites in the runbook; verify all migrations from an empty database and from the previous migration; ensure CI exercises the same commands recorded in completion logs; complete Codex Implementer owner self-review and mandatory Codex acceptance and resolve every conflict warning. Release is blocked by EF InMemory-only evidence for SQL behavior, destructive retention without legal-hold race protection, duplicate worker side effects, or secrets in source/logs.

## Retired cross-review task IDs

`P2-12`, `P2-24`, `P2-33`, `P2-43`, `P2-54`, and `P2-66` remain retired and must not be started or reused. Codex Implementer self-review and mandatory Codex acceptance are stages of each existing implementation task, not separate Person 2 implementation tasks. Only Codex records final acceptance and marks `Done` under the current policy; historical review logs remain unchanged.
