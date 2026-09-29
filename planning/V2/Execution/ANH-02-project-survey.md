# ANH-02 - Project, route, survey and dataset persistence

- Owner/branch: Anh / anh
- deliveryStatus: TODO
- contractStatus: PROPOSED_DELTA
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: data-fixture
- sourceCheckpoint: record HEAD and canonical OpenAPI hash at task start

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
Build Repositories and IntegrationTests. Run focused project membership, survey scope/concurrency, dataset, file and spatial tests selected from changed symbols; record fresh binaries and SQL version. Acceptance requires durable SQL evidence, no cross-project leakage and HUY-02 handoff VERIFIED or NO_CHANGE_NEEDED.

## Completion history
- TODO until all gates pass.





