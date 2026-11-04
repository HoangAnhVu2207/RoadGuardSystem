# 05 Migrations and Schema Baseline

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

Current migration assembly has exactly two migrations:

1.20261008171739_BaselineCurrentSchema.
2.20261009043010_AllowVerifiedDefectPostRepairTasks.

RoadGuardDbContextModelSnapshot is the current EF snapshot. Baseline replacement is for NEW isolated databases under the assigned baseline decision; it is not authorization to replay migration history on an existing team/production database. __EFMigrationsHistory is infrastructure, not one of191 application tables. Applied history is not rewritten in this task.

Baseline migration creates tables, constraints, indexes and source trigger definitions; additive migration changes the legitimate Reporter-source/verified Defect FIELD relationship guard. Preserve source provenance and constraints when designing a later additive change. No fake Survey/Defect row is a valid migration workaround.

Read current source files under RoadGuardSystem.Repositories/Migrations, the full snapshot, configuration assembly and SQL source inventory. Existing design-time factory and seeding tooling are inventory items, never run as task setup. No migration apply/seed/backfill/downgrade was performed in BE-DOC-02.

Historical fixture model/snapshot/SQL tests from DOC-AUDIT-01 have unchanged inputs but are not rerun/live deployment proof. New local model metadata revalidates table/property/constraint names without connecting SQL. EF warns about FieldInspectionPurpose default sentinel: current generated default can select database default for CLR Unknown; this is recorded as an observed model warning, not a reproduced wrong business transition.
