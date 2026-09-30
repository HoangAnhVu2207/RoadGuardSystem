# HUY-03 - Report, case, defect, inspection and repair API

- Owner/branch: Huy / huy
- deliveryStatus: PARTIAL
- contractStatus: PROPOSED_DELTA
- implementationStatus: PARTIAL_CURRENT_FACTS
- verificationStatus: PARTIAL_FOCUSED_API_SQL
- dependencyType: contract
- sourceCheckpoint: HEAD d2338dcffd8838198c2b50ee369b9678e7e06690; canonical OpenAPI SHA-256 ADA7F48F522C0C0DBDACE21F483224A00FC4C76264A9A61D12F3E2211ECCE665

## Task goal
Expose the operational workflow from report and triage through defect assessment, inspection measurement, fast-track/repair decisions, attempts, safety work, traffic release and acceptance while preserving authorization, privacy, evidence versions and state-machine boundaries.

## Business context
Reports can be received outside warranty. Defect publication is per authorized report/defect projection, not whole-case leakage. Measurement, physical completion, traffic release and acceptance are different decisions; AI suggestions never approve work.

## Operation trace
V2-P1-024..071 plus read/export operations V2-P2-036 and V2-P2-056 where the accepted contract requires them. Consume ANH-03 facts.

## Sources to read
Read operation cards, FR-09..26, BR-03..19/30..39, ERD_Report_Case_Defect, ERD_Inspection_Repair, DD 3.5-3.6, permission/API/error specs, SQ-01/02/05, state machines, Appendix E/F, decisions D02-D11/D19/D32A-D35A and ANH-03 handoff.

## In scope
- Write per-operation contracts and current-versus-target matrices before edits.
- Verify actor/project scope, report privacy, defect mapping/publication, severity/recurrence, assignment accept/decline/submit, versioned measurement, repair assignment/reassignment, review/approval and emergency safety boundaries.
- Keep curing, traffic release and acceptance separate in DTO/state mapping and stable errors.
- Add focused API/unit tests, update API.http/Postman and run real HTTP smoke with durable state/audit evidence.
- Ask for a linked decision when reopen/handover/incident/partial-publication behavior is not an approved contract.

## Out of scope
- Persistence/EF/migration edits, inventing repair thresholds/material policies, external field/device proof, AI accuracy or UI.

## Exact files and hotspots
Services/Reports, Cases, Defects, Inspections, Repairs and policy interfaces; DTOs, controllers, API.http, Postman and API/unit tests. Shared authorization/errors/OpenAPI require reservation.

## Stop conditions
Stop on state-machine conflict, missing durable BEFORE evidence, privacy projection ambiguity, unresolved approval authority or proposed production threshold. Mark exact operation PARTIAL/BLOCKED.

## Verification and acceptance
Build fresh Services/API/ApiTests. Run focused authorization/state/privacy tests and real smoke for wrong scope, state transition, response/header and durable audit/outbox effect. Acceptance requires ANH-03 handoff VERIFIED or NO_CHANGE_NEEDED.

## Source evidence - 2026-09-30 receiver review

| Label | Exact source / heading or ID | Evidence used |
|---|---|---|
| CURRENT_VERIFIED | `RoadGuardSystem.API/Controllers/InspectionTasksController.cs`; `RoadGuardSystem.Services/Implementations/Inspections/InspectionTaskQueryService.cs` | The current API exposes only assigned inspection-task reads; actor role is checked, project scope is rechecked through `IProjectScopeGuard`, and response is a bounded DTO projection with opaque rowversion. |
| CURRENT_VERIFIED | `RoadGuardSystem.Repositories/Implementations/Defects/DetectionReviewPersistenceService.cs`, `InspectionTaskReadRepository.cs`; `tests/RoadGuardSystem.IntegrationTests/Defects/P232DetectionDefectSchemaTests.cs`, `Inspections/P240FieldInspectionMeasurementSchemaTests.cs` | Current durable facts cover defect review/inspection measurement/audit/outbox/rowversion; fresh SQL defect 8/8 and inspection 6/6 pass. |
| TARGET_DOCUMENTED | `planning/V2/Person_1/V2-P1-024..071`, `planning/V2/Person_2/V2-P2-036,056`; FR-09..26; BR-03..19/30..39; SQ-01/02/05; state machines | Report/case/defect/inspection/repair contract references and separated measurement, repair, curing, release and acceptance transitions. |
| PROPOSED_DELTA | `docs/diagram/V2/03_Data/ERD_Report_Case_Defect.md`, `ERD_Inspection_Repair.md`, DD §3.5-3.6; draft operation cards `createReport`, `submitInspection`, `startRepairAttempt` | Report/case/link, policy snapshot, RepairAttempt/BEFORE, curing/release/acceptance persistence is not in current checkout; draft cannot authorize an API. |
| NOT_ENABLED | `docs/design/**`, candidate baseline migration | These sources are absent from `d2338dc`; no schema or migration proof is claimed. |

## Contract checkpoint for routes actually consumed or gated

1. `GET /api/v1/me/inspection-tasks`: Repair Crew actor only, own assignment plus project scope; validate cursor/limit; success is a bounded page DTO with rowversion; invalid cursor/role maps to 400/403; read is no-mutation and no N+1; no measurement/repair completion is inferred.
2. Inspection submit/accept/decline operations in the draft: assigned Crew/task scope, measurement payload plus Idempotency-Key/If-Match; target success would return an immutable session; stale/replay/conflict must be separate; current repository facts are partial and no API write is claimed here.
3. `POST /api/v1/reports` and report/case/defect routes in P1-024..042: Reporter/PM scope and privacy; draft requires idempotency/evidence; target success/status/errors are not frozen by current source; no current report/case persistence exists; route is blocked rather than inferred from Defect rows.
4. Repair package/item/attempt routes P1-053..071: Crew/PM/Supervisor authority, BEFORE evidence and versioned transitions; target success must keep physical completion, traffic release and acceptance separate; no RepairAttempt/BEFORE/release/acceptance repository facts exist; no endpoint is added.
5. P2-036/P2-056 label/export reads: only current durable facts may be exposed; proposed label/export persistence is absent, so no read/export claim is made and no fake AI result is promoted to approval.

## Current versus target and outcome

The existing inspection-task read is safe and bounded. ANH-03 explicitly reports that report/case/link, policy snapshot, repair attempt/BEFORE, curing, traffic release and acceptance entities are absent, so HUY-03 cannot implement the target write surface without a linked schema/compatibility decision. This task is `PARTIAL`; it does not claim a defect/repair API.

## Completion history

### 2026-09-30 - PARTIAL (ANH-03 receiver processing)

- Scope/result: Received ANH-03 and confirmed the current inspection read uses scoped repository facts. Report/case/repair/BEFORE/release/acceptance APIs remain blocked by absent persistence and open decisions; no production code change was necessary.
- Files: `planning/V2/Execution/HUY-03-report-inspection-repair-api.md`; receiver ledger row in `planning/CROSS_OWNER_HANDOFFS.md`. No Services/DTOs/API source changed.
- Acceptance: Current Defect/inspection facts are not expanded into target report/case/repair claims; measurement, physical completion, traffic release and acceptance remain separate.
- Verification: sequential `dotnet build RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj -nologo -v q -clp:ErrorsOnly`, `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly`, `dotnet build tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj -nologo -v q -clp:ErrorsOnly`, and `dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj -nologo -v q -clp:ErrorsOnly` PASS; `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~V2P1063InspectionTaskListTests"` PASS 4/4; `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~P232DetectionDefectSchemaTests"` PASS 8/8; `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~P240FieldInspectionMeasurementSchemaTests"` PASS 6/6. No live report/repair HTTP smoke exists because no current report/repair route exists.
- Handoff outcome: `PROCESSED / NEW_TASK_NEEDED:HUY-03-REPORT-CASE-REPAIR-CONTRACT` because current persistence lacks report/case/repair/BEFORE/release/acceptance facts; no repository/API policy was invented.
- Side effects/risk: no package, migration, schema, live data, provider, commit or push; missing `docs/design` and candidate baseline remain explicit blockers.



