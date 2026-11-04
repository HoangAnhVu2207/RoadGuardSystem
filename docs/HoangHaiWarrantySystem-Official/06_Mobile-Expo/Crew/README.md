# Repair Crew Mobile handoff

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

Repair Crew is a Mobile React Native + Expo actor using bearer transport. Crew receives only assigned FIELD inspection and repair work. PM owns planning, assignment, review and policy; Supervisor owns NORMAL approval/final confirmation and the applicable informational boundary; Backend remains authoritative.

## Screens and order

Read [the screen/API matrix](01_Crew-Screen-API-Matrix.md), [FIELD measurement](02_FIELD-Measurement-Workflow.md), [NORMAL and Fast Track execution](03_NORMAL-and-FastTrack-Execution.md), [offline signed sync](04_Offline-Signed-Sync-and-Handover.md), and [local security/reconciliation](05_Local-Security-and-Reconciliation.md). The matrix covers eleven screens: session, FIELD inbox/detail, acceptance, start/measurement, evidence, FIELD review/supplement, repair-attempt correction, repair execution, offline sync and receiving handover.

## Distinct workflows

- **FIELD:** accept/start, capture Crew physical measurements and evidence, submit via API-095, read/reconcile via API-096 and PM review via API-092. A FIELD supplement is not API-159.
- **Repair:** execute the assigned NORMAL/Fast Track item with API-146/157/160/161 and request review through API-150. PM review is API-158. `API-159` is only the repair-attempt supplement route and requires the repair package/item/task binding, current versions and assigned Crew.
- **Offline:** API-116/118/127/129/130 and handover APIs are available only where Backend admits the current Crew/device/scope. Operator and Reporter are not granted this signed workflow by sharing the Expo platform.

## Status and gaps

Backend routes, DTOs and role/resource guards are `CURRENT_IMPLEMENTED_BE` by source/catalog inspection. Expo screens, camera/native secure storage, SQLCipher/file encryption, signed-key custody and device E2E are `PROPOSED_EXPO` / `NOT_VERIFIED_EXPO_E2E`. Key loss, upload verification, reconciliation races, Fast Track notification (P2-06), and external/native deployment evidence remain open.

See the [Phase 2C catalog](../../04_API-Contracts/02_Full-API-Catalog.md), [consumer crosswalk](../../04_API-Contracts/09_Consumer-API-Crosswalk.md), and [mobile acceptance scenarios](../07_Mobile-Integration-Acceptance.md).
