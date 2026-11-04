# FastAPI/YOLO integration handoff

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

Phase 4 documentation for the internal AI boundary of HoangHaiWarrantySystem. Backend source is pinned at `36c3a1d273f17dd29b816136ae0182a821fd79e4`; AI repository source is read-only snapshot `5d44980a1177abd219cf488c84c365d01cea49e8`.

Status labels: `CURRENT_IMPLEMENTED_BE` means source verified in ASP.NET; `CURRENT_IMPLEMENTED_AI` means source verified in the AI repository; `PROPOSED_INTEGRATION` is a design handoff; `NOT_VERIFIED_E2E` means no deployed/provider test was run; `SOURCE_GAP` means the source does not yet prove the behavior.

The Backend remains the system of record for projects, survey inputs, files, jobs, attempts, provenance, authorization and PM decisions. AI produces detection candidates. It does not create Defects, approve repair, or replace Crew FIELD measurement. GIS road segments/sections are distinct from image/pixel segmentation.

Read in order: [boundaries](01_AI-System-Boundaries.md), [pipeline](02_Video-SRT-and-Frame-Pipeline.md), [BE contract](03_BE-to-AI-Integration-Contract.md), [callback](04_AI-to-BE-Callback-and-Results.md), [schemas](05_Model-Version-and-Detection-Schemas.md), [security](06_Security-Idempotency-and-Retry.md), [acceptance](07_AI-Acceptance-and-Evaluation.md).
