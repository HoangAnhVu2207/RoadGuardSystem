# ANH-03 - Report, case, defect, inspection and repair persistence

- Owner/branch: Anh / anh
- deliveryStatus: IN_PROGRESS
- contractStatus: PROPOSED_DELTA
- implementationStatus: PARTIAL_CURRENT_FACTS
- verificationStatus: PASS_FOCUSED_SQL_AUTH
- dependencyType: data-fixture
- sourceCheckpoint: base HEAD `4586c8caa5aa8439c1ea9f9e385a8ee59359f0bb`; draft `docs/design/05_Technical/openapi.yaml` SHA-256 `ADA7F48F522C0C0DBDACE21F483224A00FC4C76264A9A61D12F3E2211ECCE665`; dirty baseline migration, snapshot, Defect configuration and SQL tests predate this task

## Source evidence — 2026-09-30 03:11 +07:00

| Label | Exact source / heading or ID | Invariant used |
|---|---|---|
| TARGET_DOCUMENTED | `AGENTS.md` §V2 Ownership/Verification; `docs/adr/001-backend-boundary.md` §Decision; `docs/adr/004-n-layer-backend-structure.md` §Decision; `docs/adr/006-v2-endpoint-ownership-and-persistence-coordination.md` §Decision | Anh owns entities/repositories/SQL; Huy owns Service/API; SQL proof is not API proof. |
| TARGET_DOCUMENTED | `planning/V2/V2-3_DECISION_REGISTER.md` D02–D11, D19, 32A–35A; `docs/design/02_Requirements/01_FRD_SRS.md` FR-11–25; `docs/design/02_Requirements/02_Business_Rules.md` BR-03–19, BR-30–39 | Report triage, PM verification, measurement, BEFORE, repair, traffic release and acceptance are separate facts; no invented physical threshold. |
| TARGET_DOCUMENTED | `docs/design/05_Technical/07_State_Machines_V2.md` §Case/Defect, Inspection task, Repair attempt, Curing/release; `docs/design/05_Technical/05_Sequence_Diagrams.md` SQ-01/02; `docs/design/05_Technical/02_Auth_Permission_Model.md` §5.2–5.5 | Logical transitions require actor/scope/version/evidence; Service decides authorization; repository persists atomic facts. |
| PROPOSED_DELTA | `planning/V2/Person_1/V2-P1-024_createReport.md` §3; `V2-P1-051_submitInspection.md` §3; `V2-P1-058_startRepairAttempt.md` §3; `docs/design/05_Technical/openapi.yaml` operationIds `createReport`, `submitInspection`, `startRepairAttempt` | Draft HTTP contracts trace persistence needs but do not prove runtime. Other ANH-03 operation cards remain draft traces, not implementation authorization. |
| PROPOSED_DELTA | `docs/design/03_Data/ERD_Report_Case_Defect.md` §Target entities; `ERD_Inspection_Repair.md` §Target entities; `01_Data_Dictionary.md` §3.2a, §3.6, §9.3–9.4; `03_Domain_Model_V2.md` §Aggregate boundaries/transitions; `04_Data_Model_Code_Map.md` §Target gaps | Report/case/link, policy and repair entities require an approved schema contract; BEFORE and release records cannot be inferred from current inspection rows. |
| CURRENT_VERIFIED | `RoadGuardSystem.BusinessObjects/Defects/Defect.cs` `Create`; `RoadGuardSystem.BusinessObjects/Inspections/FieldInspectionTask.cs` `Create`; `RoadGuardSystem.Repositories/RoadGuardDbContext.cs` DbSets; `Configurations/DefectConfiguration.cs`, `FieldInspectionTaskConfiguration.cs`; `Implementations/Defects/DetectionReviewPersistenceService.cs` `PersistAsync`; `Implementations/Inspections/InspectionTaskReadRepository.cs` `ListAssignedAsync` | Current source has Defect and inspection, rowversion on task, idempotent defect-review write with audit/outbox, and bounded no-tracking assigned-task read. Runtime SQL still needs execution. |
| CURRENT_VERIFIED | `RoadGuardSystem.Repositories/Migrations/20260929184004_Baseline20260930.cs`/`.Designer.cs`; `Migrations/RoadGuardDbContextModelSnapshot.cs`; `tests/RoadGuardSystem.IntegrationTests/Defects/P232DetectionDefectSchemaTests.cs`; `Inspections/P240FieldInspectionMeasurementSchemaTests.cs`; `Projects/P221RoadWarrantySchemaTests.cs` | Dirty replacement baseline represents current migration source, not applied-schema proof; focused SQL tests are selected for execution. |
| HISTORICAL | `planning/RoadGuard_Plan_Person_1.md` §Work packages DB-04/05; `planning/V2/Execution/ANH-02-project-survey.md` §Verification | DB-04/05 define the queue; ANH-02 is still TODO, so scope fixtures are not yet a verified handoff. |

## Contract checkpoint (draft operation trace, persistence boundary)

- `POST /reports` (`createReport`): Reporter with own report scope; Service owns actor/authorization and input validation; draft requires description, photos and Idempotency-Key.
- Draft success is 201 `IncidentReport` with Location/ETag; stable business errors are not frozen, so repository must return absence/conflict facts rather than HTTP codes.
- Report receipt must retain reporter/source and permit triage outside warranty (D19); no report/case/link persistence exists in this checkout, so this write is blocked on schema/compatibility decision.
- `POST /inspection-tasks/{taskId}/sessions` (`submitInspection`): assigned Crew and scoped task; draft requires measurements, observedAt, Idempotency-Key and If-Match/taskVersion.
- Draft success is 201 `InspectionSession`; stale version/conflict must not commit partial measurement; current session/measurement schema exists, but the full V2 state/policy snapshot is not mapped.
- `POST /repair-attempts` (`startRepairAttempt`): assigned Crew and permitted repair scope; draft requires BEFORE file IDs, startedAt and Idempotency-Key.
- Draft success is 201 `RepairAttempt`; BEFORE must be durable before work and acceptance/release distinct; no RepairAttempt/BEFORE schema exists, so no repository write can be claimed.
- Across all three, repository owns transaction, dedup, rowversion and audit/outbox facts; Service owns state/authorization policy, stable result codes and HTTP mapping. Sensitive Reporter/evidence data must stay scoped.

## Current versus target

| Slice | Current source | Target / decision needed |
|---|---|---|
| Report/case/public link | No DbSets or migrations | D10/D19 require source ownership and Reporter isolation; approve schema/compatibility before write interface. |
| Defect severity/scope | `Defect.Create` full path requires non-Unknown severity; simple path has nullable project; mapping defaults Unknown | DD §3.4 allows unclassified severity and describes project FK as required; decide legacy compatibility/data migration before tightening. |
| Inspection | Task/session/measurement and task rowversion exist | D02/D07 task mode, validity/snapshot and actor transitions need agreed contract and fixtures. |
| Repair/BEFORE/release | No corresponding DbSets | D08/D11 require distinct durable records; schema and acceptance authority remain gated. |

Business flow: Reporter → Service authorization/scope → repository report receipt (target) → PM triage/case/defect decision → inspection assignment/session/measurement (current partial) → repair attempt/BEFORE, curing, release, acceptance (target) → scoped Service DTO response. No current persistence fact alone establishes Service authorization or HTTP behavior.

## Task goal
Establish persistence facts for the chain from reporter report through triage/case/defect assessment, inspection measurement, repair package/attempt, approval, curing, traffic release and acceptance. Every business state and evidence version must remain durable and auditable.

## Business context
A report may be outside warranty and still enter triage. A defect is not automatically repair authorization. BEFORE evidence remains durable; measurement completion, physical completion, traffic release and acceptance are distinct. PM/Supervisor decisions and safety work must not collapse into one status.

## Operation trace
Reports/cases/defects: V2-P1-024..042. Policy/inspection/repair: V2-P1-043..071, with V2-P2-036 and V2-P2-056 only as read/export facts consumed by Huy.

## Sources to read
- docs/design/02_Requirements/01_FRD_SRS.md FR-09..26; 02_Business_Rules.md BR-03..19 and BR-30..39.
- docs/design/03_Data/ERD_Report_Case_Defect.md, ERD_Inspection_Repair.md, DD sections 3.5-3.6.
- docs/design/05_Technical/02_Auth_Permission_Model.md, 05_Sequence_Diagrams.md SQ-01/02/05, 07_State_Machines_V2.md, Appendix E/F.
- Decision register D02-D11, D19, D32A-D35A and current Defect, Inspection, Repair, Warranty and policy source/tests.

## In scope
- Reconcile report/case/defect identity, report-to-defect visibility, severity/recurrence and warranty responsibility facts.
- Verify inspection assignment/accept/decline/submit, measurement provenance, immutable snapshots and fast-track eligibility.
- Verify repair package/item/attempt versions, assignment/reassignment, review/approval, emergency safety scope, curing and traffic-release evidence.
- Add focused SQL/concurrency tests and minimal persistence fixes within approved source/schema.
- Publish state transitions, ownership facts, version tokens, audit/outbox effects and fixtures to HUY-03.

## Out of scope
- Inventing physical repair thresholds, policy production values, AI matching, field/device evidence or UI.
- Service/API/DTO/HTTP/Postman edits.
- New operations for reopen, handover, incident or partial publication without an approved backlog contract.
- Migration/live DB changes without explicit scope.

## Exact files and hotspots
Primary areas: BusinessObjects/Reports, Cases, Defects, Inspections, Repairs, Warranties and policy entities; corresponding repositories/configurations and SQL tests. Shared DbContext, migrations/snapshot and seed remain single-writer hotspots.

## Stop conditions
Requires project/scope facts from ANH-02. Stop on state-machine versus current-schema conflict, missing durable BEFORE evidence, unresolved authorization/acceptance rule or a proposed physical threshold presented as production fact.

## Verification and acceptance
Build Repositories and IntegrationTests. Run focused defect, inspection, repair, warranty and concurrency SQL tests from fresh binaries; inspect durable state/audit/outbox records. Acceptance requires every changed transition covered, no state collapse and HUY-03 handoff VERIFIED or NO_CHANGE_NEEDED.

## Completion history

### 2026-09-30 03:18 +07:00 - PARTIAL

- Scope/result: Reconciled current persistence for Defect, inspection/measurement and warranty against the ANH-03 report/case/repair target. Added source evidence, persistence-boundary contracts, current-vs-target matrix and explicit blockers. No production entity/schema migration was invented.
- Files: `planning/V2/Execution/ANH-03-report-inspection-repair.md`; `planning/CROSS_OWNER_HANDOFFS.md`.
- Acceptance criteria: Current Defect/inspection facts and durable effects documented; report/case/link, policy snapshot, RepairAttempt/BEFORE, curing/release/acceptance remain blocked because ERD/DD/code map mark them `PROPOSED_DELTA` and no DbSet/configuration/migration exists. Severity nullability conflict remains open.
- Verification: `dotnet build RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj -nologo -v q -clp:ErrorsOnly` PASS (0 warnings, 0 errors); `dotnet build tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj -nologo -v q -clp:ErrorsOnly` PASS (0 warnings, 0 errors); `dotnet test ... --no-build --filter "FullyQualifiedName~P232DetectionDefectSchemaTests|FullyQualifiedName~P240FieldInspectionMeasurementSchemaTests|FullyQualifiedName~P221RoadWarrantySchemaTests"` PASS 18/18, 0 skipped, SQL Server Testcontainers `mcr.microsoft.com/mssql/server:2019-CU18-ubuntu-20.04`. `git diff --check` PASS. SQL tests inspected durable rows, audit/outbox, idempotency replay/conflict/rollback, rowversion-related schema, FK/check constraints, spatial SRID and immutable triggers.
- Reused/invalidated evidence: Existing dirty baseline migration/configuration/source evidence was read but not treated as applied-schema proof. No source code or migration edit occurred; production build and selected SQL checks were run from current binaries. API/Postman/HTTP evidence is not applicable to Anh layer and was not claimed.
- Side effects: No package, migration creation/application, schema change, data change, external provider call, commit or push. Handoff `ANH-03-PERSISTENCE-01` added to `planning/CROSS_OWNER_HANDOFFS.md`, status `SENT` pending Huy response.
- Unverified/blockers: ANH-02 fixtures are not verified; no current report/case/repair persistence exists; D02/D03/D08/D10/D11 target schema and severity compatibility decision are open. HUY-03 cannot be `VERIFIED` until receiver confirms `NO_CHANGE_NEEDED` or creates a linked schema task and integration evidence.

### 2026-09-30 03:25 +07:00 - PARTIAL (TEAM LEADER REVIEW)

- Review scope: task checkpoint changes only, base `4586c8caa5aa8439c1ea9f9e385a8ee59359f0bb`; reviewed the task evidence, contract checkpoint, current-vs-target matrix, completion history and ANH-03 handoff row. Pre-existing dirty work includes repository configuration/migration baseline, broad docs restructuring, operation cards and the untracked handoff ledger; those files were not reviewed as ANH-03 implementation changes.
- Review standard: `AGENTS.md`, `TASK_LIFECYCLE.md`, ADR 001/004/006, D02–D11/D19/32A–35A, FR-11..25, BR-03..19/30..39, state machines, SQ-01/02, Report/Case/Defect and Inspection/Repair ERDs, Data Dictionary §3.4/§3.6/§9.3–9.4, current entities/configurations/repositories and focused SQL tests.
- Findings: no RED correctness or scope violation found. One YELLOW documentation defect was found and fixed: the current-vs-target Markdown table declared three columns but had only two separator cells. No public contract, schema, authorization or state decision was made.
- Verification after fix: `git diff --check` PASS. Existing repository and integration builds plus focused SQL evidence remain valid because only task documentation changed; no code or migration source changed after those checks. No API smoke, Postman check or API claim was made.
- Handoff: `ANH-03-PERSISTENCE-01` remains `SENT`; receiver outcome and integration evidence are missing, so this review cannot authorize `DONE`, commit or push.
- Unreviewed scope: pre-existing dirty files and target entities explicitly marked `PROPOSED_DELTA`; no implementation review is claimed for them.

### 2026-09-30 03:34 +07:00 - PARTIAL (handoff publication)

- Scope/result: Owner approved publishing this source checkpoint to `develop` for Huy's `ANH-03-PERSISTENCE-01` receiver review; no schema or API behavior is claimed complete.
- Files: This task and `planning/CROSS_OWNER_HANDOFFS.md`; no ANH-03 production or test source changed.
- Verification: Fresh Repositories and IntegrationTests builds PASS; reviewed 18/18 focused SQL evidence is retained as local candidate-baseline evidence, not clean-branch proof; selected documentation diff check PASS.
- Reused/invalidated evidence: No ANH-03 code edit. The uncommitted baseline and `docs/design` restructure are excluded from publication.
- Side effects: Owner-approved Git publication only; no package, migration application, live data or provider effect.
- Unverified/blockers: Receiver outcome, report/case/repair schema decisions and fixture integration remain open; deliveryStatus stays `PARTIAL`.
