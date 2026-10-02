# RoadGuard backend draft: source facts and proposed boundaries

[Current isolated SQL data baseline](data/README.md) (RF-06A) maps model, snapshot, migrated catalog, ERD and dictionary. It is current-checkout evidence, not a target or deployed-schema assertion.

**RF-04 draft, not active.** Local branch `anh`, HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; working tree includes prior survey changes. [RF-00](../../planning/refactor/00-baseline.md) found .NET 8 solution with API, BusinessObjects, DTOs, Services, Repositories and three test projects. [RF-01](../../planning/refactor/01-code-map.md) mapped 17 controllers/57 actions, workers and the shared DbContext. This page is architecture documentation, not an instruction to rewrite source.

## Current and proposed architecture

**CURRENT_VERIFIED:** controllers bind v1 routes and call Service interfaces; Services coordinate use cases; Repositories own EF/SQL and storage adapters. `RoadGuardDbContext` is shared, with project-scoped facts across modules. API, Service, Repository and entity types vary in age and conventions; [CG01-CG17](../../planning/refactor/02-contract-gaps.md) records observed conflicts. No runtime wire claim follows from compilation alone.

**PROPOSED:** retain the existing process and project graph for the first refactor pass. Controllers own transport/auth input and approved HTTP mapping; Services own actor/project policy and workflow; Repositories own persistence, rowversion, transactions, idempotency record, outbox and object-storage metadata. Logical data ownership is by module below while one physical DbContext stays. Cross-module writes require a named coordinating Service and transaction/outbox boundary. There is no measured basis to upgrade framework or split into services now. Source: [RF-03 architecture](../../planning/refactor/03-target-architecture.md).

| Module / proposed data owner | Current seam and dependency | Planned checkpoint |
|---|---|---|
| Identity | `AuthController`, `MeController`, `ProfileController` -> identity services/repository; user/session/OTP/invitation. | RF-10-01: PR-36A/37 compatibility, PII, project actor. |
| Project/road/warranty | Project and road controllers -> project/road/warranty repositories; membership and geometry facts. | RF-10-02: version, GIS and project scope. |
| Survey/dataset | `SurveyPlanningController` and `SurveyV2Controller` -> separate services on shared plan/request tables. | RF-10-03: old/V2 row semantics, immutable dataset and UNKNOWN coverage. |
| Upload/file | `UploadsController` -> `UploadService`/persistence + object storage; source file/scope. | RF-10-04: PR-38, CG17 compatible size type, verification. |
| Processing/AI | `ProcessingV2Controller` -> processing service/repository; job/attempt/result/validation. | RF-10-05: external AI protocol, active attempt and receipt. |
| Reporter/defect | Registration exists; target case/report review actions absent; `Defect`/`AIDetection` entities exist. | RF-10-06: privacy, PM decision and candidate provenance. |
| Inspection/repair | Inspection task read action exists; target policy/repair/acceptance actions absent. | RF-10-07: measurement and Fast Track approval boundary. |
| Offline | Current upload/replay primitives only; `syncOperations` target-only. | RF-10-08: Android wire and original actor. |
| Messaging/reporting | Notification actions, outbox lease and consumer exist; dashboard/retention actions absent. | RF-10-09-A: dispatcher, audit and hold; RF-10-09-B: KPI/report/export. |

Current-versus-draft route details, DTOs and proposed owners are in the [machine crosswalk](../../planning/refactor/04-operation-crosswalk.json). It retains raw/version/normalized routes and content-based `x-fr` relations. `InspectionTasksController.List` and `listMyInspectionTasks` belong to RF-10-07/A in the corrected proposal; `BR-46` temporary safety is also RF-10-07/A, with RF-10-09-A as notification collaborator. A draft route without a source action is not evidence that a signed-off endpoint is missing. External consumers remain UNKNOWN.

## Authorization and error boundary

`[Authorize]` alone does not prove object scope. Current `WorkPackageRead`, `AiCallback` policies and `ProjectScopeGuard` are described in [RF-01 inventory](../../planning/refactor/01-endpoint-inventory.md). **PROPOSED** command rule: resolve actor, live project membership/effective dates and target object before a write; recheck on offline reconnect. PR-36A/37 require web cookie/server session and Android tokens, but Q-RF02-02 must settle bearer compatibility and CSRF. Keep `/profile`, `/me` and both reset routes in the consumer map pending that decision.

Current `ApiErrorCodes` are lowercase and controllers construct ProblemDetails differently; draft V2 errors are uppercase. This is CG01/L15, not a formatting cleanup. RF-08 first characterizes status/body/headers and client branching, then RF-09 plans version/compatibility. No new error catalogue is active from this page.
