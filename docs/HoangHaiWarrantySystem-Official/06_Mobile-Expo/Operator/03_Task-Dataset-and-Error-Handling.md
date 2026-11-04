# Operator task, dataset and error handling

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

## State handling

Server task status is authoritative (`NewAssigned`, `Accepted`, `InProgress`, `Submitted`, `SupplementRequired`, `Completed`, `Rejected`, `Cancelled`, `Postponed` where the command permits). The app renders unknown states safely and never advances a task locally.

| Failure | Client action | Do not do |
|---|---|---|
| 401 | Refresh once with rotation; retry only the original request if the session remains valid | Loop refresh or log tokens |
| 403 | Show assignment/role/scope denial and stop | Hide denial by enabling another task |
| 404 | Remove stale task from active queue after user-visible notice | Recreate a task |
| 409/412 | Refetch task/upload/dataset and show version conflict | Last-write-wins |
| 428 | Collect required K/version/header and retry same command | Generate a new task identity |
| 429/5xx/timeout | Backoff; keep exact command as `UNKNOWN` until reconciliation | Blind duplicate upload/submit |
| complete accepted but not VERIFIED | Poll API-234; allow retry only under current session/version | Submit dataset as verified |
| network loss | Resume protected upload/command queue when online | Claim offline Operator signed sync is implemented |

Dataset submission must prove current assignment, active device, matching project/route/version, verified video/SRT/GPS files and integrity. PM owns supplement/reassignment through API-231/232. Operator cannot change model version, candidate decision or Defect state.

**Evidence:** API-226–232, API-233–236, `SurveyV2PersistenceService` and Phase 2A workflow 05. Runtime Expo/Android tests are `NOT_VERIFIED_EXPO_E2E`; Backend API tests do not prove native capture.
