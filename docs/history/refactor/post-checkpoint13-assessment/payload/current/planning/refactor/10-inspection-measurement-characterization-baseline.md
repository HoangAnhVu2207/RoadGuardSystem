# RF-10-07: Inspection and measurement characterization baseline

**Status:** C01 complete with correction-02 verified; R01 RETAIN recommendation  
**Branch:** `anh`  
**Commit surveyed:** `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`  
**Evidence:** `planning/refactor/evidence/rf1007-c02/` (correction-02 archive SHA-256: `f7581e219d76bec870aa8eb7ffb4ca09baa3fe3271691c9e1cadb8b981e1de8d`)

## 1. Scope

RF-10-07-C01 characterizes the current inspection task list endpoint (`GET /api/v1/me/inspection-tasks`) and verifies measurement source linkage. This establishes the baseline for current RepairCrew-scoped task retrieval, project membership filtering, response projection, and read-side SQL immutability.

**In scope:**
- `InspectionTasksController.List` HTTP endpoint
- `InspectionTaskQueryService.ListAssignedAsync` authorization and filtering
- `InspectionTaskReadRepository.ListAssignedAsync` query projection
- Role-based authorization (RepairCrew only)
- Project membership scope filtering
- Response DTO projection (id, projectId, defectIds, mode, crewId, status, version)
- SQL read-side immutability verification
- `GroundTruthMeasurement` source linkage to defect/survey/road version

**Out of scope:**
- Task creation, assignment, or mutation endpoints (NOT_IMPLEMENTED)
- PM batch review, safety actions, policy evaluation (F/G future work)
- Fast Track repair workflows and acceptance states (F/G future work)
- Measurement submission or validation logic (separate module)
- Cursor pagination edge cases beyond basic validation
- Concurrency and optimistic locking behavior

## 2. Current implementation

### HTTP route
- **Route:** `GET /api/v1/me/inspection-tasks`
- **Authorization:** `[Authorize]` attribute; requires valid JWT bearer token
- **Role restriction:** Service layer restricts to `UserRoleCode.RepairCrew`
- **Query parameters:**
  - `cursor` (optional): Base64-encoded pagination cursor (UTC ticks | GUID format)
  - `limit` (optional): Page size, default 50, range [1, 100]

### Response outcomes
- **200 OK:** RepairCrew with valid project membership; returns paginated task list
- **400 Bad Request:** Invalid cursor format or limit out of range
- **401 Unauthorized:** Missing or invalid bearer token; actor extraction fails
- **403 Forbidden:** Non-RepairCrew roles (ProjectManager, Supervisor, Reporter, etc.)

### Actor extraction
Controller extracts actor from JWT claims:
- `sub` claim → `Guid actorUserId`
- `role` claim → `UserRoleCode actorRole` via `UserRoleCodeExtensions.FromDbCode`
- Both must be valid; empty GUID or `Unknown` role → 401

### Authorization flow
1. Service checks `actorRole == UserRoleCode.RepairCrew`; others → `Forbidden`
2. Repository queries tasks assigned to `actorUserId` (via `FieldInspectionAssignments` where `Status = Active`)
3. Service iterates results and calls `IProjectScopeGuard.AuthorizeAsync` per unique `ProjectId`
4. Tasks in projects where crew lacks membership are filtered out from response

### Response projection
Each visible task returns:
- `id`: Task GUID
- `projectId`: Project GUID
- `defectIds`: Array containing single `DefectId` from task
- `mode`: Hardcoded string `"MEASURE_ONLY"`
- `crewId`: `AssignedToUserId` from active assignment
- `policyVersionId`: Always `null` in current implementation
- `status`: Task status as uppercase string (e.g., `"ACCEPTED"`)
- `version`: Base64-encoded `RowVersion` byte array

### Pagination
- Cursor encodes `(lastDueAt.UtcTicks, lastId)` as Base64 UTF-8
- Repository orders by `DueAt ASC, Id ASC`
- Fetches `limit + 1` rows; `hasMore` when count exceeds limit
- Service returns `nextCursor` only when `hasMore` is true

### SQL schema and constraints
- `FieldInspectionTasks` table with `RowVersion` for concurrency
- `FieldInspectionAssignments` table with unique constraint on active assignments per task
- `GroundTruthMeasurements` table with immutable trigger protection after session completion
- Measurements link to `FieldInspectionSessionId`, `DefectId`, `SurveyId`, `RoadSectionVersionId`
- Measurement `Location` requires SRID 4326 (WGS84 geography)
- Measurement `Unit` restricted to `'mm'`, `'cm'`, `'m'` via check constraint
- Measurement `MeasurementType` enum: DepressionDepth, SlabFaultingHeight, CrackWidth

## 3. Observed behavior

### Role authorization
- **RepairCrew:** Receives 200 OK with tasks they are assigned to and have project access
- **ProjectManager:** Returns 403 Forbidden with ProblemDetails status 403
- **Supervisor:** Returns 403 Forbidden
- **Other roles:** Not tested; expected 403 based on service logic

### Project scope filtering
- Tasks are assigned to crew via `FieldInspectionAssignments.AssignedToUserId`
- Service calls `IProjectScopeGuard.AuthorizeAsync` per project
- Projects without crew membership are filtered out (empty result or reduced list)
- Scope check is per-project, not per-task (cached in service for efficiency)

### Read-side immutability
Fresh `DbContext` snapshots before and after success GET request (RepairCrew with valid membership) confirmed no changes to asserted fields:
- `FieldInspectionTask`: Id, Status, TaskCode, ProjectId, DefectId, RowVersion unchanged
- `FieldInspectionAssignment`: Id, Status, AssignedToUserId, AssignedByUserId, AssignedAt unchanged
- `Defect`: Id, Status, ProjectId, RoadSectionVersionId unchanged
- `GroundTruthMeasurement`: Id, DefectId, SurveyId, RoadSectionVersionId, SessionId, MeasurementType, Value, Unit, Location SRID and coordinates unchanged

**Correction-02 improvements:**
- Changed from measurement count-only to full record capture with `SingleAsync`
- Fixed RowVersion comparison: `BeEquivalentTo` → `Equal` (byte-order sensitive)
- Moved baseline capture after authentication, immediately before GET
- Added comprehensive field assertions for all entities (identity, linkage, content)

**Scope clarification:** Immutability verified for success GET only (RepairCrew with valid membership). Denied GET (PM/Supervisor) and filtered GET (crew without membership) NOT verified; no snapshots captured for audit/outbox or other entities when GET returns 403 or empty list.

### Measurement provenance
Schema inspection and seeded fixture confirmed:
- `GroundTruthMeasurement` has non-null `DefectId` and `SurveyId` for product sessions
- Measurement links to exact `RoadSectionVersionId` matching task and defect
- `MeasurementType`, `Value`, `Unit`, and `Location` stored as expected
- `Location.SRID = 4326` enforced by domain entity and database column configuration (EF)
- Measurements belong to `FieldInspectionSession` with purpose `DefectVerification` or `ResearchValidation`

Runtime enforcement of SRID 4326 constraint and immutability triggers NOT verified; configuration SOURCE_INSPECTED only.

## 4. Test evidence

**Focused test:** `Rf1007InspectionMeasurementCharacterizationTests`  
**Location:** `tests/RoadGuardSystem.ApiTests/Inspections/`

### Test cases (5 total, all passed in correction-02)
1. `InspectionTaskList_RepairCrewWithScope_ReturnsTasksAndDoesNotMutateDatabase`
   - Verifies RepairCrew receives 200 OK with correct projection
   - Asserts all response fields match seeded data
   - Comprehensive snapshots: task (Id, Status, TaskCode, ProjectId, DefectId, RowVersion), assignment (Id, Status, AssignedToUserId, AssignedByUserId, AssignedAt), defect (Id, Status, ProjectId, RoadSectionVersionId), measurement (Id, DefectId, SurveyId, RoadSectionVersionId, SessionId, MeasurementType, Value, Unit, Location SRID/coordinates)
   - Byte-order sensitive RowVersion comparison with `.Should().Equal()`

2. `InspectionTaskList_ProjectManagerRole_ReturnsForbidden`
   - Verifies ProjectManager role returns 403 Forbidden

3. `InspectionTaskList_SupervisorRole_ReturnsForbidden`
   - Verifies Supervisor role returns 403 Forbidden

4. `InspectionTaskList_CrewWithoutProjectMembership_FiltersOutTask`
   - Seeds task assigned to crew but without project membership
   - Confirms task is filtered out from response (not 403, but empty or reduced list)

5. `InspectionTaskList_MeasurementProvenance_ExistsInDatabase`
   - Queries `GroundTruthMeasurement` for seeded session
   - Confirms linkage to defect, survey, road version
   - Validates measurement type, value, unit, and SRID 4326

### Test execution (correction-02)
- **Build:** `dotnet build --no-incremental`, 2026-10-01 08:13:10-08:13:25 UTC, 164 warnings, 0 errors
- **Test:** `dotnet test --no-build --filter 'FullyQualifiedName~Rf1007InspectionMeasurementCharacterizationTests'`, 2026-10-01 08:14:47-08:15:09 UTC, duration 22 seconds
- **Results:** 5 discovered, 5 executed, 5 passed, 0 failed, 0 skipped
- **Provenance:** Test + 2 fixtures hashed before/after (3 files); assembly hash recorded after build and after test; no production-source fingerprint; build end timestamp approximate
- **TRX:** `planning/refactor/evidence/rf1007-c02/rf1007-c02-test.trx`

### Historical evidence
- **Checkpoint 11:** Initial inspection characterization attempt with test failures documented
- **Checkpoint 11 correction-01:** 5/5 passed but source-to-binary linkage NOT_VERIFIED (metadata conflict between build-metadata and test-run-metadata); has its own verification evidence separate from checkpoint 12
- **Checkpoint 12 correction-02:** Supersedes correction-01 for bounded verification; test + 2 fixtures hashed before/after and assembly hash recorded. No production-source fingerprint; build end approximate. This is not a complete production source-to-binary chain. Correction-01 linkage stays NOT_VERIFIED.

## 5. Findings

| ID | Impact | Evidence | Current state / handling |
|---|---|---|---|
| RF1007-C02-F01 | Low | `InspectionTasksController.List`, `InspectionTaskQueryService`, focused tests | Endpoint restricted to RepairCrew role; PM/Supervisor/others receive 403 Forbidden. Authorization and project scope filtering work as implemented. |
| RF1007-C02-F02 | Low | Service line 76-84: response projection with hardcoded mode | Response includes: id, projectId, defectIds (single-element array), hardcoded "MEASURE_ONLY" mode, crewId (AssignedToUserId), null policyVersionId, uppercase status string, Base64-encoded RowVersion. |
| RF1007-C02-F03 | Medium | Service lines 58-74: scope check loop with per-project caching | Project membership filtering applied at service layer after repository fetch. Repository returns all tasks assigned to user; service filters by authorized projects using cached scope guard results. |
| RF1007-C02-F04 | Low | Fresh DbContext snapshots before/after success GET | No observed changes to asserted record fields after success GET (RepairCrew with valid membership). Task, assignment, defect, measurement fields unchanged. NOT verified for denied/filtered GET operations. |
| RF1007-C02-F05 | Medium | `GroundTruthMeasurement` EF configuration and seeded test fixture | Measurement provenance links SessionId to DefectId, SurveyId, RoadSectionVersionId. SRID 4326 column configuration, unit check constraint ('mm'/'cm'/'m'), and immutability trigger reference (HasTrigger) present. Runtime constraint enforcement NOT verified. HTTP submission endpoint not characterized. |
| RF1007-C02-F06 | Low | Service line 80: hardcoded mode string | Response mode is fixed "MEASURE_ONLY" string; policyVersionId always null. Policy evaluation not implemented in current code. |

## 6. Limitations and unknowns

**NOT_IMPLEMENTED:**
- Task creation/assignment mutation endpoints
- PM batch review workflow
- Temporary safety action recording
- Policy evaluation and versioning
- Repair proposal/attempt/acceptance states
- Fast Track dossier application

**NOT_VERIFIED:**
- Response version field mapping to Base64 RowVersion (assertion only checks non-empty string)
- Denied/filtered GET immutability (no snapshots for PM/Supervisor/filtered operations)
- Cursor pagination boundary conditions (very large result sets, malformed cursors beyond basic validation)
- Concurrency behavior during active assignment changes
- RowVersion optimistic locking on task updates (no update endpoint tested)
- Measurement submission via HTTP (only schema and linkage inspected)
- External consumer usage (Web/Android clients)
- Deployed database state and real crew usage patterns
- **Database trigger enforcement at runtime:** HasTrigger is EF configuration reference only; actual trigger behavior not tested with invalid mutation attempts
- **SRID 4326 constraint enforcement at runtime:** Configuration inspected (SOURCE_INSPECTED), not tested with invalid SRID data
- Audit logs, outbox records, or other mutation side effects from GET endpoint

**SOURCE_INSPECTED (not runtime tested):**
- `FieldInspectionSession` and `GroundTruthMeasurement` domain entities
- Measurement immutability triggers in SQL Server (HasTrigger is EF configuration reference)
- Session purpose gates (DefectVerification vs ResearchValidation)
- SRID 4326 constraint declaration in EF configuration
- Base64 RowVersion mapping logic in service layer

## 7. Decisions needed

**Q-RF02-04:** Approved Fast Track method/material dossier and numeric thresholds for policy evaluation (blocks F/G policy implementation)

**Q-RF02-07:** PM visibility scope, report-to-defect mapping, out-of-warranty routing, public image/PII projection (blocks F/G PM review workflow)

**Q-RF02-05:** May affect survey-derived measurement evidence if GIS/segment changes alter provenance

## 8. Next steps

Characterization C01 is complete with correction-02 verified and R01 assessment complete. Remaining RF-10-07 work:
- **R01 assessment:** Complete with RETAIN recommendation. No extraction justified; authorization/projection diverges from other services, duplication is superficial, API contract stability concerns.
- **F/G workflows:** PM batch/reminder, safety actions, policy evaluation, repair acceptance (require approved Q-RF02-04 and Q-RF02-07 decisions)
- **Measurement submission:** If HTTP submission endpoint exists or planned, characterize separately

Parent RF-10-07 remains Partial until F/G decisions and implementation are complete.
