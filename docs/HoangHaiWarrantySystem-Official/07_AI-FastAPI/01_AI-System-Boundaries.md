# AI system boundaries

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

## Responsibilities

| Boundary | Owner | Evidence/status |
|---|---|---|
| Project, survey, dataset, file identity, road geometry and access | ASP.NET Backend | `CURRENT_IMPLEMENTED_BE`; `ProcessingV2Controller.cs`, Phase 2C API-044/045/046 |
| Job/attempt, manifest hash, model/config pins and callback admission | ASP.NET Backend | `CURRENT_IMPLEMENTED_BE`; `ProcessingV2Service.cs`, `ProcessingV2PersistenceService.cs` |
| Frame extraction, SRT parsing, inference and candidate formatting | AI service | `CURRENT_IMPLEMENTED_AI` for repository source; deployment is `NOT_VERIFIED_E2E` |
| PM KEEP_NEW/LINK_EXISTING/REJECT and Defect creation | Backend/PM | `TARGET_CONFIRMED`; AI output is only a candidate |
| FIELD physical dimensions and repair execution | Crew/Backend workflow | `TARGET_CONFIRMED`; bbox is not a physical measurement |

`src/api/worker_service.py` exposes an async job API, but its non-empty-video branch currently instantiates `MockAIAdapter`; this is source evidence of a synthetic path, not proof of real YOLO execution. `src/api/ui_server.py` is a local web test bench and is not the Backend production integration.

No SfM, orthomosaic, DSM, 3D reconstruction or AI-derived physical measurement is part of this handoff. Image road-surface segmentation may be placed where the approved deployment architecture chooses; it is not GIS segmentation.

## Open findings

- **P1-01 PARTIAL:** real FastAPI/provider/checkpoint/capability adoption is unverified.
- **P1-02 PARTIAL:** callback verifies job membership and pins, but latest/active attempt fencing is incomplete.
- **P1-05 NOT_VERIFIED:** external/native/deployment integration was not executed.
