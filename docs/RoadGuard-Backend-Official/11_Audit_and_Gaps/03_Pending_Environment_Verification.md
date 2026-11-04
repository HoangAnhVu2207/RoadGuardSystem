# Pending environment verification

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

These accepted integration/deployment checks are deferred acceptance, not removed Backend scope. No shared/production SQL migration, real data write or remote publication occurred.

| Gate | Owner | Required evidence | Current task status |
|---|---|---|---|
| FastAPI/video model | External AI + BE | Real authenticated dispatch/callback, actual video source/manifests/checksums/model/attempt, late result/retry/error/source compatibility | NOT_RUN; synthetic worker not substitute |
| Android offline/reconnect | Android + BE | Actual signed command interoperability; durable queue kill/reboot; key custody; encrypted handover/decrypt; conflicts/revoke/replay | NOT_RUN |
| Physical SQL schema | Operations/DB | Approved isolated/deployed SQL comparison to migration/model, actual permissions/triggers; no team mutation from this task | LIVE_SQL_VERIFIED NOT_RUN; historical fixture source hashes match |
| Object storage | Operations/storage + BE | Real multipart/resume/verify/failure recovery and authorized download; 8GiB video/32GiB dataset admission vs actual streaming | NOT_RUN |
| Email and Web cookies | Operations/Web | Real delivery/recovery, actual HTTPS browser, CSRF, proxy/cookie host/keyring replicas, current revoke/renew | NOT_RUN; selected TestHost cookie tests fresh |
| GIS/source facts | Owner/GIS | Official CRS/profile/operation/independent control-point tolerance, real coverage/warranty/location/statistical sources | NOT_VERIFIED; sample transforms never official accuracy |
| UAT/deployed Swagger | QA/operations | Correct deployed source identity + actual production-host OpenAPI/business flows for intended actors | NOT_RUN |
| Performance and restore | Operations/QA | 50 concurrent/p95≤2sec; consistent SQL/object/keyring restore RPO≤15min/RTO≤4h with timed proof | NOT_RUN |

Missing credentials/environments are not requested or guessed; this task can complete documentation alignment while runtime/E2E certification remains pending.
