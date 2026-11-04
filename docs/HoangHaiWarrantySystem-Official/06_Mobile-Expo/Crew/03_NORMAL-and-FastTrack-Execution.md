# NORMAL and Fast Track execution

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

## NORMAL

PM proposes package/item (API-139/140), Supervisor approves (API-142), PM assigns Crew (API-143), Crew checks eligibility (API-146), starts and executes with API-161/157/160, requests review (API-150), PM reviews, then Supervisor performs final confirmation (API-147). Crew cannot approve, assign, review as PM or final-confirm as Supervisor. API-141 item detail and API-148 history are PM/Supervisor guarded in `RepairWorkflowRepository.ReadItemAsync/GuardAsync`; they are not Crew read APIs. The eligibility ETag is a representation hash, not the item rowversion for repair commands. Establishing the complete Crew read/bootstrap path remains P5-G02 in the [Phase 5 traceability audit](../../08_Testing-and-Operations/Phase5/04_Workflow-and-Evidence-Traceability.md).

## Fast Track

Fast Track remains policy-controlled. PM publishes/revokes policy (API-162–167), Supervisor confirms authoritative road coverage (API-175/176), and the Backend evaluates current eligibility (API-146). Crew executes only when the server admits the current item and assignment. PM retains FT final authority (API-147); Supervisor notification/information producer remains P2-06 and must not be fabricated by the app.

Both modes require current project/task/item/binding/version, assigned Crew, evidence provenance and idempotency K. Eligibility is a preflight, not irrevocable permission. A 409/412 or 403 means refetch/stop; do not alter mode or state locally. AI candidate facts are not Defect authority and never become physical measurement. No normal approval or Fast Track final may be simulated offline without the signed registry contract.

**Evidence:** API-139–161, API-162–176, workflows 07/08, `RepairProducerService`, `RepairExecutionService`, `RepairPolicyService`. Runtime Expo UI and native execution tests are `NOT_VERIFIED_EXPO_E2E`.
