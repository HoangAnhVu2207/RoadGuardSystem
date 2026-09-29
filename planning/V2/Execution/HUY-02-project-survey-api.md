# HUY-02 - Project, route, survey and dataset API

- Owner/branch: Huy / huy
- deliveryStatus: TODO
- contractStatus: PROPOSED_DELTA
- implementationStatus: NEEDS_REPO_CHECK
- verificationStatus: NOT_RUN
- dependencyType: contract
- sourceCheckpoint: record HEAD and canonical OpenAPI hash at task start

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

## Completion history
- TODO until all gates pass.



