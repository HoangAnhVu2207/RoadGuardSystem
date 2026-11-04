# 01 Database Overview

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

Four model layers remain separate:

- CONCEPTUAL_MODEL: owner-confirmed actor/workflow/obligation/source relationships, not a physical schema certification.
- EF_LOGICAL_MODEL: fresh disconnected DesignTimeModel metadata from RoadGuardDbContext and current configuration assembly.
- MIGRATION_DERIVED_PHYSICAL_MODEL: current baseline/additive migration and snapshot, SQL triggers/scripts and unchanged-input historical fixture evidence.
- LIVE_SQL_VERIFIED: NOT_RUN in BE-DOC-02; no deployed/shared SQL was queried or migrated.

Fresh EF capture shows75 explicit DbSet properties,196 EF entity types sharing191 tables,1940 distinct mapped table/column pairs and662 FK declarations. DbSet count is not entity/table count: sidecars and owned/table-sharing mappings exist without an explicit DbSet. Per-entity property rows can repeat shared physical columns; do not sum them to claim physical columns.

SQL Server persistence uses configurations, defaults/checks/filtered indexes, rowversion, restrictive relationships, append-only/immutability triggers and transactional source receipts. Complete entity dictionary/relationship/constraint catalogs include converters and generated values. Raw metadata is in ef-model.json. Historical SQL catalog was rechecked against578 unchanged input hashes; its physical observation stays historical evidence, not a new LIVE_SQL claim.

Source-of-truth CRS is versioned explicitly. WGS84 geography4326, WGS84 UTM32648/32649 and native VN2000 profiles are distinct; do not relabel one as another. Sample affine transforms and synthetic roundtrip are not official accuracy acceptance. Geometry/profile provenance, analytic LINE/ARC/model length, rendering tessellation and official chainage stay separate; see roads workflow and SpatialConstants/GeometryEngine/CrsProfileEngine source.

Relational-count clarification:662CLR FK declaration rows are not the physical constraint count. Fresh EF relational mapping is657FK/692indexes/219keys/229checks; migration source adds5FK/1index, yielding662FK/693indexes. Historical fixture catalog verifies the latter at its source baseline; live SQL remains NOT_RUN.
