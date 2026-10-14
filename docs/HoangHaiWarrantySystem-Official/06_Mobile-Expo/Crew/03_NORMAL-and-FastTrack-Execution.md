# NORMAL and Fast Track execution

## NORMAL

PM proposes package/item (API-139/140), Supervisor approves (API-142), PM assigns Crew (API-143), Crew checks eligibility (API-146), starts and executes with API-161/157/160, requests review (API-150), PM reviews, then Supervisor performs final confirmation (API-147). A Crew client may read current item and submit assigned evidence; it cannot approve, assign, review as PM or final-confirm as Supervisor.

## Fast Track

Fast Track remains policy-controlled. PM publishes/revokes policy (API-162–167), Supervisor confirms authoritative road coverage (API-175/176), and the Backend evaluates current eligibility (API-146). Crew executes only when the server admits the current item and assignment. PM retains FT final authority (API-147); Supervisor notification/information producer remains P2-06 and must not be fabricated by the app.

Both modes require current project/task/item/binding/version, assigned Crew, evidence provenance and idempotency K. Eligibility is a preflight, not irrevocable permission. A 409/412 or 403 means refetch/stop; do not alter mode or state locally. AI candidate facts are not Defect authority and never become physical measurement. No normal approval or Fast Track final may be simulated offline without the signed registry contract.

**Evidence:** API-139–161, API-162–176, workflows 07/08, `RepairProducerService`, `RepairExecutionService`, `RepairPolicyService`. Runtime Expo UI and native execution tests are `NOT_VERIFIED_EXPO_E2E`.
