# Crew local security and reconciliation

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

Use Expo SecureStore or equivalent OS-protected storage for bearer refresh tokens and private key material. Keep access tokens memory-first. Do not put raw evidence, presigned URLs, full ProblemDetails or private keys in logs, AsyncStorage or plain SQLite.

A production app should use an approved native encrypted database/file queue and explicit logout/key-loss policy, but SQLCipher, Keystore interoperability, background sync and encrypted local photos are not verified by this Backend repository. Mark those decisions `PROPOSED_EXPO` until an Expo native build and device test prove them.

## Reconciliation algorithm

1. Persist exact command body, K, source/task/item versions and origin before attempting a network command.
2. On timeout, classify `UNKNOWN`; query supported resource/receipt endpoints or replay the same K under current authority.
3. Treat `COMMITTED`/`REPLAYED` as durable effects only. Keep `PENDING_DEPENDENCY`, `STALE_SNAPSHOT`, `CONFLICT` visible and require snapshot/refetch or owner action.
4. Never replace an origin, reset first-start time, change original actor, or resolve conflicts with last-write-wins.
5. After revocation, membership expiry, key loss or snapshot staleness, stop mutation and require reauthorization. Preserve evidence for later handover/review.

Security boundaries are Backend-authoritative. Local encryption protects at-rest exposure but cannot prove signature ownership, sensor accuracy or true event time.
