---
name: roadguard-review-p1
description: Use when Codex reviews RoadGuard Person 1 tasks (P1-*), domain, Services, DTO/API contracts and Antigravity self-review evidence for mandatory acceptance, or an explicitly read-only review. Not for implementing fixes or assigning P2 persistence work to P1.
---

# Review Person 1 tasks

Read the [shared review contract](../roadguard-review/references/review-contract.md) first, then the assigned row/dependencies in [the P1 plan](../../../planning/RoadGuard_Plan_Person_1.md). Apply the [task checklist](references/task-checklist.md) only to the requested slice. The shared contract supplies implementation/metadata boundaries, acceptance authority, evidence rules, severity and report structure; do not route recursively through the general skill.

## Follow the command or query

Trace API DTO -> authentication/current-user -> current project authorization -> service orchestration -> domain transition -> repository/transaction -> response/error/audit. Read actual callees before reporting an absent guard.

- Confirm current server-side role, active/effective membership and operation permission, including replay and download. JWT role/project claims alone are insufficient. Account-wide Admin operations follow their task contract, not an invented project-membership rule.
- Keep controllers thin, DTOs public, EF entities private; check validation, stable error codes, ProblemDetails, correlation and sensitive-data exposure.
- Domain methods own entity-local invariants; Services owns cross-aggregate checks and transactions. Check stale-version decisions and each state precondition, including immutable submitted/approved/evidentiary content.
- Verify retries preserve state/audit consistency without duplicate domain effects. Check atomic failure paths and reauthorization before returning cached outcomes.
- Match unit and API tests to accepted state/output, forbidden role/project, invalid transition, retry and stale version where relevant. HTTP 200 alone does not prove the contract. P1 tests do not substitute for required P2 SQL proof.

Check the paired P2 artifact is present and its plan dependency is satisfied before assessing readiness. Schema changes after handoff require the documented ownership decision. For a cross-layer defect read [handoff checks](../roadguard-review/references/handoff.md) and identify which owner must change which contract; read-only review does not authorize edits.

Return concrete findings, separate verification gaps, and a scoped verdict in the user's language. Codex records review/status and may mark Done only after the full task acceptance gate; Antigravity stops at Ready for review. Explicit report-only means no file/status edits. Do not start future P1 tasks or substitute passing tests for missing AC/dependency/self-review evidence.

```text
Dùng $roadguard-review-p1 review P1-41 trong diff hiện tại. Đối chiếu TN05/TN06, quyền PM, measurement đã submit, concurrency và audit; chỉ báo lỗi, không sửa.
```
