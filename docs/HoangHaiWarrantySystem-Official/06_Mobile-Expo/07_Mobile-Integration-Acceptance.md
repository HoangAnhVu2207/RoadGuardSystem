# Mobile Integration Acceptance

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

These are acceptance scenarios for a real React Native + Expo Android/iOS build. Backend test presence is not Expo E2E evidence.

| ID | Scenario | Expected proof | Status |
|---|---|---|---|
| M-01 | Operator bearer login/refresh/logout | `/auth/android/*` transport, rotation and cleanup | `NOT_VERIFIED_EXPO_E2E` |
| M-02 | Operator assigned task accept/decline | API-225–230, current assignment/version and 403 for wrong task | `NOT_VERIFIED_EXPO_E2E` |
| M-03 | Video/SRT multipart resume | API-233/236/235/234, checksum/ETag, complete≠VERIFIED | `NOT_VERIFIED_EXPO_E2E` |
| M-04 | Dataset submit | API-229 rejects unverified/mismatched scope; missing telemetry not PASS | `CURRENT_IMPLEMENTED_BE`, device test NOT_RUN |
| M-05 | Crew FIELD accept/start | API-078/080/081/093, immutable origin and assignment guard | `NOT_VERIFIED_EXPO_E2E` |
| M-06 | Crew physical measurement | Dimensions originate from Crew form/evidence, never AI bbox | `TARGET_CONFIRMED`, native test NOT_RUN |
| M-07 | Crew evidence upload | BEFORE/AFTER/MEASUREMENT purpose, verified polling and retry | `NOT_VERIFIED_EXPO_E2E` |
| M-08 | PM supplement → Crew resubmit | FIELD: PM API-092 → Crew API-095 with latest ParentSubmissionId, K/task version; repair: PM API-158 → Crew API-159 with repair attempt/version | `NOT_VERIFIED_EXPO_E2E` |
| M-09 | NORMAL authority | Supervisor approval and final cannot be executed by Crew | `CURRENT_IMPLEMENTED_BE`, E2E NOT_RUN |
| M-10 | Fast Track authority | eligibility/policy/coverage gates; PM final; Supervisor information boundary | `CURRENT_IMPLEMENTED_BE`, P2-06 open |
| M-11 | Device enrollment | API-116/118, key custody and revoke fail closed | `NOT_VERIFIED_EXPO_E2E` |
| M-12 | Signed batch interoperability | P-256/SPKI/P1363 canonical bytes and per-item receipts | `NOT_VERIFIED_EXPO_E2E` |
| M-13 | Snapshot conflict/replay | stale snapshot and duplicate K reconcile without last-write-wins | `NOT_VERIFIED_EXPO_E2E` |
| M-14 | Handover/key loss | encrypted package admission, recipient separation, stranded data warning | `NOT_VERIFIED_EXPO_E2E` |
| M-15 | Offline storage inspection | tokens/private keys/photos/queue protected and logout cleanup | `NOT_VERIFIED_EXPO_E2E` |

Run API ID/route/actor validation against Phase 2C before device testing. Do not call a test PASS from static contract presence alone.
