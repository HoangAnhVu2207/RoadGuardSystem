# 04 Implementation Status

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

| Phân hệ | CURRENT_IMPLEMENTATION | Verification boundary |
|---|---|---|
| Auth/accounts/staff/projects | Implemented; focused current HTTP/security tests include these surfaces | Actual browser/proxy/keyring/mobile adoption NOT_RUN |
| Roads/GIS/pavement/survey | Source-traced versioned geometry, assignment, datasets, explicit CRS/profile/source | Official control-point/accuracy/real coverage acceptance pending |
| Reporter/Case/candidate/AI | Implemented source workflows; partial matching/callback/pre-Defect gates remain | Live FastAPI/video model NOT_RUN; synthetic path separate |
| FIELD/repair/FT | PM policy, measurements, normal/FT execution/review/correction/lifecycle implemented | R03 final information producer gap; additional FT after expiry deferred |
| Offline | Device/snapshot/provenance/signed batch/receipt/package/grant/artifact/reconcile implemented | Actual Android/device key/decryption/restart pending; many runtime branches NOT_RUN |
| Reporting/notifications/retention/export | Source producers/consumers and history, scoped reads, evaluation/export flows present | Registered source type is not proof a producer exists; deployed workers/storage pending |
| Database/API | Fresh EF model and runtime Swagger captured safely; no physical SQL query in this task | EF_LOGICAL_MODEL/MIGRATION_DERIVED_PHYSICAL_MODEL; LIVE_SQL_VERIFIED NOT_RUN |

Method existence is SOURCE_VERIFIED only. Tests are PASS only within fresh executed cases; DOC-AUDIT-01 counts are historical source-revalidated evidence. No whole-system completion or all-route security certification is claimed.
