# Offline signed sync and handover

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

This is the Crew Mobile execution boundary. Backend offline operations also admit PM and, for grant/package preparation operations, Supervisor under separate guards. Operator and Reporter do not receive signed sync enrollment merely because they use Mobile Expo.

## Enrollment and snapshot

PM/Crew uses API-116 to register a device and API-118 to revoke it; API-127 creates a scoped snapshot and API-128 reads it. Registration binds project/device/public keys and current source/task scope. Private keys remain on the device; Backend does not issue or escrow a private key. Lost key, uninstall or device replacement can strand unacknowledged evidence; no recovery is inferred.

## Canonical signed batch

Use the exact Phase 2C wire: P-256 ECDSA, SPKI public key, IEEE P1363 64-byte signature, canonical Web JSON options, descriptor order by `OriginId.ToString("N")`, lowercase SHA-256 hashes and the declared manifest/source registration IDs. Claim digest and encrypted package signature are different formats. Do not use JSON.stringify, DER signatures, client timestamps or altered field order as substitutes.

API-130 accepts the signed sync envelope; API-113 creates an artifact, API-114/115 read artifacts/batches; API-122 imports an encrypted package; API-125 exports. API-126 package preparation and API-119/121 grant commands are Supervisor control-plane operations, not Crew commands. API-129 reconciles starts. Per-item outcomes include `COMMITTED`, `REPLAYED`, `PENDING_DEPENDENCY`, `STALE_SNAPSHOT`, `CONFLICT`; HTTP 200 is not an all-items ACK.

## Handover

A package may include source signature, recipient grant, recipient signature and admission. These are separate proofs. Handover does not turn recipient into original measuring Crew, does not prove physical sensor accuracy, person identity or historical UTC, and does not grant general PM/Supervisor authority. Preserve source actor, first-start/origin/task/snapshot hashes and receipts through import/reconcile.

`CURRENT_IMPLEMENTED_BE` is source/contract evidence. Native Expo key generation, P-256 interoperability, encrypted queue/package storage and background sync are `PROPOSED_EXPO`/`NOT_VERIFIED_EXPO_E2E`; .NET codec presence is not a device test.
