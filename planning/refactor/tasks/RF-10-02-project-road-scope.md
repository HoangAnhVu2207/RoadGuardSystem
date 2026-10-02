# RF-10-02: Project, membership, road/GIS, segment and warranty

> Current execution 2026-10-01: Anh alone owns R/C refactor on `anh`. F/G is unassigned for later development; proposed A/B text below is historical only. Future identity after a common baseline is Huy/A (`huy`), Anh/B (`anh`). See `../10-refactor-slices.md` and `../11-development-plan.md`.

- **Status/checkpoint:** `10-02-C01` DONE locally for current project/membership, road/GIS version and warranty characterization; parent RF-10-02 remains PARTIAL. Evidence: `planning/refactor/10-project-gis-characterization-baseline.md` and `planning/refactor/reports/RF-10-02-C01.md`. Segment/publish F/G remains unstarted. Checkpoint-05 handoff is the review artifact; no production behavior, contract, schema, migration, CI or deployment changed.
- **Goal:** reconcile R04-05/CG05 and Accepted 39A, preserving existing project/road/warranty endpoints while planning missing route/segment operations.
- **In scope:** membership/effective dates, project authority, road section/version, warranty/handover source, GIS version and segment preview/publish. **Out of scope:** assuming device model proves telemetry, deriving construction standard from 100 m, changing survey rows directly.
- **Dependencies/decisions:** RF-10-01 actor/scope facts, RF-09 transition mechanics; real GPX/JSON route/CRS/order/geometry samples and PM/Supervisor approval authority. Pilot 39A permits adjustable 100 m suggestion and remainder keep/merge only.
- **Read first:** R03-05, CG05, decision 39A, FR-04..10/BR-34..37 as reference, project/road/warranty controllers, `ProjectScopeGuard`, `RoadSegmentSet`/`RoadSegment`, EF migrations, spatial tests and Postman gaps CG16.
- **Likely files:** `ProjectsController`, `ProjectRoadSectionsController`, `ProjectWarrantiesController`, related Services/Repositories/entities/configurations, spatial ApiTests/IntegrationTests; `contracts/`/Postman/`.http` for approved operations.
- **Contract/data/consumer effect:** existing six project/road/warranty routes remain; new route/segment contract versioned for web/Android/survey consumers. Additive segment schema/version data may need backfill from road sections; no geometry overwrite of historical version.
- **Steps:** characterize scope and effective-date checks -> preserve current route results -> validate geometry samples -> design versioned segment set/PM remainder action -> approved expand/backfill/verify -> integrate consumers with old-version read window.
- **Verify:** wrong role/project/time-window API tests, SQL Server spatial constraints/geometry round-trip, segment remainder and version immutable tests, Postman CG16 coverage, consumer payload samples. No shared DB testing.
- **Done when:** current routes unchanged or explicitly versioned; 39A pilot rule correctly scoped; source geometry provenance and actor approval proven; all migration/consumer gates passed or blocked checkpoint declared.
- **Recovery:** retain previous road/segment version for reads and republish via approved state transition; additive schema forward repair or isolated backup restore, not applied migration deletion.

## Two-developer delivery supplement

- **Proposed owner:** A. Split dual-owner parent tasks into single-owner slices before edits. See [ownership plan](../03-two-developer-plan.md).
- **Pre-edit checkpoint:** record exact allowed files/symbols, read-only dependencies, shared-file reservation, baseline commit and preserved dirty changes. Record START / INTEGRATE / RELEASE gates for this slice; existing decision/data gates remain in force.
- **Independence:** use an agreed interface/version and fixture for consumer work; label test-double evidence separately. Do not claim parent Done before required real integration.
- **Self-review:** correctness pass plus ownership/consumer impact pass, safe in-scope autofix, focused verification and final diff review. Record unresolved findings.
- **Cross-owner impact:** create a note from [coordination template](../templates/coordination-note.md), recommend primary fixer and wait for the two developers to assign the overlapping change. Continue independent work; do not edit the other owner's files or duplicate their business logic.
- **Report:** [revised seven-section template](../templates/task-report.md); single-owner task may retain its existing report path, slices use `reports/<TASK-ID>-<SLICE>-<A|B>.md`. Record implementation status separately from delivery stage.
