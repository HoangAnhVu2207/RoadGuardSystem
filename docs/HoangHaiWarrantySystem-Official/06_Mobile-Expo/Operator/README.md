# Operator Mobile handoff

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

Drone Operator is a Mobile React Native + Expo actor using bearer transport. PM creates and assigns survey work on Web; Operator reads the current assignment, accepts or declines it, uploads survey video and telemetry, and submits the dataset. Supervisor/PM approval, processing decisions, candidate decisions and repair authority stay in Backend/Web boundaries.

## Screens and order

Read [the screen/API matrix](01_Operator-Screen-API-Matrix.md), then [survey video/SRT upload](02_Survey-Video-SRT-and-Upload.md), and finally [task, dataset and error handling](03_Task-Dataset-and-Error-Handling.md). The matrix covers eight screens: session, work list, task detail, decline/supplement, work package, capture/upload, dataset submission and recovery/history.

## API boundary

The documented Operator APIs are API-225/226/227/229/230 for task lifecycle, API-233/234/235/236 for upload lifecycle, API-104 for the permitted geometry package, and API-232/231 for PM-owned supplement/reassignment context. Every route must be checked against current assignment, device, project, route/version and file-purpose guards. IDs and routes link to the [Phase 2C catalog](../../04_API-Contracts/02_Full-API-Catalog.md) and [consumer crosswalk](../../04_API-Contracts/09_Consumer-API-Crosswalk.md); no endpoint is inferred from a UI label.

Operator does not receive Crew device enrollment, signed canonical batch sync, snapshot handover, repair execution or PM/Supervisor approval rights. Upload complete is not `VERIFIED`; SRT/GPS absence is not coverage success.

## Status and gaps

Backend route/DTO/role behavior is `CURRENT_IMPLEMENTED_BE` by source inspection. Screen design and Expo/native capture are `PROPOSED_EXPO` / `NOT_VERIFIED_EXPO_E2E`. DJI SDK integration, background upload guarantees, real device storage protection, deployment and external E2E remain open. Operator offline signed sync is not asserted.
