# Crew screen/API matrix

Actor: `REPAIR_CREW`; client: React Native + Expo; bearer transport. Crew sees assigned FIELD/repair work only. PM and Supervisor remain authoritative for project, policy, assignment and approval.

| Screen | Reads | Commands | Required guards/states | Status |
|---|---|---|---|---|
| C-01 Session | Auth login/refresh/logout | Auth commands | Secure token rotation and logout cleanup | `PROPOSED_EXPO_UI`, `NOT_VERIFIED_EXPO_E2E` |
| C-02 FIELD inbox | API-078 `GET /api/v1/projects/{projectId}/field-inspection-tasks` | None | Response must be filtered to current assigned Crew; empty/retry/403 | `CURRENT_IMPLEMENTED_BE`, `PROPOSED_EXPO_UI` |
| C-03 FIELD detail/map | API-080 detail, API-087 geometry, API-088 history, API-094 start-origin | None | Show task/Defect/source pins; do not expose projectwide pavement | `CURRENT_IMPLEMENTED_BE` |
| C-04 Accept/reject | API-080 | API-081 accept, API-091 reject | Exact task version/K; assignment may expire | `CURRENT_IMPLEMENTED_BE` |
| C-05 Start/measurement | API-080/087/094 | API-093 start, API-095 submissions; API-156 assessment where applicable | Physical dimensions are Crew observations; first-start/original origin immutable | `CURRENT_IMPLEMENTED_BE`; native capture `NOT_VERIFIED_EXPO_E2E` |
| C-06 Evidence | API-085/086; API-233/234/235/236 upload routes | Upload BEFORE/AFTER/MEASUREMENT; then typed submission | Verified file, checksum, purpose and task scope; complete≠VERIFIED | `CURRENT_IMPLEMENTED_BE`, `PROPOSED_EXPO_UI` |
| C-07 FIELD review/supplement | API-096 `GET .../field-inspection-tasks/{taskId}/submissions/{submissionId}` | API-095 creates a FIELD submission; PM review API-092 | FIELD submission and its evidence are distinct from repair-attempt correction; PM review can request a new FIELD submission through the source workflow | `CURRENT_IMPLEMENTED_BE`; Expo UI `NOT_VERIFIED_EXPO_E2E` |
| C-08 Repair-attempt correction | API-157/158 history and PM review | API-159 `POST .../attempts/supplements` | `API-159` is **repair-attempt supplement only**; it is not a generic FIELD supplement. Current repair item/task/binding, assigned Crew, K and If-Match are required | `CURRENT_IMPLEMENTED_BE`; Expo UI `NOT_VERIFIED_EXPO_E2E` |
| C-09 Repair execution | API-141 item read, API-146 eligibility | API-153/154/155 safety; API-157 attempt, API-160 finish, API-161 start, API-150 review request | Current assigned Crew/item/binding/authority; NORMAL/FT policy gates | `CURRENT_IMPLEMENTED_BE` |
| C-10 Offline sync | API-113–130 | API-116/118 device, API-127 snapshot, API-130 sync, API-122 import, API-129 reconcile | Enrollment/key custody/source snapshot/receipts; no arbitrary replay; only Backend-admitted Crew paths | `CURRENT_IMPLEMENTED_BE`; native crypto/queue `PROPOSED_EXPO` |
| C-11 Receiving handover | API-135–138 | API-137 ack/138 appointment | Current responsible recipient; first ACK immutable | `CURRENT_IMPLEMENTED_BE` |

Crew cannot call PM API-079/082/083/084/089/090/092, repair proposal/approval/final commands API-139–149, policy commands API-162–167, or Supervisor grant/confirmation commands unless crosswalk explicitly lists Crew. UI visibility never changes Backend authorization.
