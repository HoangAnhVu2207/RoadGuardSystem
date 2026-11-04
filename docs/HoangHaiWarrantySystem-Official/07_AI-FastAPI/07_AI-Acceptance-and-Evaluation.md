# AI acceptance and evaluation

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

| Scenario | Evidence required | Current status |
|---|---|---|
| Backend creates job and emits pinned manifest/attempt | API-044 source + HTTP test | Source verified; HTTP/E2E NOT_RUN |
| AI accepts adopted manifest and returns job status | approved provider contract + deployed request | NOT_VERIFIED; local worker source only |
| Real video + SRT produces timestamped candidates | real files, released checkpoint, provenance | NOT_VERIFIED |
| Missing/corrupt video or SRT is classified | deterministic provider test | NOT_VERIFIED |
| Callback accepts valid result under AI identity | API-031 auth and DTO test | Backend source verified; external caller NOT_RUN |
| Late/replayed callback is fenced | attempt race/replay test | P1-02, NOT_VERIFIED |
| Candidate remains PM decision and Crew measurement | workflow/API authorization tests | source/workflow documented; E2E NOT_RUN |
| Model version/checksum and raw artifact remain traceable | database + object storage inspection | NOT_VERIFIED |

Static checks performed for this phase are documentation/source inspections only. No FastAPI process, real YOLO checkpoint, MinIO, TLS, deployment, Android/Web client, or cross-service E2E test was run. Model quality metrics are therefore not claimed. See Phase 2C API contracts and Phase 2B integration-boundary notes for Backend evidence.
