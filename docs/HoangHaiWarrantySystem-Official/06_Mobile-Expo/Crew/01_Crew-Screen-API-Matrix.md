# Crew screen/API matrix

<!-- CURRENT-DOCS-SYNC-BEGIN -->

## Current integration — 2026-10-27

`CURRENT_VERIFIED` static source at BE `01cc4725b178199215b02f4ddf7093491a16f2f7`, branch `huy-review`; FE read-only `hoang` / `afec0e714af1e6ea8c845af110996b84888efa07`. This integrated BE is later than Candidate3; exact Candidate3 deployment equivalence is **NOT_VERIFIED**.

50 controllers, 259 controller operations, 229 controller paths, 384 production schemas; 260 canonical IDs include conditional operational ADD-19 `/ready`. `/health` remains separate liveness. Counts come from disconnected production API Explorer/Swagger capture, excluding test controllers and their schemas. No database queries were executed for discovery. EF metadata: 200 entity types, 195 table identities, 1980 model properties and 1975 distinct mapped column identities; these are model counts, not a deployed SQL audit.

API030 media-type compatibility remains **NOT_VERIFIED**; H02–H05 **OPEN**, FULL_CONTRACT uncertified; FE acceptance, production, live external AI and native Mobile acceptance remain **PENDING / NOT_VERIFIED**.

[Current integration: G02–G06, B2, C1, FIELD/Crew, A1, API238 and Owner principles](../../../RoadGuard-Backend-Official/CURRENT-INTEGRATION-20261027.md). [Current 260-ID catalog](../../04_API-Contracts/02_Full-API-Catalog.md) preserves all existing identifiers. The current FE hoang tree is read-only and not the old Anh-test browser acceptance source. Old gap/role/transport rows below remain historical until revalidated individually; current resource authority and the current catalog control implementation navigation.

## Historical checkpoint (retained)

The following baseline, counts, execution labels and gap statements describe the dated checkpoint below. They are not current acceptance; use the current integration section and source-pinned inventories above.

<!-- CURRENT-DOCS-SYNC-END -->

Actor: `REPAIR_CREW`; client: React Native + Expo; bearer transport. Crew sees assigned FIELD/repair work only. PM and Supervisor remain authoritative for project, policy, assignment and approval.

| Screen | Reads | Commands | Required guards/states | Status |
|---|---|---|---|---|
| C-01 Session | none | API-002 login; API-003 refresh; API-007 logout | Secure token rotation and logout cleanup | `PROPOSED_EXPO_UI`, `NOT_VERIFIED_EXPO_E2E` |
| C-02 FIELD inbox | API-078 `GET /api/v1/projects/{projectId}/field-inspection-tasks` | None | Response must be filtered to current assigned Crew; empty/retry/403 | `CURRENT_IMPLEMENTED_BE`, `PROPOSED_EXPO_UI` |
| C-03 FIELD detail/map | API-080 detail, API-087 geometry, API-088 history, API-094 start-origin | None | Show task/Defect/source pins; do not expose projectwide pavement | `CURRENT_IMPLEMENTED_BE` |
| C-04 Accept/reject | API-080 | API-081 accept, API-091 reject | Exact task version/K; assignment may expire | `CURRENT_IMPLEMENTED_BE` |
| C-05 Start/measurement | API-080/087/094 | API-093 start, API-095 FIELD submission | Physical dimensions are Crew observations; first-start/original origin immutable; repair assessment is a separate C-09 command | `CURRENT_IMPLEMENTED_BE`; native capture `NOT_VERIFIED_EXPO_E2E` |
| C-06 Evidence | API-085/086 task evidence; API-234 upload; API-029 metadata | API-233 create, API-236 parts, API-235 complete; raw storage PUT | BEFORE/AFTER/MEASUREMENT as admitted; verified file/checksum/purpose/task; complete≠VERIFIED | `CURRENT_IMPLEMENTED_BE`, `PROPOSED_EXPO_UI` |
| C-07 FIELD review/supplement | API-080 task status, API-096 submission, API-088 history | API-095 FIELD resubmission | PM review API-092 is external to Crew commands; preserve `ParentSubmissionId`, original start and source lineage | `CURRENT_IMPLEMENTED_BE`; Expo UI `NOT_VERIFIED_EXPO_E2E` |
| C-08 Repair-attempt correction | API-080 task, API-096 FIELD submission; no dedicated repair-attempt GET established | API-159 repair-attempt supplement | PM API-158 review and Crew API-157 intake are POSTs, not history reads. K + If-Match/current repair binding mandatory; full Crew repair read/bootstrap remains `NOT_VERIFIED` (P5-G02) | `CURRENT_IMPLEMENTED_BE`; UI `PARTIAL`, `NOT_VERIFIED_EXPO_E2E` |
| C-09 Repair execution | API-146 eligibility, API-152 safety measure, API-080 task | API-156 assessment, API-161 start, API-160 finish, API-157 attempt, API-150 review request; API-153/154/155 safety | Crew cannot call item-read API-141/history API-148. Eligibility representation token is not command item rowversion; current item version/bootstrap is P5-G02 | `CURRENT_IMPLEMENTED_BE`; consumer readiness `PARTIAL` |
| C-10 Offline sync | API-114/115/117/120/123/124/128 scoped reads | API-113 artifact register; API-116 register/118 revoke device; API-127 snapshot; API-130 sync; API-122 import; API-125 export; API-129 reconcile | Supervisor-only API-119/121 grant and API-126 prepare are external dependencies, not Crew commands | `CURRENT_IMPLEMENTED_BE`; native crypto/queue `PROPOSED_EXPO` |
| C-11 Receiving request ACK | API-135 list, API-136 detail | API-137 ack | Current responsible recipient, immutable first ACK. API-138 appointment is Supervisor/PM-kind-guarded and unavailable to Crew; the former broad catalog metadata was corrected in Phase 5 (P5-G01) | `CURRENT_IMPLEMENTED_BE`; appointment denied to Crew by source |

Crew cannot call PM API-079/082/083/084/089/090/092, repair proposal/approval/final commands API-139/140/142/143/144/145/147/149, policy commands API-162–167, or Supervisor grant/confirmation commands unless crosswalk explicitly lists Crew. UI visibility never changes Backend authorization.

Phase 5 correction: the API columns contain only actor-admitted reads/commands; API IDs in the guard column describe external dependencies or explicit denials. Source guards override broad catalog actor strings. See `RepairWorkflowRepository.GuardAsync`, `BusinessDutyRepository.Guard`, `OfflineWorkflowService.ExecuteAsync`, `FieldInspectionWorkflowService.ExecuteAsync` and the [Phase 5 traceability audit](../../08_Testing-and-Operations/Phase5/04_Workflow-and-Evidence-Traceability.md).
