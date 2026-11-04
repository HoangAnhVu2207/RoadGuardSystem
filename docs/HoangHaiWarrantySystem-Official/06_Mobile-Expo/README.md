# Mobile Expo — Operator and Crew integration

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

Phase 3C canonical handoff for React Native + Expo clients. Supervisor and PM remain Web; Reporter, Drone Operator and Repair Crew are Mobile Expo actors. This package maps current Backend contracts to mobile responsibilities. It does not claim an Expo repository, native build, device E2E, DJI SDK, background sync, SQLCipher or Keystore implementation.

## Reading order

1. [Operator screen/API matrix](Operator/01_Operator-Screen-API-Matrix.md)
2. [Operator survey upload](Operator/02_Survey-Video-SRT-and-Upload.md)
3. [Operator task errors](Operator/03_Task-Dataset-and-Error-Handling.md)
4. [Crew screen/API matrix](Crew/01_Crew-Screen-API-Matrix.md)
5. [FIELD measurement](Crew/02_FIELD-Measurement-Workflow.md)
6. [NORMAL/Fast Track execution](Crew/03_NORMAL-and-FastTrack-Execution.md)
7. [Offline signed sync](Crew/04_Offline-Signed-Sync-and-Handover.md)
8. [Local security/reconciliation](Crew/05_Local-Security-and-Reconciliation.md)
9. [Acceptance scenarios](07_Mobile-Integration-Acceptance.md)

## Status vocabulary

`CURRENT_IMPLEMENTED_BE` means source/controller/service/DTO evidence exists. `TARGET_CONFIRMED` means owner product behavior. `PROPOSED_EXPO` describes client work required to consume the contract. `NOT_VERIFIED_EXPO_E2E` means no real Expo/device test was supplied. `SOURCE_GAP` is a Backend or integration limitation already recorded in Phase 0–2C.

The exact route and DTO contract remains [Phase 2C Full API Catalog](../04_API-Contracts/02_Full-API-Catalog.md) and [consumer crosswalk](../04_API-Contracts/09_Consumer-API-Crosswalk.md). Mobile must not infer authority from a visible button or from an eligibility preview.
