# Model versions and detection schemas

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

The Backend pins released `ModelVersionId`, preprocessing/config versions, mode and manifest hash on the processing job. The AI repository's `AIDetectionPayloadFormatter` emits a richer internal record: detection ID, processing job, model version, defect type, target band, overlap flag, confidence, optional geometry/station, camera pose and raw bbox/crop URI. `MockAIAdapter` generates synthetic pothole/crack examples and marks `is_mock=true`.

For the Backend callback, the canonical wire is the DTO in `RoadGuardSystem.DTOs/Processing/ReceiveAiResultRequestDto.cs` and Phase 2C API-031: detection ID, frame file ID, timestamp, type code, confidence and normalized bbox. The AI formatter's `estimated_width`/`estimated_length` and geometry fields must not be copied into physical Crew measurements; any mapping requires an approved contract and provenance review.

Model class names, checkpoint files, confidence metrics and class coverage are repository facts only. A configured model path or README claim is not a released model, and no accuracy/F1/recall result is asserted. `CURRENT_IMPLEMENTED_AI` source schema and `CURRENT_IMPLEMENTED_BE` DTO validation are separate from `NOT_VERIFIED_E2E` compatibility.

Road segment/section IDs and image segmentation masks have different semantics. Preserve frame/timestamp/model/checksum provenance when transforming a detection into a Backend candidate.
