# 03 Database Integration Tests

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

Database test sources include isolated fixture model/snapshot/catalog, identity sessions, reporter/case/receipt, repair policy/execution/lifecycle/safety, coverage/statistics/retention, outbox and signed offline persistence. Complete test declaration inventory is provided beside the API test matrix; all methods are source-indexed, not blanket PASS.

BE-DOC-02 does not run new SQL integration tests or apply migrations. Fresh EF DesignTimeModel is captured from disconnected host:196entitytypes/191tables/1940unique mapped columns/662FK. This is EF_LOGICAL_MODEL only. No test success is inferred from migration source.

Historical audit schema test applied current two migrations to fixture-owned SQL and compared EF/snapshot/catalog. Its5cases and overall84 SQL cases were historical execution;578schema input hashes are revalidated unchanged. Physical191tables/1940columns/662FK/693indexes/219keys/229checks/189triggers describe that historical fixture, not live deployment. Raw historical trigger definitions and current source hashes are retained in provenance/constraint metadata.

Future SQL verification must use disposable database, preserve actual source/policy/assignment/account/current recipient facts, prove transaction rollback/dedup/concurrency and fail-closed immutable history. Production row policy, external bytes, restore duration, live connection permissions and actual trigger effects require explicit runtime acceptance beyond a model match.
