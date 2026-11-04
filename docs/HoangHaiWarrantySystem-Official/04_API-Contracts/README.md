# Full Backend API contracts — HoangHaiWarrantySystem

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

**Phase 2C · 15/10/2026 · implementation source pinned `36c3a1d273f17dd29b816136ae0182a821fd79e4` · independent JEV pending.**

[API entrance](README.md) · [Review/evidence](PHASE2C_REVIEW_REPORT.md) · [10 workflows](../02_Business-Workflows/README.md) · [Backend/Data](../03_Backend-and-Data/PHASE2B_REVIEW_REPORT.md).

`CURRENT_VERIFIED` = source/static contract discovery, không HTTP/E2E/deployment PASS. `CURRENT_IMPLEMENTED` trong phần AI là phân nhóm contract BE đã có, không nhãn real provider acceptance. `TARGET_CONFIRMED` client/business theo [owner decisions](../../product/confirmed-decisions.md), [latest HUY](../../../planning/development/HUY-FINAL-INTEGRATION.md) và owner Phase2C authorization; precedence theo [governance](../00_Governance/01_Authority-and-Source-of-Truth.md). `PROPOSED`, `NOT_VERIFIED`, `HISTORICAL` giữ phạm vi. Product HoangHaiWarrantySystem; technical RoadGuardSystem/routes giữ nguyên.

## Read order and authority

Phase2B owner PASS for documentation scope. This phase documents **47 controllers /241 unique operations /223 paths**, health separately, at approved source baseline. Source Controller/DTO/service/repository/current guards establish implementation; owner decisions establish expected behavior. Current source mismatch becomes GAP, not silently changed contract. OpenAPI artifact remains unchanged; generated catalog isn't semantic authorization proof or accepted E2E fixture.

| Document | Use |
|---|---|
| [01 Conventions](01_API-Conventions.md) | Version/base/auth/content/query/error/time/coordinates/retry rules |
| [02 Full catalog](02_Full-API-Catalog.md) | One primary entry per241 operation, exact params/schemas/status/errors/source/current roles |
| [03 DTO/schema](03_DTO-and-Schema-Reference.md) | Source DTO/attrs/enums and357 condensed schemas; no fake examples |
| [04 Auth/authorization](04_Authentication-and-Authorization-Contracts.md) | Web cookie/CSRF, Mobile Android transport, Reporter chain, service identity |
| [05 K/V/errors](05_Idempotency-Concurrency-and-Errors.md) | Module-specific runtime rules +241 declared header crosswalk |
| [06 Upload/evidence](06_File-Upload-and-Evidence-Contracts.md) | Private/project/offline scopes, complete/verify/deployment gate |
| [07 Offline crypto](07_Offline-Sync-and-Crypto-Wire.md) | Exact source canonical bytes/hash/P1363/public keys and current receipts |
| [08 AI](08_Processing-and-AI-Contracts.md) | Implemented BE callback versus proposed provider/unverified YOLO |
| [09 Consumer crosswalk](09_Consumer-API-Crosswalk.md) | Shared APIs conditional on actual role/source, not UI grants |
| [Review](PHASE2C_REVIEW_REPORT.md) | Exact checks/exceptions/GAPs/NOT_RUN/delivery gate |

Source artifacts: [unchanged OpenAPI](../../RoadGuard-Backend-Official/03_API_Contracts/openapi.backend.yaml), [API/source index](../../RoadGuard-Backend-Official/10_Traceability/api-source-map.json), [method guards](../../RoadGuard-Backend-Official/10_Traceability/api-method-evidence.json), [source declarations](../../RoadGuard-Backend-Official/10_Traceability/source-inventory.json). Condensed reference doesn't duplicate raw YAML/examples; no lock/schema/route edits.

Reporter uses OTP→Android transport→private upload→VERIFIED→own Report/list/detail/supplement/publication; independent Drone Survey. Supervisor/PM Web; Reporter/Operator/Crew Mobile Expo; technical Android route unchanged. No Reporter PM/Crew/signed sync grant, no backend encrypted handover proof local Expo encryption, no signature person/time proof, no synthetic YOLO claim. P1/P2 remain open. API operation source/test presence isn't E2E acceptance.

**STOP after Phase2C push. Independent JEV PENDING; Phase3 NOT_STARTED.**
