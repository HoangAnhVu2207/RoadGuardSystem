> V9 adoption: G02-G06 collections are owner-approved and implemented. See [FE-APPROVED-COLLECTIONS.md](FE-APPROVED-COLLECTIONS.md) for exact routes/wire, authority, evidence and limits. Older collection-pending language below is historical. Canonical241 coverage unchanged; eleven new GETs are additive. A1/B2/C1 remain gated. No FE_READY or FULL_CONTRACT promotion.

> Task33 CURRENT UPDATE (supersedes conflicting historical Task30–32 statements below): G01 SegmentSet collection approved by owner and implemented with existing detail authority, status filter and keyset pagination. New isolated deployments enable internal Development Mock AI and create A/B geometry/survey/dataset plus road/empty fixture results. External AI callback/issuer remains blocked. See FE-DISCOVERY-WORKFLOWS.md, FE-MOCK-AI-GUIDE.md, FE-241-API-COVERAGE-MATRIX.md and FE-FEATURE-AVAILABILITY.md. Historical counts below are not fresh Task33 evidence. Rendered243 = canonical241 + G01 + operational /ready.

# FE GET collection authorization and discovery â€” Task32A

CURRENT_VERIFIED source HEAD `dd94aa697c411bb11d305973580055216f65ba73`, working tree (206 inherited paths). Public business API inventory: 241 operations, 97 GET operations. Packaged OpenAPI additionally includes operational GET /ready: 242 total operations, 98 GET. This audit does not promote ledger runtime/full-contract coverage.

22 direct collection/history GETs are tabulated, plus 3 embedded discovery reads. Summary/statistics/coverage snapshots are retained in the all-GET appendix, not counted as independent resource catalogs. API aliases are separate documented routes, not additional business capability.

Roles are actual codes PM, SUPERVISOR, OPERATOR, CREW, REPORTER. Supervisor can have global authority where explicitly implemented; do not infer project membership is mandatory for that actor. Anonymous receives authentication challenge; authenticated denial codes vary by endpoint. User/role/session guards remain required, including on reads. SQL authority is not delegated to FE.

## Collection matrix

| API_ID | METHOD / ROUTE | PURPOSE | ROLE / PRINCIPAL | PROJECT / RESOURCE / DATA_FILTER | RESULT_FIELDS | TEST / STATUS |
|---|---|---|---|---|---|---|
| API-018 | GET `/api/v1/cases` | Incident cases | PM current project; Supervisor global. No-project query is Supervisor unassigned intake ONLY. | Exact projectId/status; CreatedAt,Id descending keyset; pageSize 1..100. Query: `projectId, status, pageSize, cursor` | items, nextCursor | Source verified; runtime dimensions below. Tests: historical assertion locators in acceptance ledger; not executed here |
| API-037 | GET `/api/v1/me/inspection-tasks` | My FIELD assignments | RepairCrew only, active assignment AND current project membership; current actor. | DueAt,Id ascending; limit1..100/cursor; post-page scope filtering can produce empty page with continuation. Query: `cursor, limit` | items, nextCursor, asOf | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Inspections/V2P1063InspectionTaskListTests.cs` (NOT_RUN here) |
| API-038 | GET `/api/v1/me/survey-tasks` | My survey tasks | DroneOperator only, current assignment, current project membership checked per item. | RequestedAt,Id keyset limit1..100; post-page scope filtering. Query: `cursor, limit` | items, nextCursor, asOf | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Surveys/Anh01SurveyCorrectionTests.cs`; `tests/RoadGuardSystem.ApiTests/Surveys/Anh01SurveyFlowTests.cs`; `tests/RoadGuardSystem.ApiTests/Surveys/P122SurveyPlanningTests.cs`; `tests/RoadGuardSystem.ApiTests/Surveys/P2SurveyV2ApiTests.cs`; `tests/RoadGuardSystem.ApiTests/Surveys/Rf1003SameRowSurveyTests.cs` |
| API-039 | GET `/api/v1/notifications` | Notification inbox | Recipient only AND current source-specific project/duty authority. Supervisor global project authority does not grant another recipient inbox. | OccurredAtUtc,Id descending keyset; limit1..100; unknown source proofs fail closed. Query: `cursor, limit` | items, nextCursor, asOf | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Notifications/H6NotificationOperationsHttpTests.cs` |
| API-049 | GET `/api/v1/projects` | Accessible projects | Supervisor global, PM active valid membership; Operator/Crew additionally current relevant assignment/status/binding. Reporter denied. | search<=255; status PLANNING/ACTIVE/CLOSED; limit1..100; CreatedAt,Id keyset. Query: `search, status, cursor, limit` | items, nextCursor, asOf | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Projects/ProjectListTests.cs` |
| API-056 | GET `/api/v1/projects/{projectId}/baseline-selections` | Current baseline selections | PM/Supervisor with current project authority. | project and optional segmentSetId; current records only; aggregate items, no paging contract. Query: `segmentSetId` | items | Source verified; runtime dimensions below. Tests: historical assertion locators in acceptance ledger; not executed here |
| API-061 | GET `/api/v1/projects/{projectId}/candidates` | Matching candidates | PM only; current project plus source authorization/version lock. FIELD_OBSERVATION source returns source_not_ready. | sourceKind/sourceId required; expand; pageSize1..200; cursor bound to source/version/target hash. Query: `sourceKind, sourceId, expand, pageSize, cursor` | source, geometryVersion, algorithmVersion, items, nextCursor | Source verified; runtime dimensions below. Tests: historical assertion locators in acceptance ledger; not executed here |
| API-062 | GET `/api/v1/projects/{projectId}/clocks` | Deadline clocks | Current project/source duty authority, recipient/assignee restrictions in repository; not arbitrary user clocks. | limit1..100; JSON actor/project/role-bound cursor; stale cursor409; due time/id. Query: `cursor, limit` | status, items, continuation | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Notifications/H6NotificationOperationsHttpTests.cs`; `tests/RoadGuardSystem.ApiTests/Projects/H6ProjectLifecycleHttpTests.cs` |
| API-065 | GET `/api/v1/projects/{projectId}/crs-profiles` | CRS profiles | PM current project; Supervisor global. Operator/Crew/Reporter denied. | projectId; code/revision ordering; full bounded-by-project catalog, no pagination/search. Query: `(none documented)` | array item fields: 'id', 'projectId', 'code', 'revision', 'status', 'definition', 'createdBy', 'createdAt' | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Projects/Anh01GeometryWorkflowTests.cs` |
| API-070 | GET `/api/v1/projects/{projectId}/defects` | Defects | PM only with current membership; Supervisor is not implicitly allowed by this service. | project/status/type/segmentId; pageSize1..200; Id keyset. Active source links resolve segment filter; per-row projection queries. Query: `status, type, segmentId, pageSize, cursor` | items, nextCursor | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Defects/Huy01DefectConcurrencySchemaTests.cs`; `tests/RoadGuardSystem.ApiTests/Defects/Rf1006ReporterDefectCharacterizationTests.cs`; `tests/RoadGuardSystem.ApiTests/Repairs/LD08DefectStatisticsHttpTests.cs`; `tests/RoadGuardSystem.ApiTests/Reports/Huy01CaseDefectReadSqlTests.cs` |
| API-078 | GET `/api/v1/projects/{projectId}/field-inspection-tasks` | Project FIELD tasks | PM/Supervisor; RepairCrew current exact active assignment plus project authority. | Exact project; afterId/limit1..100; Id sort; per-task view queries. Query: `afterId, limit` | items, nextCursor | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Inspections/H3FieldInspectionWorkflowHttpTests.cs` |
| API-088 | GET `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/history` | FIELD task history | Same current task/project authority as FIELD detail; crew exact assignment. | Known project/task parent; task-scoped history aggregate, no global list. Query: `(none documented)` | assignments, events | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Inspections/H3FieldInspectionWorkflowHttpTests.cs` |
| API-097 | GET `/api/v1/projects/{projectId}/geometry-location-impacts` | Geometry location impacts | PM/Supervisor read with current project authority. | previousRouteVersionId/newRouteVersionId optional exact filters; project aggregate; no cursor contract. Query: `previousRouteVersionId, newRouteVersionId` | array item fields: 'id', 'projectId', 'previousRouteVersionId', 'newRouteVersionId', 'references', 'recordedAt' | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Projects/H2PavementWorkflowHttpTests.cs` |
| API-105 | GET `/api/v1/projects/{projectId}/labels` | Training labels | PM only, current project and source/case authorization. | project pageSize1..200; GUID keyset cursor. Query: `pageSize, cursor` | items, nextCursor | Source verified; runtime dimensions below. Tests: historical assertion locators in acceptance ledger; not executed here |
| API-112 | GET `/api/v1/projects/{projectId}/members` | Project members / candidate pool | PM/Supervisor current authority; includeHistory Supervisor only. Candidate pool is not final assignment eligibility. | search/role/status/includeHistory; limit1..100; membershipId keyset. No contact PII. Query: `search, role, status, includeHistory, limit, cursor` | items, nextCursor, asOf | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Projects/StaffMembershipReadHttpTests.cs` |
| API-135 | GET `/api/v1/projects/{projectId}/receiving-requests` | Business receiving requests | Current project authority; non-PM/Supervisor limited to responsible actor; responsibility transfer resolved. | scopeId optional; after GUID; limit1..100; Id sort. Query: `scopeId, after, limit` | items, continuation | Source verified; runtime dimensions below. Tests: historical assertion locators in acceptance ledger; not executed here |
| API-148 | GET `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/history` | Repair item history | Current repair/project/item authority and binding; not all repair packages. | Known project/package/item path; item history only. Query: `(none documented)` | item, decisions, attempts, reviewRequests | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Repairs/H4RepairExecutionHttpAdmissionTests.cs`; `tests/RoadGuardSystem.ApiTests/Repairs/H4RepairLifecycleHttpAdmissionTests.cs`; `tests/RoadGuardSystem.ApiTests/Repairs/H4RepairPolicyHttpTests.cs`; `tests/RoadGuardSystem.ApiTests/Repairs/H4RepairProducerHttpAdmissionTests.cs`; `tests/RoadGuardSystem.ApiTests/Repairs/H4RepairSafetyHttpAdmissionTests.cs`; `tests/RoadGuardSystem.ApiTests/Repairs/H4RepairWorkflowHttpAdmissionTests.cs` |
| API-168 | GET `/api/v1/projects/{projectId}/reports/items` | Reporting item discovery | PM/Supervisor, active identity/role/current project authority. | metric/from/to/routeVersionId/segmentSetId/segmentIds/pageSize/cursor; metric-dependent dimensions. Query: `metric, from, to, routeVersionId, segmentSetId, segmentIds, cursor, pageSize` | schemaVersion, metric, readAt, items, nextCursor, availability | Source verified; runtime dimensions below. Tests: historical assertion locators in acceptance ledger; not executed here |
| API-170 | GET `/api/v1/projects/{projectId}/reports/timeline` | Reporting timeline | PM/Supervisor active/current project authority. | aggregateType/aggregateId/from/to/pageSize/cursor; audit/reporting timeline, not resource ownership grant. Query: `aggregateType, aggregateId, from, to, cursor, pageSize` | schemaVersion, readAt, items, nextCursor, availability | Source verified; runtime dimensions below. Tests: historical assertion locators in acceptance ledger; not executed here |
| API-198 | GET `/api/v1/projects/{projectId}/route-systems` | Route systems | PM current project; Supervisor global. | Project only, code sort; no pagination/search contract. Query: `(none documented)` | array item fields: 'id', 'projectId', 'code', 'name' | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Projects/Anh01GeometryWorkflowTests.cs` |
| API-215 | GET `/api/v1/reports` | My reporter reports | Reporter owner only. Not PM project intake; use cases for PM. | ReceivedAt,Id descending keyset; pageSize1..100; current receipt authority. Query: `pageSize, cursor` | items, nextCursor | Source verified; runtime dimensions below. Tests: historical assertion locators in acceptance ledger; not executed here |
| API-237 | GET `/api/v1/users` | Privileged staff directory | Supervisor only, active identity/role. PM/Reporter/Operator/Crew denied. | search/role/status; limit1..100; userId keyset; default Reporter excluded; contact fields privileged. Query: `search, role, status, limit, cursor` | items, nextCursor, asOf | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Projects/StaffMembershipReadHttpTests.cs` |
| API-207 | GET `/api/v1/projects/{projectId}/work-package` | Embedded roadSections and warranties | ProjectScopeGuard: Supervisor global; other roles require active valid role-matched project membership. | Exact project; current road versions only; road Code order, warranty date/Id; no cursor. Query: `(none documented)` | projectId, projectCode, name, description, engineeringUtmSrid, status, startDate, endDate, accessRole, rowVersion, roadSections, warranties | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Projects/ProjectListTests.cs` |
| API-194 | GET `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}` | Embedded segments in known Segment Set | PM/Supervisor project authority; validates project/road/version/set parent chain. | Known setId required; sequence sort; set status/definition plus geometry/offset fields; no cursor. Query: `(none documented)` | id, routeVersionId, status, definition, geometryHash, segments, publishedBy, publishedAt, version, metadataStatus, missingMetadata | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Projects/Anh01GeometryWorkflowTests.cs` |
| API-104 | GET `/api/v1/projects/{projectId}/geometry-package` | Geometry package with embedded set segments | PM/Supervisor; assigned Operator only for current assigned geometry and project. | routeVersionId AND segmentSetId required; both nonempty. No general discovery. Query: `routeVersionId, segmentSetId` | schemaVersion, projectId, route, segmentSet | Source verified; runtime dimensions below. Tests: `tests/RoadGuardSystem.ApiTests/Projects/AnhHuyGeometrySourceTests.cs` |

## Source chain and FE action map

### API-018 â€” W03: Incident cases
- FE: Project list -> cases?projectId -> caseId -> case detail/conclusion
- Service: `RoadGuardSystem.Services/Implementations/Cases/CaseWorkflowService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Cases/CaseWorkflowRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-037 â€” W06: My FIELD assignments
- FE: My tasks -> taskId/projectId -> FIELD detail; no arbitrary project tasks
- Service: `RoadGuardSystem.Services/Implementations/Inspections/InspectionTaskQueryService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Inspections/InspectionTaskReadRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-038 â€” W05: My survey tasks
- FE: My surveys -> taskId -> task/work-package -> dataset/assessment
- Service: `RoadGuardSystem.Services/Implementations/Surveys/SurveyV2Service.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyV2PersistenceService.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-039 â€” W10: Notification inbox
- FE: Inbox -> notificationId -> detail/scope -> authorized source; no external delivery claim
- Service: `RoadGuardSystem.Services/Implementations/Messaging/NotificationService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Messaging/H6ProtectedNotificationRepository.Scope.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-049 â€” W02: Accessible projects
- FE: Choose project -> projectId -> work-package/roads
- Service: `RoadGuardSystem.Services/Implementations/Projects/ProjectListService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Projects/ProjectListReadModel.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-056 â€” W05: Current baseline selections
- FE: Project/known set -> baseline selections -> selection detail
- Service: `RoadGuardSystem.Services/Implementations/Surveys/SurveyAssessmentService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Surveys/SurveyAssessmentRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-061 â€” W04: Matching candidates
- FE: Case/AI source ID -> candidates -> decision -> decision detail
- Service: `RoadGuardSystem.Services/Implementations/Defects/CandidateMatchingService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Defects/CandidateDecisionRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-062 â€” W10: Deadline clocks
- FE: Project -> clocks -> clock action/receiving request; not a Fast Track catalog
- Service: `RoadGuardSystem.Services/Implementations/Messaging/H6NotificationReadService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Messaging/H6ProtectedNotificationRepository.Operations.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-065 â€” W02: CRS profiles
- FE: Project -> profiles -> profileId -> geometry draft input
- Service: `RoadGuardSystem.Services/Implementations/Projects/GeometryWorkflowService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Projects/GeometryWorkflowPersistenceService.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-070 â€” W04: Defects
- FE: Project -> defects -> defectId -> assess/repair flow; segment selector discovery blocked separately
- Service: `RoadGuardSystem.Services/Implementations/Defects/DefectWorkflowService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Defects/DefectWorkflowRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-078 â€” W06: Project FIELD tasks
- FE: Project -> FIELD tasks -> taskId -> history/evidence/geometry
- Service: `RoadGuardSystem.Services/Implementations/Inspections/FieldInspectionWorkflowService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Inspections/FieldInspectionWorkflowRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-088 â€” W06: FIELD task history
- FE: FIELD list -> taskId -> history
- Service: `RoadGuardSystem.Services/Implementations/Inspections/FieldInspectionWorkflowService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Inspections/FieldInspectionWorkflowRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-097 â€” W02: Geometry location impacts
- FE: Project -> impacts -> impactId -> detail/decision
- Service: `RoadGuardSystem.Services/Implementations/Projects/PavementWorkflowService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Projects/PavementWorkflowRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-105 â€” W04: Training labels
- FE: Project -> labels -> labelId -> revision/review
- Service: `RoadGuardSystem.Services/Implementations/Labels/TrainingLabelService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Labels/TrainingLabelRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-112 â€” W01: Project members / candidate pool
- FE: Project -> members -> userId -> assignment command revalidates task-specific eligibility
- Service: `RoadGuardSystem.Services/Implementations/Staff/StaffReadService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Staff/StaffReadRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-135 â€” W10: Business receiving requests
- FE: Project -> receiving requests -> requestId -> detail/accept
- Service: `RoadGuardSystem.Services/Implementations/Messaging/BusinessDutyService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Messaging/BusinessDutyRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-148 â€” W07: Repair item history
- FE: Known repair item or assigned FIELD repairContext -> history
- Service: `RoadGuardSystem.Services/Implementations/Repairs/RepairWorkflowService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Repairs/RepairWorkflowRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-168 â€” W10: Reporting item discovery
- FE: Project/report metric -> items -> typed sourceId -> source detail; not export-job listing
- Service: `RoadGuardSystem.Services/Implementations/Reporting/ReportingService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Reporting/ReportingRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-170 â€” W10: Reporting timeline
- FE: Known aggregate -> timeline
- Service: `RoadGuardSystem.Services/Implementations/Reporting/ReportingService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Reporting/ReportingRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-198 â€” W02: Route systems
- FE: Project -> route systems -> systemId -> road geometry
- Service: `RoadGuardSystem.Services/Implementations/Projects/GeometryWorkflowService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Projects/GeometryWorkflowPersistenceService.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-215 â€” W03: My reporter reports
- FE: My reports -> reportId -> detail -> evidence download (re-authorized)
- Service: `RoadGuardSystem.Services/Implementations/Reports/ReporterLifecycleService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Reports/ReporterLifecycleRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-237 â€” W01: Privileged staff directory
- FE: Supervisor directory -> userId -> privileged user detail
- Service: `RoadGuardSystem.Services/Implementations/Staff/StaffReadService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Staff/StaffReadRepository.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-207 â€” W02: Embedded roadSections and warranties
- FE: Project -> work-package.roadSections -> roadSectionId/currentVersionId; NO_NEW_GET_NEEDED for road list
- Service: `RoadGuardSystem.Services/Implementations/Projects/ProjectWorkPackageService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Projects/ProjectWorkPackageReadModel.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-194 â€” W02: Embedded segments in known Segment Set
- FE: Known setId -> set.segments; missing fresh-session Segment Set discovery
- Service: `RoadGuardSystem.Services/Implementations/Projects/GeometryWorkflowService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Projects/GeometryWorkflowPersistenceService.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.
### API-104 â€” W02: Geometry package with embedded set segments
- FE: Known task/work-package scope -> geometry-package; cannot find unknown setId
- Service: `RoadGuardSystem.Services/Implementations/Projects/GeometryWorkflowService.cs`; repository: `RoadGuardSystem.Repositories/Implementations/Projects/GeometryWorkflowPersistenceService.cs`.
- Field contract: use exact 200 schema in `openapi.json`; where untyped, the source DTO and real examples are authoritative implementation evidence, not a fabricated Swagger schema.

## Runtime verification limits

Task32A executes its own disposable-profile HTTP/SQL harness and focused existing xUnit ProjectList/Staff/Geometry scenarios. Consult FE-GET-LIST-EXAMPLES and external evidence index for executed assertions, exit codes and SQL postconditions. Static rows and historical ledger tests are not new runtime PASS. Empty fixtures do not verify populated lifecycle/source authorization. No ALL APIs/FULL_CONTRACT/FE_READY claim.

Project keyset uses CreatedAt/Id; member keyset uses membershipId. FIELD and Survey current membership filtering occurs after page selection, so an empty page may still have a continuation. FE must follow nextCursor until null, not stop at items.length==0. Cursor format differs across APIs; preserve returned value verbatim.

Cases/Defects/FIELD projections can perform per-record queries; bounded page limits reduce exposure but populated-load query budgets are NOT_RUN. No unrelated query refactor was authorized. Full project CRS/system/work-package aggregates have no pagination contract; do not invent limit/search parameters.

## W01â€“W10 navigation / blockers

| Workflow | Collection / select / detail | Remaining discovery boundary |
|---|---|---|
| W01 Identity | Supervisor users237 -> user238; self035/profile047; projects049 | No public all-user directory for PM. Use members112 for candidate pool; final command validates eligibility. |
| W02 Project/geometry | projects049 -> work-package207 roads -> geometry192; CRS065/system198 -> known set194 | Segment Set IDs cannot be freshly enumerated. BLOCKED_OWNER_POLICY proposal G01. |
| W03 Reporter/cases | own reports215 -> report217/evidence218; PM cases018 -> case019 | PM list is scoped case intake, not Reporter owner list. Supervisor no-project means unassigned intake. |
| W04 Defects/candidates/labels | defects070 ->071; source061 -> decision060; labels105 ->107 | Defect segment selector depends on G01; FIELD source candidate not ready. |
| W05 Survey/AI | Operator tasks038 ->226/workpackage225 -> dataset026/assessment028; baselines056 ->058 | PM project-wide survey/processing/AI run rediscovery absent; issuer remains blocked. |
| W06 FIELD | my037 or project078 ->080/history088/evidence085 | Current assignment/membership required; no fabricated assigned approved fixtures. |
| W07 Repair | assigned FIELD detail may expose repairContext -> item141/history148 | No general package/item discovery catalog; reporting metric not proof of equivalent contract. G02. |
| W08 Fast Track | command result -> FIELD/repair IDs; receiving135 ->136 | No scoped Fast Track catalog; B2 unresolved parameters remain G03. |
| W09 Offline | issued package124/snapshot128/artifact114/device117/grant120/batch115 by known ID | Not global list. Device/package rediscovery requirement must be scoped if FE needs history. |
| W10 Messaging/reporting | inbox039 ->040/scope042; clocks062; report168/169/170 | Export create075/manifest077/content076 by known exportId, no job collection. Retention known IDs only; G04/G06. |

## All GET routes (including non-collection reads)

This appendix reconciles every ledger GET with the current packaged OpenAPI. It is an inventory, not an authorization or runtime claim for detail APIs outside the audited collection chains.

| API | Workflow | Route | Kind | Controller source evidence |
|---|---|---|---|---|
| API-013 | W01 | `/api/v1/auth/web/csrf` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/WebAuthController.cs` |
| API-017 | W01 | `/api/v1/auth/web/session` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/WebAuthController.cs` |
| API-018 | W03 | `/api/v1/cases` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/CasesController.cs` |
| API-019 | W03 | `/api/v1/cases/{caseId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/CasesController.cs` |
| API-025 | W05 | `/api/v1/datasets/{datasetId}/coverage` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs` |
| API-026 | W05 | `/api/v1/datasets/{id}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs` |
| API-028 | W05 | `/api/v1/datasets/{id}/assessments/{assessmentId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/SurveyAssessmentsController.cs` |
| API-029 | W05 | `/api/v1/files/{fileId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/UploadsController.cs` |
| API-030 | W05 | `/api/v1/files/{fileId}/content` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/UploadsController.cs` |
| API-035 | W01 | `/api/v1/me` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/MeController.cs` |
| API-037 | W06 | `/api/v1/me/inspection-tasks` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/InspectionTasksController.cs` |
| API-038 | W05 | `/api/v1/me/survey-tasks` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs` |
| API-039 | W10 | `/api/v1/notifications` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/NotificationsController.cs` |
| API-040 | W10 | `/api/v1/notifications/{notificationId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/NotificationsController.cs` |
| API-042 | W10 | `/api/v1/notifications/{notificationId}/scope` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/H6NotificationOperationsController.cs` |
| API-045 | W05 | `/api/v1/processing-jobs/{jobId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs` |
| API-047 | W01 | `/api/v1/profile` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ProfileController.cs` |
| API-049 | W02 | `/api/v1/projects` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/ProjectsController.cs` |
| API-052 | W05 | `/api/v1/projects/{projectId}/ai-candidates/{detectionId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/AiMockRunsController.cs` |
| API-054 | W05 | `/api/v1/projects/{projectId}/ai-mock-runs/{runId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/AiMockRunsController.cs` |
| API-055 | W05 | `/api/v1/projects/{projectId}/ai-mock-runs/{runId}/result` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/AiMockRunsController.cs` |
| API-056 | W05 | `/api/v1/projects/{projectId}/baseline-selections` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/SurveyAssessmentsController.cs` |
| API-058 | W05 | `/api/v1/projects/{projectId}/baseline-selections/{id}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/SurveyAssessmentsController.cs` |
| API-060 | W04 | `/api/v1/projects/{projectId}/candidate-decisions/{decisionId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/CandidatesController.cs` |
| API-061 | W04 | `/api/v1/projects/{projectId}/candidates` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/CandidatesController.cs` |
| API-062 | W10 | `/api/v1/projects/{projectId}/clocks` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/H6NotificationOperationsController.cs` |
| API-065 | W02 | `/api/v1/projects/{projectId}/crs-profiles` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs` |
| API-067 | W02 | `/api/v1/projects/{projectId}/crs-profiles/{profileId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs` |
| API-068 | W10 | `/api/v1/projects/{projectId}/defect-statistics` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/DefectStatisticsController.cs` |
| API-070 | W04 | `/api/v1/projects/{projectId}/defects` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/DefectsController.cs` |
| API-071 | W04 | `/api/v1/projects/{projectId}/defects/{defectId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/DefectsController.cs` |
| API-075 | W10 | `/api/v1/projects/{projectId}/exports/{exportId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ExportsController.cs` |
| API-076 | W10 | `/api/v1/projects/{projectId}/exports/{exportId}/content` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ExportsController.cs` |
| API-077 | W10 | `/api/v1/projects/{projectId}/exports/{exportId}/manifest` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ExportsController.cs` |
| API-078 | W06 | `/api/v1/projects/{projectId}/field-inspection-tasks` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs` |
| API-080 | W06 | `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs` |
| API-085 | W06 | `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence/{fileId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs` |
| API-086 | W06 | `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/evidence/{fileId}/content` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs` |
| API-087 | W06 | `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/geometry` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs` |
| API-088 | W06 | `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/history` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs` |
| API-094 | W06 | `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/start-origin` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs` |
| API-096 | W06 | `/api/v1/projects/{projectId}/field-inspection-tasks/{taskId}/submissions/{submissionId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/FieldInspectionWorkflowController.cs` |
| API-097 | W02 | `/api/v1/projects/{projectId}/geometry-location-impacts` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs` |
| API-099 | W02 | `/api/v1/projects/{projectId}/geometry-location-impacts/{impactId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs` |
| API-102 | W02 | `/api/v1/projects/{projectId}/geometry-map-publications/{publicationId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs` |
| API-103 | W02 | `/api/v1/projects/{projectId}/geometry-map-publications/{publicationId}/layers/{layer}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs` |
| API-104 | W02 | `/api/v1/projects/{projectId}/geometry-package` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs` |
| API-105 | W04 | `/api/v1/projects/{projectId}/labels` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/TrainingLabelsController.cs` |
| API-107 | W04 | `/api/v1/projects/{projectId}/labels/{labelId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/TrainingLabelsController.cs` |
| API-109 | W02 | `/api/v1/projects/{projectId}/lifecycle` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ProjectLifecycleController.cs` |
| API-112 | W01 | `/api/v1/projects/{projectId}/members` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/StaffDirectoryController.cs` |
| API-114 | W09 | `/api/v1/projects/{projectId}/offline/artifacts/{artifactId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs` |
| API-115 | W09 | `/api/v1/projects/{projectId}/offline/batches/{batchId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs` |
| API-117 | W09 | `/api/v1/projects/{projectId}/offline/devices/{registrationId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs` |
| API-120 | W09 | `/api/v1/projects/{projectId}/offline/grants/{grantId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs` |
| API-123 | W09 | `/api/v1/projects/{projectId}/offline/origins/{originId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs` |
| API-124 | W09 | `/api/v1/projects/{projectId}/offline/packages/{packageId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs` |
| API-128 | W09 | `/api/v1/projects/{projectId}/offline/snapshots/{snapshotId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/OfflineWorkflowController.cs` |
| API-131 | W02 | `/api/v1/projects/{projectId}/pavement-layouts/{layoutId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/PavementWorkflowController.cs` |
| API-133 | W01 | `/api/v1/projects/{projectId}/primary-project-manager` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/StaffDirectoryController.cs` |
| API-135 | W10 | `/api/v1/projects/{projectId}/receiving-requests` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/BusinessDutyController.cs` |
| API-136 | W10 | `/api/v1/projects/{projectId}/receiving-requests/{requestId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/BusinessDutyController.cs` |
| API-141 | W07 | `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/RepairsController.cs` |
| API-146 | W08 | `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/eligibility` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/RepairEligibilityController.cs` |
| API-148 | W07 | `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/history` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/RepairsController.cs` |
| API-152 | W07 | `/api/v1/projects/{projectId}/repair-packages/{packageId}/items/{itemId}/safety-measures/{measureId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/RepairSafetyController.cs` |
| API-163 | W08 | `/api/v1/projects/{projectId}/repair-policies/drafts/{draftId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/RepairPoliciesController.cs` |
| API-166 | W08 | `/api/v1/projects/{projectId}/repair-policies/revisions/{revisionId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/RepairPoliciesController.cs` |
| API-168 | W10 | `/api/v1/projects/{projectId}/reports/items` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/ReportingController.cs` |
| API-169 | W10 | `/api/v1/projects/{projectId}/reports/summary` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ReportingController.cs` |
| API-170 | W10 | `/api/v1/projects/{projectId}/reports/timeline` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/ReportingController.cs` |
| API-172 | W10 | `/api/v1/projects/{projectId}/retention/evaluations/{id}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/RetentionController.cs` |
| API-173 | W10 | `/api/v1/projects/{projectId}/retention/files/{fileId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/RetentionController.cs` |
| API-175 | W08 | `/api/v1/projects/{projectId}/road-coverage` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/RoadCoverageController.cs` |
| API-178 | W02 | `/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs` |
| API-182 | W02 | `/api/v1/projects/{projectId}/road-geometry-drafts/{draftId}/readiness` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs` |
| API-185 | W02 | `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/geometry-drafts/{draftId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs` |
| API-192 | W02 | `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/geometry` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs` |
| API-194 | W02 | `/api/v1/projects/{projectId}/road-sections/{roadSectionId}/versions/{routeVersionId}/segment-sets/{setId}` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs` |
| API-198 | W02 | `/api/v1/projects/{projectId}/route-systems` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/GeometryWorkflowController.cs` |
| API-206 | W10 | `/api/v1/projects/{projectId}/weekly-digests/{digestId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/H6NotificationOperationsController.cs` |
| API-207 | W02 | `/api/v1/projects/{projectId}/work-package` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/ProjectWorkPackagesController.cs` |
| API-208 | W03 | `/api/v1/reporter-evidence/files/{fileId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ReporterEvidenceController.cs` |
| API-209 | W03 | `/api/v1/reporter-evidence/files/{fileId}/content` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ReporterEvidenceController.cs` |
| API-210 | W03 | `/api/v1/reporter-evidence/publications/{publicationId}/reports/{reportId}/evidence/{evidenceId}/content` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ReporterEvidenceController.cs` |
| API-212 | W03 | `/api/v1/reporter-evidence/uploads/{uploadId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ReporterEvidenceController.cs` |
| API-215 | W03 | `/api/v1/reports` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/ReportsController.cs` |
| API-217 | W03 | `/api/v1/reports/{reportId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ReportsController.cs` |
| API-218 | W03 | `/api/v1/reports/{reportId}/evidence/{evidenceId}/download` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ReportsController.cs` |
| API-221 | W10 | `/api/v1/retention/holds/{id}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/RetentionController.cs` |
| API-223 | W05 | `/api/v1/survey-plans/{id}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs` |
| API-225 | W05 | `/api/v1/survey-tasks/{id}/work-package` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs` |
| API-226 | W05 | `/api/v1/survey-tasks/{taskId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/SurveyV2Controller.cs` |
| API-234 | W05 | `/api/v1/uploads/{uploadId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/UploadsController.cs` |
| API-237 | W01 | `/api/v1/users` | collection/embedded audited above | `RoadGuardSystem.API/Controllers/StaffDirectoryController.cs` |
| API-238 | W01 | `/api/v1/users/{userId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/UsersController.cs` |
| API-241 | W05 | `/api/v1/validation-runs/{runId}` | detail / aggregate / status / content; no independent collection claim | `RoadGuardSystem.API/Controllers/ProcessingV2Controller.cs` |


## Explicit support classifications and execution limits

| Scope | Classification | Evidence level in Task32A |
|---|---|---|
| All 22 existing direct collection/history GETs | EXISTING_GET_VERIFIED for implementation presence and scoped source predicates | Source audit only unless listed below; this label does not mean all contracts executed. |
| Roads embedded in API-207 | NO_NEW_GET_NEEDED | Real HTTP positive, cross-project deny and membership revocation; SQL road/version/project parent check. |
| Known-set segments API-194 / package API-104 | EXISTING_GET_VERIFIED for known-ID reads; MISSING_GET_REQUIRED G01 for set discovery | Existing SQL geometry regression selected for known-set path. API-104 missing setId returns 400; negative validation is not positive segment discovery. |
| FIELD API-037 / Survey API-038 | EXISTING_GET_NEEDS_FILTER (UX/performance follow-up, not proven disclosure) | Current membership checked after keyset page; may return empty items with continuation. Follow nextCursor, not items length. |
| Segment Set / Repair / Fast Track / Export discovery | MISSING_GET_REQUIRED G01–G04; BLOCKED_OWNER_POLICY for new public contract adoption | Four proposed capabilities, not four implemented endpoints. Fast Track policy parameters remain gated. |
| Staff candidate pool | NO_NEW_GET_NEEDED for browsing current members | Assignment-specific eligibility is revalidated by commands; directory is Supervisor-only. |

No confirmed authorization disclosure was reproduced in this bounded run. This is not an assertion that every list has full negative runtime coverage.

Fresh source/ZIP HTTP harness exercises API-049,207,112,237,215,217,018,070,065,198,039,192 and negative API-104. Positive collection responses for Case, Defect and Notification are empty fixture results; producer/nonempty/indirect privacy tests are NOT_RUN in this task. Project/member privilege, cross-project and revocation checks do not imply equivalent coverage for every other row.

Focused existing SQL suite: ProjectListTests, StaffMembershipReadHttpTests and Anh01GeometryWorkflowTests; exact counts/results in external Task32A evidence and MASTER-HANDOFF. Test locators elsewhere in this matrix are historical/source locators, not newly executed PASS claims. No acceptance-ledger FULL_CONTRACT or runtime totals are promoted by this audit.

CURRENT_VERIFIED focused SQL regression: 75 unique .NET cases PASS / 0 FAIL / 0 SKIP / 0 ABORT, elapsed test execution 1m09s; source disposable HTTP harness 54 assertions PASS. Full emitted TRX and test names are stored in Task32A external evidence. Source/ZIP reruns of the same assertions are not additional unique .NET tests.
