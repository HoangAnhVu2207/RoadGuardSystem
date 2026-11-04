# 01 Backend Test Strategy

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

Test evidence is scoped by execution, source identity and environment. Unit tests isolate domain/policy/crypto/boundary logic; ApiTests exercise actual middleware/controllers/DI with fixture/fake/disconnected or owned SQL per class; IntegrationTests use disposable SQL fixtures. A class name or syntactic reference does not prove a negative case was exercised.

For this documentation-only package: fresh build with default analyzers; fresh92 focused auth/directory/project/Supervisor/Swagger tests; fresh safe API Explorer/OpenAPI and EF metadata capture; full source/DTO/entity/action inventories and document/schema/link/hash checks. Full unit/SQL suites and live external E2E are NOT_RUN; no source changed, so unrelated full suite repetition is unnecessary.

Historical DOC-AUDIT-01 executed381tests:380pass/1fail,0skip at f0c8762. All971 production source fingerprints match current baseline; selected old fixture/schema input hashes match. This confirms source identity and historical evidence provenance, not fresh execution of those381cases. Current fresh92 are separately counted and must not be added to historical381 as one run.

Negative validation coverage must distinguish wrong current actor/session/role/project/resource, stale version, wrong state/evidence, protected replay after revoke, same key changed payload, rollback and assignment/membership windows. Focused test assertion bodies have been inventoried; per-operation references are static candidates, with executed classes mapped by TRX.

Never target shared/team/production SQL for tests. Existing source boundary fixtures can write inventory: use external output override if selected. Current capture explicitly disables startup migration/seed, real storage and workers. Actual browser/TLS/proxy, email/MinIO8GiB, FastAPI/model, Android queues/keys and restore/SLO checks need separate acceptance.
