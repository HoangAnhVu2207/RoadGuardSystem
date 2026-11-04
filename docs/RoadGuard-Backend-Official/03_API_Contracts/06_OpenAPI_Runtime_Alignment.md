# 06 OpenAPI Runtime Alignment

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

Current details: [integration, authority, compatibility and evidence](../CURRENT-INTEGRATION-20261027.md); [exact current endpoint catalog](02_Endpoint_Catalog.md); [384 current schema definitions](../../HoangHaiWarrantySystem-Official/04_API-Contracts/03_DTO-and-Schema-Reference.md). Historical deep traces below must be rechecked against current SHA before reuse.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

Snapshot `openapi.backend.yaml` comes from fresh GET `/swagger/v1/swagger.json` using existing CustomWebApplicationFactory Development, database deliberately disconnected, storage endpoint disabled and hosted workers removed. API Explorer restricts production controller assembly; test-only paths and the unreferenced ProbePayloadDto schema are excluded. No Backend wire contract is corrected or canonical V2 replaced.

Observed:47 controllers,241 operations,223 paths,357 production-reachable component schemas, OpenAPI3.0.1. Raw host response had358 schemas including one test probe. Every production method/path matches the independent API Explorer list. `/health` is separately documented infrastructure.

All241 operationId fields are absent in the runtime snapshot; OpenAPI3.0.1 permits absence. Duplicate non-null IDs=0. Do not silently manufacture stable client identifiers or declare operation IDs present. Optional future consumer-generator IDs require compatibility review, not a Backend fix in this task.

Request/response schema $refs, security schemes, nullable/required and ETag/If-Match/Idempotency-Key declarations are preserved. OpenAPI validity does not prove that manual Swagger declares every dynamic error/required guard or every anonymous response projection. Controller/result mapper evidence in catalog/error pages remains necessary. Nullability/schema assertions are structural observations, not every invalid-input request executed.

Historical V2 proposal uses different contract shape and frozen FE lock; its route count does not establish runtime equivalence. Prior lock mismatch remains known; no V2 or generated FE files are edited. Deployed runtime Swagger/UAT NOT_RUN.
