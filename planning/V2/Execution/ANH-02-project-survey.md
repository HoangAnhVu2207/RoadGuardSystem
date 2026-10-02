# ANH-02 - Project, route, survey and dataset persistence

- Owner/branch: Anh / anh
- deliveryStatus: PARTIAL
- contractStatus: PROPOSED_DELTA
- implementationStatus: CURRENT_VERIFIED
- verificationStatus: PASS_FOCUSED_SQL_PARTIAL_CONTRACT
- dependencyType: data-fixture
- sourceCheckpoint: current work base HEAD 2efc8a5775f834c7f0fe37cc0ce703011649e1f1 plus uncommitted changes; new `SurveyV2PersistenceService.Dataset.cs` SHA-256 88BCE3F290E64169127F4CD29D89A02B20387DFAA5003F959B3B114A61AEF5F9; canonical OpenAPI SHA-256 ADA7F48F522C0C0DBDACE21F483224A00FC4C76264A9A61D12F3E2211ECCE665; prior HEAD 4586c8c and 1544497 evidence retained below

## Task goal
Provide verified persistence facts for project membership and scope, route/version/segment planning, survey assignment, upload/session files and dataset coverage/readiness. Huy must be able to implement scoped API behavior without confusing a file upload with a confirmed dataset or baseline.

## Business context
A project owns route versions and survey work. PM decisions are scoped to the project; route confirmation, assignment, dataset admission, quality and coverage are separate transitions. Missing flight or position evidence is UNKNOWN, never inferred PASS/FAIL.

## Operation trace
Project/membership: V2-P1-016..023. Survey planning/assignment: V2-P2-009..018 and V2-P2-053..055. Dataset/files: V2-P2-019..028 and V2-P2-059.

## Sources to read
- docs/design/02_Requirements/01_FRD_SRS.md FR-04..08, FR-27..30; 02_Business_Rules.md BR-01/02/20/40/41.
- docs/design/03_Data/ERD_Project_Route.md, ERD_Survey_Dataset.md, DD sections 3.2-3.3a and 9.4/9.6.
- docs/design/05_Technical/02_Auth_Permission_Model.md, 03_API_Specification.md, 05_Sequence_Diagrams.md SQ-01..03, 07_State_Machines_V2.md.
- Decision register D14-D22, D38-D39A and current Project/Survey/File entities, mappings, repositories and SQL tests.

## In scope
- Reconcile project/member scope, route version anchors, road sections/slabs and survey plan/task state against current schema.
- Verify scoped reads/writes, idempotent assignment, rowversion/If-Match behavior and no cross-project leakage.
- Verify upload session/part completion, immutable file metadata, dataset admission receipt, coverage per band and UNKNOWN semantics.
- Add focused SQL tests and minimal repository/entity fixes supported by current contract.
- Publish repository methods, fact/absence/conflict semantics, fixture IDs and durable effects to HUY-02.

## Out of scope
- API/controller/DTO/HTTP/Postman work; geometry algorithm invention; GPX/CRS claims without real bytes.
- AI analysis, defect decisions, repair workflow or external object-storage deployment.
- Creating/applying migrations, changing live data or silently promoting PARTIAL operation cards.

## Exact files and hotspots
Primary areas are BusinessObjects/Projects, Surveys and Files, matching repository interfaces/implementations/configurations and SQL tests under Projects/Surveys/Files. RoadGuardDbContext, mapping, snapshot, seed and shared DI are reserved hotspots.

## Stop conditions
Run after ANH-01 facts needed for actor scope. Stop on ERD/migration conflict, unresolved route/version or geometry policy, absent real file bytes, or any need for schema/provider decision. Keep upload, quality, baseline and coverage as distinct facts.

## Verification and acceptance
Build Repositories and IntegrationTests. Run focused project membership, survey scope/concurrency, dataset, file and spatial tests selected from changed symbols; record fresh binaries and SQL version. Acceptance requires durable SQL evidence and no cross-project leakage. The HUY-02 handoff reports facts and does not gate Anh's independent work.

## Source evidence checkpoint

### Current work checkpoint - 2026-09-30 12:59 +07:00

| Label | Exact source / heading or ID | Invariant used |
|---|---|---|
| CURRENT_VERIFIED | `AGENTS.md` / V2 Source Routing, Verification Ladder; `docs/adr/006-v2-endpoint-ownership-and-persistence-coordination.md` / Decision 1-5 | ANH-02 owns repository facts and focused SQL proof; no migration/schema or cross-layer expansion in this scope. |
| TARGET_DOCUMENTED | `docs/diagram/V2/02_Requirements/02_Business_Rules.md` / BR-01, BR-02, BR-20, BR-40, BR-41; `01_FRD_SRS.md` / FR-04..08, FR-26..30; `03_To_Be_Process.md` / PF-07 | Project scope, one primary PM, immutable route versions, file integrity, assignment history and distinct position/quality/coverage/baseline facts. Missing evidence stays UNKNOWN. |
| TARGET_DOCUMENTED | `docs/diagram/V2/03_Data/01_Data_Dictionary.md` / 3.2, 3.3, 3.2b, 3.3a, 9.6; `02_ERD_V2.md` / Current verified persistence backbone, Target V2 modules; `04_Data_Model_Code_Map.md` / Verified current map | Current route/survey/file anchors exist; coverage and SegmentBaseline are target concepts, not current schema. |
| TARGET_DOCUMENTED | `docs/diagram/V2/05_Technical/02_Auth_Permission_Model.md` / 5.3, 5.6; `05_Sequence_Diagrams.md` / SQ-03; `07_State_Machines_V2.md` / logical transitions; `planning/V2/V2-3_DECISION_REGISTER.md` / D14-D22 | Service policy and per-request scope, verified upload before dataset, separate status axes; Q11 threshold and geometry remain open. |
| PROPOSED_DELTA | `docs/diagram/V2/05_Technical/openapi.yaml` / `getDatasetCoverage`, `confirmBaseline`, `CoverageResultPage`, `BaselineConfirm`; SHA-256 `ADA7F48F522C0C0DBDACE21F483224A00FC4C76264A9A61D12F3E2211ECCE665` | Coverage GET is proposed and baseline POST conditional on Q11; neither authorizes fabricated per-band rows or status. |
| CURRENT_VERIFIED | `RoadGuardSystem.Repositories/Implementations/Projects/ProjectMembershipReadModel.cs` / `FindActiveEffectiveAsync`; `Implementations/Surveys/SurveyV2PersistenceService.cs` / `ResolveScopeAsync`, `SubmitDatasetAsync`; `Implementations/Files/UploadPersistenceService.cs` / `CompleteAsync`, `VerifyNextAsync`; `RoadGuardDbContext.cs` / relevant DbSets; `Migrations/RoadGuardDbContextModelSnapshot.cs` / route/survey/file mappings | Current source and snapshot inspected at base HEAD `2efc8a5`; no `SurveyCoverageResult` or `SegmentBaseline` DbSet. |
| CURRENT_VERIFIED | `tests/RoadGuardSystem.IntegrationTests/{Projects,Surveys,Files}` / focused ANH-02 SQL filter | Fresh Debug/net8.0 Repositories and IntegrationTests builds PASS; focused SQL 56/56 PASS, 0 failed/skipped on local SQL Server `MSSQL$HANHNAV`. Fixture databases are isolated. |

Historical source entries below refer to their recorded dirty checkout and `docs/design/**` paths; those paths and the candidate baseline migration are absent from this work base.

| Source | Heading / ID | Invariant used | Label / checkpoint |
|---|---|---|---|
| `AGENTS.md`; `docs/adr/001-backend-boundary.md`; `docs/adr/004-n-layer-backend-structure.md`; `docs/adr/006-v2-endpoint-ownership-and-persistence-coordination.md` | V2 ownership; Decision | Person 1 owns entity/repository/SQL facts; Service owns authorization and wire policy; no schema change without scope | CURRENT_VERIFIED / HEAD `4586c8c` plus dirty checkout |
| `planning/V2/V2-3_DECISION_REGISTER.md` | D14-D22, 38, 39A | Segment/slab, aircraft position, quality and coverage stay separate; missing evidence is UNKNOWN; geometry/threshold/provider gates remain | TARGET_DOCUMENTED / current file |
| `docs/design/02_Requirements/01_FRD_SRS.md`; `docs/design/02_Requirements/02_Business_Rules.md` | FR-04..08, FR-26..30; BR-01/02/20/40/41 | One primary PM, server-side project scope, immutable route versions, verified files, per-band baseline; missing telemetry cannot imply coverage | TARGET_DOCUMENTED / current files |
| `docs/design/03_Data/ERD_Project_Route.md`; `docs/design/03_Data/ERD_Survey_Dataset.md`; `docs/design/03_Data/01_Data_Dictionary.md`; `docs/design/03_Data/03_Domain_Model_V2.md` | Project/Route and Survey/Dataset entity details; DD 3.2/3.3/3.2b/3.3a/9.6; Aggregate boundaries | Current entity anchors exist; route branch/slab and per-band coverage/baseline are target extensions requiring physical schema check | TARGET_DOCUMENTED / current files |
| `docs/design/05_Technical/02_Auth_Permission_Model.md`; `docs/design/05_Technical/07_State_Machines_V2.md`; `docs/design/05_Technical/05_Sequence_Diagrams.md` | 5.1/5.3/5.6; logical machines; SQ-03 | Service checks actor and active scope; repository supplies facts/atomic writes; upload receipt does not confirm dataset or baseline | TARGET_DOCUMENTED / current files |
| `docs/design/05_Technical/openapi.yaml`; `planning/V2/Person_1/V2-P1-016..023`; `planning/V2/Person_2/V2-P2-009..028,053..055,059` | Operation IDs and schema; V2(3) status | Operation cards are wire trace, not proof of runtime; proposed coverage and route behavior remains gated | PROPOSED_DELTA / OpenAPI SHA-256 `ADA7F48F522C0C0DBDACE21F483224A00FC4C76264A9A61D12F3E2211ECCE665` |
| `RoadGuardSystem.Repositories/Implementations/Projects/ProjectMembershipReadModel.cs`; `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs`; `RoadGuardSystem.Repositories/Implementations/Files/UploadPersistenceService.cs` | FindActiveEffectiveAsync; ResolveScopeAsync/SubmitDatasetAsync; upload receipt | Existing membership, route scope, task rowversion, verified-file and dataset source facts are implemented in source; SQL verification pending | CURRENT_VERIFIED / HEAD `4586c8c` |
| `RoadGuardSystem.Repositories/Migrations/20260929184004_Baseline20260930.cs`; `.Designer.cs`; `Baseline20260930.Triggers.cs`; `RoadGuardDbContextModelSnapshot.cs` | Baseline20260930 | Migration baseline and snapshot are pre-existing uncommitted work, not ANH-02 migration approval or proof of applied schema | CURRENT_VERIFIED / dirty checkout; SQL proof pending |

## Persistence contract and current-vs-target

1. Traced routes/methods and HTTP statuses are owned by HUY-02; this task provides repository facts for project, route, survey, upload and dataset operations.
2. Service determines actor role, membership/assignment and permitted transition; repository inputs include actor/project/task/file IDs and expected version or idempotency key where the existing interface requires them.
3. Repository reads return scoped facts or absence; writes return durable success, replay, conflict or stale-version facts without deciding HTTP error codes.
4. Project/route writes preserve one primary PM and immutable used route versions; survey task writes preserve assignment history, rowversion and idempotency receipt in SQL transactions.
5. Upload verification confirms file metadata integrity only; dataset submission records source files/scope separately; quality, position, coverage and baseline are distinct facts.
6. Missing telemetry or coverage method yields UNKNOWN/absence, never inferred PASS/FAIL; audit records actor and mutation without exposing file bytes.

| Slice | Current source | Target / decision gate |
|---|---|---|
| Project/member/route version | Project membership read model, project/road version repositories and SQL mappings exist | BR-01/02 and FR-04..08 require scope, one primary PM and immutable version; branch/slab and geometry algorithm remain proposed |
| Survey plan/task | `ISurveyV2Repository` and `SurveyV2PersistenceService` store route scopes, assignments, rowversions and idempotency receipts | D20 and FR-26 require observation scope/history; verify SQL behavior without creating new schema |
| Upload/dataset | Upload and dataset repository paths exist; `SubmitDatasetAsync` uses verified file rows and writes `SurveyDataVersion` | D15, BR-40/41 and FR-27..30 require separate position, quality, coverage and baseline; full coverage method/schema is unresolved |

## Completion history
- Initial checkpoint: TODO until all gates pass.

### 2026-09-30 03:10 +07:00 - PARTIAL

- Scope/result: Reconciled project/member, route-version, survey-plan/task, upload and dataset repository facts. Fixed `ResolveScopeAsync` so the request root `RouteVersionId` must be present in the requested scope; added a focused SQL integration test for the rejected mismatch. Coverage/baseline per segment/band remain blocked by the documented target schema/method gate.
- Files: `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs`; `tests/RoadGuardSystem.IntegrationTests/Surveys/P2V2SurveyScopeConcurrencyTests.cs`; `planning/V2/Execution/ANH-02-project-survey.md`; `planning/CROSS_OWNER_HANDOFFS.md`.
- Acceptance criteria: project scope, route scope validation, survey rowversion/idempotency, upload/file integrity and dataset source manifest facts are covered by current source and focused SQL tests. No cross-project leakage was observed. Coverage method, per-band baseline and branch/slab target entities are not claimed as runtime facts.
- Verification: `dotnet build RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj -nologo -v q -clp:ErrorsOnly` PASS (0 errors); `dotnet build tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj -nologo -v q -clp:ErrorsOnly` PASS (0 errors); `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v q --filter "FullyQualifiedName~P2V2SurveyScopeConcurrencyTests|FullyQualifiedName~P211ProjectMembershipReadModelTests|FullyQualifiedName~P220ProjectMembershipSchemaTests|FullyQualifiedName~P221RoadWarrantySchemaTests|FullyQualifiedName~P222SurveyPlanningSchemaTests|FullyQualifiedName~P223SurveyAssignmentSchemaTests|FullyQualifiedName~P219DatasetContractTests|FullyQualifiedName~P230FlightSurveyFileSchemaTests|FullyQualifiedName~P230DataVersionQualityCheckSchemaTests|FullyQualifiedName~UploadPersistenceSqlTests|FullyQualifiedName~FileRepositorySqlTests"` PASS 50/50, failed 0, skipped 0; SQL Server `16.0.1000.6` (2022 Express) verified with `sqlcmd`; isolated databases were created and dropped by the test fixture.
- Reused/invalidated evidence: Existing dirty migration/configuration work was not used as ANH-02 implementation evidence and was not changed. The repository/test builds and focused test run were fresh after the service/test edits.
- Side effects: No package, migration, schema, live-data or external-provider change. No commit or push.
- Unverified/blockers: `getDatasetCoverage` and `confirmBaseline` remain blocked because the approved persistence model lacks the per-band coverage/baseline entities and threshold/method contract. HUY-02 handoff is `SENT`; receiver action and integration evidence are pending.

### 2026-09-30 03:20 +07:00 - PARTIAL / REVIEW_FIX

- Scope/result: Diff-first Team Leader review completed for the ANH-02 task delta. The route-root validation fix is consistent with the existing repository contract and no additional code defect requiring a business or schema decision was found. Clarified the historical TODO line so it does not contradict the authoritative `PARTIAL` status.
- Files: `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs`; `tests/RoadGuardSystem.IntegrationTests/Surveys/P2V2SurveyScopeConcurrencyTests.cs`; `planning/V2/Execution/ANH-02-project-survey.md`; `planning/CROSS_OWNER_HANDOFFS.md` (untracked pre-existing ledger file, task row inspected).
- Acceptance criteria: Repository ownership remains Person 1; no Service/API/DTO/Postman or provider behavior was added. Current route scope, rowversion, idempotency, file and dataset facts remain bounded to existing source and decisions. Coverage/baseline target gaps remain explicitly blocked.
- Verification: Diff review covered the changed service method, its repository interface/status types, focused integration test, task checkpoint and handoff. `git diff --check` PASS for the reviewed tracked delta. Existing fresh builds and SQL-focused run remain valid because no production/test code changed during review: Repositories build PASS, IntegrationTests build PASS, focused SQL filter 50/50 PASS, 0 skipped on SQL Server 2022 Express `16.0.1000.6`.
- Reused/invalidated evidence: No code hunk changed during review; build/test evidence was reused. Documentation-only checkpoint edit does not invalidate compiled binaries or SQL results.
- Side effects: No package, migration, schema, data, provider, stage, commit or push.
- Unverified/blockers: HUY-02 receiver outcome remains pending (`SENT`); `getDatasetCoverage` and `confirmBaseline` remain outside verified runtime persistence scope.

### 2026-09-30 03:34 +07:00 - PARTIAL (handoff publication)

- Scope/result: Owner approved publishing the reviewed root-route scope guard and checkpoint to `develop` so Huy can consume `ANH-02-PROJECT-SURVEY-01`; receiver status remains `SENT`.
- Files: `SurveyV2PersistenceService.cs`, `P2V2SurveyScopeConcurrencyTests.cs`, this task and `planning/CROSS_OWNER_HANDOFFS.md`.
- Verification: Fresh Repositories and IntegrationTests builds PASS; focused identity/survey/notification SQL filter 73 passed, 0 failed, 0 skipped on the dirty local checkout; `git diff --check` PASS for selected code/task files.
- Reused/invalidated evidence: No code change after the 50/50 survey review evidence; clean `develop` requires its own integration verification because candidate baseline and `docs/design` are excluded.
- Side effects: Owner-approved Git publication only; no package, migration application, live data or provider effect.
- Unverified/blockers: Receiver outcome and coverage/baseline persistence decisions remain open; deliveryStatus stays `PARTIAL`.

### 2026-09-30 11:37 +07:00 - PARTIAL (owner accepted Huy handoff)

- Scope/result: Owner accepted Huy's handoff. `ANH-02-PROJECT-SURVEY-01` has receiver outcome `NO_CHANGE_NEEDED` for the current root-route validation and repository statuses at clean HEAD `1544497`; coverage/baseline and geometry remain proposed or absent.
- Files: this checkpoint and `planning/CROSS_OWNER_HANDOFFS.md`; no production or test source changed. Earlier 50/50 project/survey SQL evidence remains historical dirty-checkout evidence.
- Acceptance criteria: HUY-02 recorded survey API 1/1 and scoped survey SQL 2/2 plus full-solution Integration 317/317 on `d2338dc`; `1544497` changes request examples and planning only. The required current local SQL gate did not pass; per-band coverage/baseline method and schema are still unresolved. Task and handoff remain `PARTIAL`/`PROCESSED`.
- Verification: Repositories build PASS (51 warnings, 0 errors) and IntegrationTests build PASS (316 warnings, 0 errors), Debug/net8.0. The combined identity/project/survey SQL filter executed 122 tests: 1 passed, 121 failed, 0 skipped, because the configured SQL Server connection was unavailable before fixture setup. `MSSQL$HANHNAV` and Docker are stopped; service start was denied. `python docs/diagram/V2/ci/check_alignment.py` PASS (133 tasks); `git diff --check` PASS; `python docs/diagram/V2/09_Frontend/contracts/check_contracts.py` fails `CONTRACT_LOCK_MISMATCH` on unchanged contract files.
- Reused/invalidated evidence: Huy's receiver tests remain recorded evidence at their checkpoint, not a passing local run. No runtime source changed after build; changed SQL environment invalidates local reuse of prior SQL results.
- Side effects: no package, migration, schema, live data, external provider, commit or push; SQL service remains stopped.
- Unverified/blockers: rerun focused project/survey SQL against a reachable server; approve the missing coverage/baseline physical model and method in a separately scoped task before claiming those slices or `DONE`. Candidate baseline and `docs/design/**` are absent here.

### 2026-09-30 13:24 +07:00 - PARTIAL (current SQL gate and dataset scope)

- Scope/result: Current `anh` checkout on base HEAD `2efc8a5` passed the previously blocked SQL gate. Two new SQL tests reproduced dataset admission accepting an unassigned segment and a verified `DOCUMENT` file as video; repository now rejects both before writing a dataset. Dataset scope must be a non-empty subset of persisted task scopes, with matching route version, segment set, segment IDs and band. Video file purpose must be `SURVEY_VIDEO`, telemetry purpose `TELEMETRY`, and each must remain verified and task/project scoped. A valid assigned scope/video succeeds and same-key replay creates no additional dataset, SurveyFile or audit row. These are persistence backstops derived from FR-26/27, BR-02/20, DD 3.3a and SQ-03; no coverage threshold or baseline policy was chosen.
- Files: `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs`, new `SurveyV2PersistenceService.Dataset.cs` (owner-approved split of oversized class), `tests/RoadGuardSystem.IntegrationTests/Surveys/P2V2SurveyScopeConcurrencyTests.cs`, this task, `planning/V2/Execution/README.md`, `planning/CROSS_OWNER_HANDOFFS.md`.
- Acceptance criteria: Current project/member, route, survey planning/assignment, file and dataset persistence gates pass on local SQL. Dataset admission now enforces task scope and file purpose without cross-task/project promotion. `getDatasetCoverage`, `confirmBaseline`, branch/slab and coverage method remain target/proposed, not current persistence claims.
- Verification: Debug/net8.0 `dotnet build RoadGuardSystem.Repositories/RoadGuardSystem.cRepositories.csproj -nologo -v q -clp:ErrorsOnly` PASS (47 warnings, 0 errors); `dotnet build tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj -nologo -v q -clp:ErrorsOnly` PASS (322 warnings, 0 errors); `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v q --filter "FullyQualifiedName~P2V2SurveyScopeConcurrencyTests|FullyQualifiedName~P211ProjectMembershipReadModelTests|FullyQualifiedName~P220ProjectMembershipSchemaTests|FullyQualifiedName~P221RoadWarrantySchemaTests|FullyQualifiedName~P222SurveyPlanningSchemaTests|FullyQualifiedName~P223SurveyAssignmentSchemaTests|FullyQualifiedName~P219DatasetContractTests|FullyQualifiedName~P230FlightSurveyFileSchemaTests|FullyQualifiedName~P230DataVersionQualityCheckSchemaTests|FullyQualifiedName~UploadPersistenceSqlTests|FullyQualifiedName~FileRepositorySqlTests"` PASS 59/59, failed 0, skipped 0. `dotnet build tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj -nologo -v q -clp:ErrorsOnly` PASS (132 warnings, 0 errors); boundary test filter `FullyQualifiedName~RepositorySourceBoundaryTests|FullyQualifiedName~ServiceSourceBoundaryTests` PASS 2/2, failed 0, skipped 0. SQL Server `MSSQL$HANHNAV` is running, version `16.0.1000.6` Express; fixtures used isolated databases.
- Reused/invalidated evidence: Initial clean-base SQL 56/56 superseded the historical stopped-server failure. New tests failed 2/2 against the previous implementation as intended. Final builds and 59/59 SQL run were fresh after the code split; prior compiled evidence was invalidated. No endpoint/DTO/HTTP code changed, so hosted HTTP smoke is outside this persistence task.
- Side effects: No package, migration, schema, live-data, external storage/provider, commit or push. Only disposable SQL fixture databases were created and dropped.
- Unverified/blockers: Q11 lacks agreed coverage/position/quality thresholds and method version; current model/snapshot lacks `SurveyCoverageRequirement`, `SurveyCoverageResult` and `SegmentBaseline`. `SurveyDataVersion` file verification alone is not per-band coverage or baseline. Owner chose the separate [COV-BASELINE-Q11-CONTRACT](../Governance/COV-BASELINE-Q11-CONTRACT.md) task at 2026-09-30 13:28 +07:00; ANH-02 remains `PARTIAL` until an approved contract/schema scope and its gates pass. Huy's new handoff is a report, not an approval gate.

### 2026-09-30 13:28 +07:00 - PARTIAL (owner Q11 decision)

- Scope/result: Owner kept ANH-02 `PARTIAL` and selected a separate Q11/schema decision task. `planning/V2/Governance/COV-BASELINE-Q11-CONTRACT.md` records the missing method, threshold, physical model, versioning and migration-approval questions. Huy's handoff ledger now links this gate.
- Files: this task, `planning/V2/Governance/COV-BASELINE-Q11-CONTRACT.md`, `planning/V2/Governance/README.md`, `planning/CROSS_OWNER_HANDOFFS.md`.
- Acceptance criteria: No coverage/baseline runtime claim or migration permission was inferred from the decision to create a task. Existing 59/59 SQL and 2/2 boundary evidence remains valid because production and test source did not change.
- Verification: `python docs/diagram/V2/ci/check_alignment.py` PASS (`ALIGNMENT_GUARDS_PASS`, 133 operations, owners 71/62); `git diff --check` PASS; both new task links resolve locally. Runtime evidence from the preceding checkpoint is reused.
- Side effects: documentation only; no package, migration, schema, live data, external provider, commit or push.
- Unverified/blockers: Q11 values, model compatibility, migration and API coverage/baseline behavior remain open in the linked task.
