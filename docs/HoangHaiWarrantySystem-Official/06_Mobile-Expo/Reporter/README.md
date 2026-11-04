# Reporter Mobile — React Native + Expo

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

**Phase 3B · baseline `d5c935bfe00b82ffb9474f222cdc79d329387f39` · documentation handoff only.**

Reporter is a Mobile React Native + Expo actor. Supervisor and PM remain Web; Operator and Repair Crew remain Mobile. This package describes a proposed Expo consumer of the current Backend contracts. No Mobile repository, Expo SDK lock, native build, device test, SMTP, object storage or E2E result was supplied, so those items are `NOT_VERIFIED_EXPO_E2E`.

## Reading order

1. [Screen/API matrix](01_Reporter-Screen-API-Matrix.md)
2. [Authentication, OTP and session](02_Authentication-OTP-and-Session.md)
3. [Private photo upload](03_Private-Photo-Upload.md)
4. [Report and IncidentCase lifecycle](04_Report-and-IncidentCase-Lifecycle.md)
5. [Expo architecture and local security](05_Expo-Architecture-and-Local-Data-Security.md)
6. [Errors, retry and reconciliation](06_Error-Retry-and-Reconciliation.md)
7. [Acceptance scenarios](07_Reporter-Acceptance-Scenarios.md)
8. [Review report](PHASE3B_REVIEW_REPORT.md)

## Authority and boundaries

- `CURRENT_VERIFIED`: Backend controllers/services/DTOs and the Phase 2C catalog at the pinned source SHA.
- `TARGET_CONFIRMED`: Reporter is Mobile React Native + Expo; the product is HoangHaiWarrantySystem and technical routes retain RoadGuardSystem names.
- `PROPOSED_EXPO_UI`: screens, navigation and client adapters in this package.
- `SOURCE_GAP` / `NOT_VERIFIED_EXPO_E2E`: missing native app, provider and deployed verification.

The online Reporter MVP uses Android bearer login/refresh and does not use the Crew signed offline workflow, device enrollment or ECDSA batch submission. A Reporter creates a report from private `REPORT_PHOTO` evidence without a Drone Survey. The backend creates an unassigned IncidentCase; Supervisor triages it before PM processing. Reporter can read only own projections and explicitly published updates.

The single API catalog is authoritative for exact DTO fields, headers and status codes: [Phase 2C API entrance](../../04_API-Contracts/README.md), [full catalog](../../04_API-Contracts/02_Full-API-Catalog.md). This handoff does not add routes or grant roles.
