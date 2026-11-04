# Security, idempotency and retry

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

Backend commands require bearer authentication and project/role guards. AI callbacks use the separate `AiCallback` service scheme. Secrets, signing keys, callback URLs and object-store credentials must be injected by deployment configuration; none are documented as literals or client secrets.

Backend command idempotency uses `Idempotency-Key`; create/retry require it and retry also requires `If-Match`. API-031 requires the key and validates hashes. The AI repository's `IdempotencyManager` computes SHA-256 over `processing_block_id`, model version and manifest checksum and stores results in an in-memory dictionary. This is useful compatibility evidence but is not durable multi-instance idempotency or replay protection.

Retry rules: retry infrastructure failures with bounded backoff and the same canonical attempt payload; do not retry malformed data indefinitely. Duplicate same-payload callbacks should be replay-safe; differing payloads under one key must be a conflict. Current Backend latest-attempt fencing remains P1-02.

Do not log tokens, private evidence, raw credentials or full sensitive payloads. Validate artifact ownership/checksum before inference and callback. The AI UI server has permissive CORS and local file/static endpoints; it is a test bench and must not be treated as production security evidence.
