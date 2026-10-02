# RF-10-03-C02 Survey Request, Task and Assignment Baseline

## Scope and evidence

- Branch `anh`, HEAD `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; working tree was already dirty. This is current local characterization only; no production, contract, schema, migration, CI or shared database change was made.
- `CURRENT_VERIFIED`: `SurveyPlanningController.CreateRequest` creates a `SurveyRequest` through the old road-section route. `SurveyV2Controller.CreateTask` creates a `SurveyRequest` plus an active `SurveyAssignment`; `GetTask` joins the assignment-backed path.
- `RUNTIME_TESTED`: final `Rf1003RequestTaskAssignmentCharacterizationTests` passed 1/1 on the owned SQL Server API fixture. `old request -> V2 GET` returned 404; assigned operator GET returned 200; Reporter GET returned 403; manager A POST creating in project B returned 403 while the project-B manager positive control succeeded; replay returned the same task and SQL counts remained unchanged; missing idempotency header returned 428. Durable request/assignment state was checked in a new DbContext.
- `HISTORICAL_RUNTIME`: P122 and P2 existing tests remain separate-family evidence and are not counted as this cross-family test.
- `NOT_VERIFIED/N/A`: no production route was found that assigns an old request; no reverse old mutation route exists for a V2 task; external clients, deployed rows and coexistence policy remain unknown.
- `HISTORICAL_LIMITATION`: Checkpoint 08 corrections were applied without before-snapshots; this baseline documents the gap but does not retroactively reconstruct missing snapshots.

## Current matrix

| Flow | Route and actor | Durable writes | Read/mutation result | Replay/precondition | Status |
|---|---|---|---|---|---|
| Old request | `POST /api/v1/projects/{project}/road-sections/{section}/survey-requests`, authenticated project manager | One `SurveyRequest`; no assignment | V2 task GET on the same request id returned 404 | Old `operationId`; no V2 `Idempotency-Key` or `If-Match` | CURRENT_VERIFIED + RUNTIME_TESTED |
| V2 task | `POST /api/v1/projects/{project}/survey-tasks`, authenticated project manager, operator must be active drone operator | One `SurveyRequest` and one active `SurveyAssignment`, same project | Assigned operator GET returns 200; Reporter GET returns 403; manager A POST creating a task in project B returns 403. | Required `Idempotency-Key`; replay returns same task without extra request/assignment; task row has rowversion; missing key returns 428 | CURRENT_VERIFIED + RUNTIME_TESTED |
| Assignment lifecycle | C02 uses `SurveyV2Controller` -> `SurveyV2Service` -> `SurveyV2PersistenceService.MutateTaskAsync`; `SurveyAssignmentPersistenceService` is a separate path | C02 V2 reassignment ends prior assignment and adds replacement plus audit; no outbox enqueue is present in this call chain. Separate assignment repository creates outbox. | No old-request production path was found in repo | Concurrency/operation receipt handled by C02 V2 repository path; not exercised for old request | SOURCE_INSPECTED; old path NOT_VERIFIED |

## Findings

### Checkpoint 09 correction coverage

- Old request replay reuses the exact payload and `operationId`; the same request identity is returned and fresh request/assignment/audit effects are unchanged.
- Wrong-project proof uses a valid project-B scope. A project-B manager positive control succeeds; manager A receives `403`, and fresh project-B request/assignment/audit effects are unchanged.
- V2 create replay snapshots task identity, project, active operator, status, rowversion, assignment count and audit count before and after replay. The current reassigned projection is preserved.
- After reassignment, operator B GET returns `200` with the task identity, project, operator B and `REASSIGNED` status; fresh SQL confirms the active assignment.
- Outbox status is `SOURCE_INSPECTED` only for the C02 V2 call chain. `SurveyV2PersistenceService.MutateTaskAsync` does not enqueue an outbox message; `SurveyAssignmentPersistenceService` is a separate source path and must not be used as C02 runtime evidence. No outbox runtime claim is made.
- Checkpoint 10 correction: old replay reads the actual `SurveyRequest` row and project request-id set; V2 create, stale reassign and reassign replay read `IdempotencyRecords` by actor/project/operation/key. The stale request gets one receipt containing the observed concurrency outcome. Snapshot comparisons use content equivalence, not reference equality.
- V2 create replay response is asserted as the stored create projection (`operatorId` A, `NEW_ASSIGNED`, original version), while fresh SQL confirms the task has since been reassigned to operator B. This is observed stored create-time replay behavior; SQL keeps durable reassignment. No claim that replay response reflects current state after later mutations.

- `RF10-03-C02-F01` (high): old request and V2 task share `SurveyRequests` but differ in assignment creation and V2 read visibility. Keep both paths; do not infer coexistence or backfill policy.
- `RF10-03-C02-F02` (medium): V2 task creation is the reachable production path that establishes `SurveyAssignment`; no valid old-request assignment creator was identified in the repository. Treat old-to-task linkage as UNKNOWN, not as an absent external consumer.
- `RF10-03-C02-F03` (medium): V2 idempotency/precondition headers are mandatory while old request uses body `operationId`; no cross-family deduplication claim is supported.
- `RF10-03-C02-F04` (medium): Assigned operator GET returns 200; Reporter GET returns 403; manager A POST creating a task in project B returns 403. This is current authorization behavior; no target policy is inferred.
- `RF10-03-C02-F05` (medium): assignment reassignment and stale `If-Match` behavior are covered by C02 with separate fresh SQL snapshots before/after stale and replay. The stale key creates one idempotency receipt containing the current concurrency outcome; C02 does not generalize this to all rejected requests and did not invent an old-request assignment path.

## Decisions and limits

Q-RF02-03 remains the gate for row ownership, old-row exposure and any compatibility/backfill. No Q11 or target coexistence rule was accepted. F/G work, schema changes and consumer migration are outside C02.
