# HUY-02 - Project, route, survey and dataset API

- Owner/branch: Huy / huy
- deliveryStatus: PARTIAL
- contractStatus: PROPOSED_DELTA
- implementationStatus: CURRENT_VERIFIED_FOR_EXISTING_SLICES
- verificationStatus: PARTIAL_FOCUSED_API_SQL
- dependencyType: contract
- sourceCheckpoint: HEAD d2338dcffd8838198c2b50ee369b9678e7e06690; canonical OpenAPI SHA-256 ADA7F48F522C0C0DBDACE21F483224A00FC4C76264A9A61D12F3E2211ECCE665

## Task goal
Implement or verify scoped API behavior for project/membership, route/survey planning, assignment, upload completion, dataset submission and coverage. The API must reflect durable repository facts and keep planning, quality, baseline and coverage states separate.

## Business context
PM operates within a project membership scope. A submitted file is not automatically a confirmed dataset; unknown position/quality remains UNKNOWN. Route versions and survey assignments must be concurrency-safe.

## Operation trace
V2-P1-016..023, V2-P2-009..029, V2-P2-053..055 and V2-P2-059. Consume ANH-02 facts; do not access DbContext directly.

## Sources to read
Read the operation cards, FR-04..08/27..30, BR-01/02/20/40/41, ERD_Project_Route, ERD_Survey_Dataset, DD 3.2-3.3a/9, permission model, API/error specs, SQ-01..03, state machines, decisions D14-D22/D38-D39A and ANH-02 handoff.

## In scope
- Create contract/matrix per operation for actor, project scope, input, status, error, If-Match/idempotency and durable effect.
- Fix only Services/DTO/controllers and tests for wrong project/actor, stale version, duplicate replay, assignment state, upload/session and UNKNOWN coverage.
- Keep pagination/filtering and response envelopes consistent with current convention.
- Update API.http/Postman and run real HTTP smoke including durable SQL result.
- Record missing repository facts to ANH-02 through the handoff ledger.

## Out of scope
- Geometry/CRS algorithms, new route schema, migrations, storage/provider deployment, AI callbacks, defect or repair decisions.
- Reopening historical DONE cards solely because ownership changed.

## Exact files and hotspots
Services/Projects and Surveys, DTOs/Projects/Surveys/Files, project/survey/file controllers, API.http, Postman and API/unit tests. Shared DI/errors/OpenAPI are single-writer files.

## Stop conditions
Stop on current-versus-target route mismatch, absent file bytes, unresolved coverage semantics, schema change need or contract-lock mismatch. Use PARTIAL with affected operations.

## Verification and acceptance
Build changed Services/API/ApiTests fresh. Run focused project/survey/dataset API tests and non-empty filters, then HTTP smoke for success, wrong scope, stale/replay and durable effect. Acceptance requires ANH-02 handoff VERIFIED or NO_CHANGE_NEEDED.

## Source evidence - 2026-09-30 receiver review

| Label | Exact source / heading or ID | Evidence used |
|---|---|---|
| CURRENT_VERIFIED | `RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs` (`CreatePlanAsync`, `CreateTaskAsync`, `MutateTaskAsync`, `SubmitDatasetAsync`, `GetDatasetCoverageAsync`) | Service validates actor role, project scope, route/scope input, idempotency and If-Match; it maps repository status facts to DTO/service results. |
| CURRENT_VERIFIED | `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs`, `SurveyPlanningController.cs`, `UploadsController.cs`, `InspectionTasksController.cs` | Existing routes bind request/headers, extract actor and map `Forbidden`, `NotFound`, `Replayed`, conflict and stale results without DbContext access. |
| CURRENT_VERIFIED | `tests/RoadGuardSystem.ApiTests/Surveys/P2SurveyV2ApiTests.cs`, `P122SurveyPlanningTests.cs`; `tests/RoadGuardSystem.IntegrationTests/Surveys/P2V2SurveyScopeConcurrencyTests.cs`, `P230FlightSurveyFileSchemaTests.cs` | Fresh survey API 1/1, SQL root-scope 2/2 and file 5/5 pass on `d2338dc`; Unit/architecture suite also passed 162/162. |
| TARGET_DOCUMENTED | `planning/V2/Person_1/V2-P1-016..023`, `planning/V2/Person_2/V2-P2-009..029,053..055,059`; FR-04..08/27..30; BR-01/02/20/40/41; SQ-01..03 | Project scope, route-version anchor, assignment/replay and separate upload/dataset/quality/coverage/baseline rules. |
| NOT_ENABLED | `docs/design/**`, candidate baseline migration and target branch/slab/per-band coverage entities | `docs/design` and candidate baseline are absent from the handoff commit; no schema or geometry/provider claim is made. |

## Contract checkpoint for routes actually consumed

1. `GET/POST/PATCH /api/v1/projects...` project/membership routes: authenticated PM/Supervisor actor and project scope; validate IDs/payload; success returns safe project/route DTO; forbidden/not-found/stale/idempotency facts map to stable ProblemDetails; repository facts remain scoped; no cross-project projection.
2. `POST/GET /api/v1/projects/{projectId}/survey-plans`, `/survey-tasks` and `GET /api/v1/me/survey-tasks`: PM creates/changes, Operator reads own assignment; validate scope, role, reason and cursor; success returns plan/task with opaque version; replay/conflict/stale map to 409/412; assignment state is not promoted by upload.
3. `POST /api/v1/survey-tasks/{taskId}/accept|decline|cancel|reassign|supplements`: actor is assigned Operator for accept/decline or in-scope PM for management; require Idempotency-Key and If-Match; success returns task DTO; stale/replay/conflict are distinct; audit/idempotency is repository-owned and scope is rechecked.
4. `POST /api/v1/uploads`, part-urls, complete and `GET /api/v1/uploads/{id}`, `/files/{id}`: actor/project/file scope; validate checksum/parts and required preconditions; success returns upload/file metadata; replay/stale/not-found map to stable errors; verified file metadata does not imply dataset quality or coverage.
5. `POST /api/v1/survey-tasks/{taskId}/datasets`: assigned Operator only; require verified file IDs, route scope, device and If-Match/idempotency; success records dataset source facts; replay/stale/conflict are mapped separately; upload/telemetry admission is not baseline or quality confirmation.
6. `GET /api/v1/datasets/{datasetId}/coverage`: in-scope PM/Supervisor or owning Operator; success currently returns explicit `UNKNOWN` position/quality/coverage values when evidence is absent; no PASS/FAIL inference, no per-band baseline claim, and no invented threshold/method.
7. Branch/slab/access-point/baseline/mission-export operations in the trace: remain blocked or absent where repository/schema/geometry facts are not present; do not add a route or persistence policy from the draft card alone.

## Current versus target and outcome

The ANH-02 root `RouteVersionId` scope guard and repository status mapping are consumed without a Service/DTO/API contract change. Dataset coverage remains an explicit UNKNOWN projection and `confirmBaseline`/branch/slab target behavior is not enabled. HUY-02 therefore remains `PARTIAL`.

## Completion history

### 2026-09-30 - PARTIAL (ANH-02 receiver processing)

- Scope/result: Received ANH-02 and confirmed project scope, root route-version validation, survey task replay/rowversion, upload verification and dataset-source mapping are consumed through Service interfaces. No production code change was necessary.
- Files: `planning/V2/Execution/HUY-02-project-survey-api.md`; receiver ledger row in `planning/CROSS_OWNER_HANDOFFS.md`. No Services/DTOs/API source changed.
- Acceptance: Upload verification is kept separate from dataset quality, coverage and baseline; wrong role/project and stale/replay statuses remain explicit. Coverage/baseline/geometry target entities are not claimed.
- Verification: sequential `dotnet build RoadGuardSystem.Services/RoadGuardSystem.dServices.csproj -nologo -v q -clp:ErrorsOnly`, `dotnet build RoadGuardSystem.API/RoadGuardSystem.eAPI.csproj -nologo -v q -clp:ErrorsOnly`, `dotnet build tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj -nologo -v q -clp:ErrorsOnly`, and `dotnet build tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj -nologo -v q -clp:ErrorsOnly` PASS; `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~P2SurveyV2ApiTests"` PASS 1/1; `dotnet test tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~UploadApiTests"` PASS 1/1, required MinIO smoke 1 skipped; `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~P2V2SurveyScopeConcurrencyTests"` PASS 2/2; `dotnet test tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj --no-build --nologo -v minimal --filter "FullyQualifiedName~P230FlightSurveyFileSchemaTests"` PASS 5/5. SQL evidence is current checkout corroboration, not migration rollout proof.
- Handoff outcome: `PROCESSED / NO_CHANGE_NEEDED` for the current project/survey consumer seam; ANH-02 remains `PROCESSED`, not `VERIFIED`, because clean baseline/coverage method and full HTTP durable smoke are incomplete.
- Side effects/risk: no package, migration, schema, live data, provider, commit or push; `docs/design` and candidate migration remain `NOT_ENABLED`.

- Full-solution gate after the receiver follow-up: `dotnet test RoadGuardSystem.slnx --nologo -v minimal` passed Unit `170/170`, API `117/118` with one optional MinIO skip, and Integration `317/317`; no failures. Clean baseline/geometry/coverage and hosted HTTP evidence remain unverified.



