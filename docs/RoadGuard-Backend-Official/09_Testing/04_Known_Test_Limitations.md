# 04 Known Test Limitations

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

Current details: [integration, authority, compatibility and evidence](../CURRENT-INTEGRATION-20261027.md); [exact current endpoint catalog](../03_API_Contracts/02_Endpoint_Catalog.md); [384 current schema definitions](../../HoangHaiWarrantySystem-Official/04_API-Contracts/03_DTO-and-Schema-Reference.md). Historical deep traces below must be rechecked against current SHA before reuse.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

**CURRENT_VERIFIED — source baseline `cdeb1ca1037744d06995058af13d0582f6273c0a`, `huy-review`, 2026-10-13.** Source observation is not all-path runtime execution. Expected business behavior uses owner-confirmed decisions and assigned specs; implementation/test evidence has its own limits.

Fresh focused API test result:92total,91PASS,1FAIL,0skip. Failure ManualSwaggerContractTests.ProductionInventoryAndEvidenceExcerptsMatchCurrentSource points at stale SWG-014 BusinessDutyRepository excerpt. The test/code/embedded Swagger evidence JSON are outside docs scope and remain unchanged. Prior independent audit found11stale of2533unique excerpts across9sourcefiles; this statistic is historical source-revalidated evidence, not eleven fresh runtime failures.

Build PASS0errors/222warnings is fresh; warnings are not suppressed and analyzer remained enabled. Fresh OpenAPI structural validity and API Explorer match do not certify all241 business requests or declaration completeness. Runtime operationId is absent for all operations; this is permitted by schema and documented as consumer limitation.

All unselected tests are NOT_RUN this task; historical failures of two FE validators/281legacy local references belong to the prior audit and are not imported as failures in the new internal-link gate. V2 bytes/lock are preserved. Fresh new docs links are checked independently.

Storage-open privacy race is a source risk not reproduced; callback stale-attempt limitation is observed source gap without live exploit test. Android device, real FastAPI, physical deployed SQL, MinIO/email/browser/proxy, GIS/control data, NFR and backup restore remain pending. No dummy values or absent environment is turned into PASS.
