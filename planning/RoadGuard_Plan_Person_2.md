# RoadGuard execution plan — Person 2

Owner: Person 2 (data/integration/test/operations primary). Self-review: task owner. Baseline: 16/09/2026.

This is one of exactly two execution plans. Person 2 takes only one `In Progress` task at a time. Every task follows `AGENTS.md` and the four Negative-First phases. Person 2 owns production-like SQL Server proof, retry/idempotency evidence, and delivery infrastructure.

## Task completion contract

For each task: read the traced specification sections; write negative tests first and observe the expected failure; write positive tests; implement; run the narrow tests, affected suite, format, and build; then complete `Antigravity_Completion_Log_Template.md` and the mandatory owner self-review. The owner may self-mark `Done` only when every task gate is green and all self-review findings are resolved.

## Current status and branch synchronization gate

| Task | Status | Branch | Note |
|---|---|---|---|
| `P2-00` | `Done` | `huy` | Product Owner-confirmed completion; latest observed local tip is `b2662fe`. |
| `P2-01` | `Ready for Codex review` | `huy` | Compose/config templates, CI YAML, seed framework and integration test. |
| `P2-02` | `Not started` | `huy` | Must not overlap `P2-01`; keep queued until `P2-01` is `Done`. |

Prior gate: `P2-01` was recorded as `P2-01` | `Not started - blocked by branch synchronization` until an owner-approved Git integration produced baseline `20ff1d3` containing both Person 1 Wave 0 tip `3e13ca6` and Person 2 `P2-00` tip `b2662fe`.

## Exclusive ownership and conflict control

- Person 2 has exclusive file ownership of Repositories, migrations, SQL integration tests, Docker/Compose, CI workflows, seed infrastructure, and operations files while a Person 2 task is active.
- Person 2 establishes entity/property/enum shape and persistence first. Only after that task is self-reviewed and `Done` may Person 1 begin the paired domain/API task and add domain methods/invariants.
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
| `P2-00` / 1.5d | TE-01/09, P1-00 project names | Configure EF Core SQL Server + NetTopologySuite, `DbContext`, configuration assembly and SQL Server integration-test fixture. Output: repository project, test fixture, initial connectivity test. | Negative: missing/invalid connection configuration fails fast; unsupported SRID mapping test fails. Positive: create/drop isolated test DB and round-trip geography/geometry. |
| `P2-01` / 1d | TE-01/10, P1-00 | Add local Docker Compose dependencies, seed framework and CI restore/format/build/test/coverage pipeline. Output: compose/config templates, CI YAML, seed entry point. | Negative: CI sample/failing test proves gate blocks; secrets absent; unhealthy DB fails readiness. Positive: clean checkout command sequence and deterministic seed pass. |
| `P2-02` / 1d | TE-05/07, P2-00 | Add audit/outbox/idempotency/concurrency primitives and transaction conventions. Output: base mappings/interceptors or explicit services, interfaces, integration tests. | Negative: duplicate key, stale row version, transaction rollback, sensitive-value redaction. Positive: atomic domain change+outbox/audit; replay returns prior outcome. |

## Wave 1 — identity storage and authorization proof

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-10` / 1.5d | US-01, CN01-CN03/CN10; P2-00 | Map/migrate User, Role, Session, RefreshToken, PasswordResetLog and AccountStatusChangeLog; map nullable `Session.device_metadata_json` as `nvarchar(max)` with `ISJSON`, application schema validation and write-once behavior; support atomic `UserRoleChanged` persistence/audit plus active-session/token revocation; seed four roles. Output: entities/configurations/migration/seed plus migration downgrade/recovery note. | Negative: duplicate username/token hash, invalid status/log transition, role change leaving an active credential, malformed/non-object/unknown-property metadata JSON, metadata containing secrets, metadata update after session creation, plaintext token/password scan. Positive: session issuance preserves `issued_at`, valid schema-v1 metadata round-trips on SQL Server, role change/session cascade and append-only logs persist atomically. |
| `P2-11` / 1d | US-01, TE-03; P2-10 | Map the project-membership authorization read model using authoritative `ProjectMember.role_code` and add SQL integration fixtures only. Output: active/effective membership query plus reusable users/memberships for four roles. API policies, tokens, HTTP 401/403 behavior, and API tests remain exclusively in P1-12. | Negative: mismatched role, ended/expired membership, and cross-project IDs return no rows. Positive: each role query sees only assigned scope and membership changes are visible immediately. |

## Wave 2 — project and survey persistence

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-20` / 1.5d | US-03, DA01/03-05; P2-02 | Map/migrate Project, ProjectMember, HandoverDocument, Warranty; filtered uniqueness for active primary PM and concurrency token. | Negative: second PM, invalid FK/date, stale update, hard-delete referenced history. Positive: PM handover transaction and multiple warranty records persist. |
| `P2-21` / 1.5d | US-03, DA02/12; P2-20 | Map/migrate RoadSection/RoadSectionVersion with project UTM SRID rules and version indexes. | Negative: SRID 0/wrong zone, null geometry, duplicate version, in-place mutation. Positive: N/N+1 versions coexist and dependent record retains original version ID. |
| `P2-22` / 1d | US-04, DA06-DA09; P2-21 | Map/migrate SurveyPlan, SurveyRequest and postponement history with constraints/indexes. | Negative: invalid dates/status/version FK and duplicate active plan/request where prohibited. Positive: create and append postpone reason/history. |
| `P2-23` / 1d | US-05, KS01-KS04/KS14; P2-22 | Map/migrate Survey/assignment/history; unique active assignment and audit/outbox events. | Negative: duplicate active assignment, invalid status byte, cancel transaction rollback. Positive: reassignment preserves old assignment and emits notification once. |

## Wave 3 — files, workers, and AI persistence

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-30` / 2d | US-06, KS05-KS10; P2-23 | Implement the file-storage boundary/local test store and map Flight, File, SurveyFile, SurveyDataVersion, and QualityCheck with exactly-one-target/checksum constraints. Upload HTTP contracts and confirmation policy remain in P1-30. | Negative: path traversal, oversize storage write, duplicate chunk key, invalid SHA-256, and invalid QualityCheck stage/target. Positive: streamed store/read, resume, server checksum, and atomic persistence completion. |
| `P2-31` / 2d | US-07/18, KS10-KS13/QT06-QT10; P2-30 | Map ProcessingBlock/Job/Attempt/AIModelVersion and implement generic outbox/lease/retry worker infrastructure. Processing state rules, AI adapter contracts, and failure classification remain in P1-31. | Negative: duplicate delivery, lease race, infrastructure retry exhaustion, and DB/storage timeout. Positive: one leased execution under replay with persisted attempt count and no duplicate outbox side effect. |
| `P2-32` / 1.5d | US-08, AI01/04-07; P2-31 | Map immutable AIDetection, Defect, verification history, and FieldInspectionTask; provide constraints and repository transaction support. Detection review decisions and the create-defect/task orchestration remain in P1-32. | Negative: mutate raw AI payload, duplicate retained-detection key, orphan persistence, and invalid status value. Positive: repository transaction persists the detection/defect/task/outbox set atomically when invoked by the service. |

## Wave 4 — measurements and baseline persistence

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-40` / 2d | US-20, TN01-TN04/TN12; P2-32 | Map FieldInspectionAssignment/Session, GroundTruthMeasurement and Evidence with exactly-one target, units and offline client uniqueness. | Negative: wrong target count, duplicate client ID, invalid unit/value/SRID, update submitted row, evidence checksum not confirmed. Positive: append session/measurement/evidence and idempotent retry. |
| `P2-41` / 1d | US-20, TN05-TN06; P2-40 | Map DefectVerificationLog and transactional review/audit constraints. | Negative: verification without completed task/measurement, duplicate decision, stale transaction. Positive: decision+defect state+audit commit atomically; rollback leaves all unchanged. |
| `P2-42` / 1d | US-04, DA10-DA11; P2-41 | Add baseline eligibility query/indexes and SQL integration fixtures. | Negative: each missing prerequisite independently blocks confirmation. Positive: eligible baseline query remains project/version scoped and confirmation is concurrency-safe. |

## Wave 5 — repair persistence and evidence

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-50` / 1.5d | US-11, SC01-SC04; P2-41 | Map RepairBatch/Version/Item with immutable version snapshot, money precision and uniqueness against active repairs. | Negative: duplicate version/item/active repair, arithmetic overflow/rounding mismatch, mutate submitted version. Positive: server total and version snapshot round-trip exactly. |
| `P2-51` / 1d | US-11, SC05-SC09/SC12; P2-50 | Map approval decisions and current-version pointer with concurrency/transaction constraints. | Negative: stale simultaneous decisions, approval without items, change approved snapshot. Positive: return and N+1 submission preserve complete history. |
| `P2-52` / 1d | US-12, SC10-SC11; P2-51 | Map RepairAssignment and unique active assignment/outbox notification. | Negative: assignment to non-current/unapproved version and duplicate delivery. Positive: reassign ends prior record and emits exactly one notification. |
| `P2-53` / 2d | US-13/14, HT01-HT13/HT15; P2-52 | Map append-only RepairProgress/Evidence/InspectionResult/UnplannedDefectReport; implement server integrity query. | Negative: update/delete evidence, missing before/after, cross-item evidence, unconfirmed file, unplanned defect auto-added. Positive: append progress/rework versions and independent item outcomes. |

## Wave 6 — read models, export, retention, research and release

| ID / size | Trace and dependency | Work and concrete output | Required tests and evidence |
|---|---|---|---|
| `P2-60` / 2d | US-09/15, AI08-AI12/BC01-05; P2-53 | Build project-scoped dashboard/read-model queries, indexes and representative performance seed. | Negative: double-count version snapshots, cross-project leakage, null/incompatible periods. Positive: expected metrics/drill-down IDs on golden seed; capture query timing/plan threshold. |
| `P2-61` / 2d | US-16, BC06-BC10; P2-60 | Map ReportExport and implement background PDF/ZIP/CSV generation/storage with checksums and provenance manifest. Export request/status/download authorization and API contracts remain in P1-61. | Negative: worker replay, partial file, storage timeout, duplicate job key, and manifest checksum mismatch. Positive: deterministic manifest, completion/failure recovery, and idempotent file generation. |
| `P2-62` / 2d | US-19, QT11-QT14; P2-61 | Map retention/legal-hold/deletion log and implement a deletion executor that consumes an already approved immutable scope and rechecks legal hold at execution. Request/review policy, dry run, and APIs remain in P1-62. | Negative: active/racing legal hold, partial delete, immutable-scope mismatch, and executor replay. Positive: exact approved scope is applied and the append-only deletion log records the result. |
| `P2-63` / 2d | RS01-RS06; P2-40 | Map research entities, import staging and golden dataset tests; preserve independent purpose. | Negative: duplicate/ambiguous pair, invalid JSON/CSV/unit, excluded/outlier mishandling. Positive: same dataset/version produces identical bias/MAE/RMSE; operational tables unchanged. |
| `P2-64` / 1.5d | US-17, CN04, QT01-QT05; P2-10, P2-60 | Map Notification, catalog/severity/reminder versions, and account-handover records; implement persistence/outbox handlers only. Admin, inbox, configuration, suspension, and handover orchestration APIs remain in P1-64. | Negative: hard-delete catalog/rule history, duplicate reminder delivery, missing session-revocation persistence, and silent loss of open-work records. Positive: versioned configuration persists, a reminder outbox record is emitted once, and the handover query is complete. |
| `P2-65` / 1.5d | US-10/18, AI14, QT06-QT10; P2-31 | Map AI device, training-label approval and dataset export provenance; add model activation uniqueness and job admin indexes. | Negative: multiple active model versions, duplicate label approval/export, missing source/model/checksum, device cross-scope leak. Positive: activation switch is atomic; approved-label export is reproducible and traceable. |
| `P2-67` / 1d | TE-10/release; all core tasks | Create deterministic end-to-end seed, backup/restore rehearsal, security/config scan and release test script/runbook. Output: seed scenario, `docs/runbook.md`, CI release job/report. | Negative: missing secret/config, failed restore, oversized upload and unauthorized seeded user. Positive: clean DB→migrate→seed→full workflow→backup/restore→tests. |

## Person 2 release obligations

Keep SQL Server integration tests deterministic and isolated; publish the exact Docker/SQL Server prerequisites in the runbook; verify all migrations from an empty database and from the previous migration; ensure CI exercises the same commands recorded in completion logs; complete owner self-review and resolve every conflict warning. Release is blocked by EF InMemory-only evidence for SQL behavior, destructive retention without legal-hold race protection, duplicate worker side effects, or secrets in source/logs.

## Retired cross-review task IDs

`P2-12`, `P2-24`, `P2-33`, `P2-43`, `P2-54`, and `P2-66` are retired because each owner now self-reviews and self-marks `Done`. These IDs must not be started or reused. Independent review may still be requested explicitly by the repository owner, but it is not a scheduled implementation task.
