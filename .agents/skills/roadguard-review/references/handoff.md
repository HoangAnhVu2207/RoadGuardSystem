# P1/P2 handoff and integration review

Read both actual plan rows; task numbers alone do not establish a dependency. Under the current policy the schema/persistence task needs Codex Implementer self-review and Codex-accepted Done before paired P1 implementation starts, and its artifacts must be present in the reviewed checkout. Historical Done remains historical evidence, not a claim of retroactive Codex review. A reviewer may still report useful defects while a dependency is blocked.

| Boundary | Compare across the two owners |
|---|---|
| Identity and access | P2 membership query uses current effective `ProjectMember.role_code`; P1 policy uses it before protected operations. Current server-side `User.role_code` controls Supervisor bypass. Role/session revocation must work end to end. P2-11 does not own P1-12 HTTP policies/tests. |
| Entity handoff | P2 establishes entity/property/enum shape; P1 adds domain invariants after handoff. Changed FK/nullability/enum/storage shape reopens schema ownership and needs a conflict decision. |
| Transaction | Domain state, append-only audit and outbox persist atomically; rollback leaves all unchanged. Verify service transaction boundaries and repository behavior agree. Rejection audit is required only where specified; never persist a rolled-back success event. |
| Idempotency | Key is scoped by current authorized actor/project/operation as specified. Same key/payload returns the same result; conflicting payload is rejected. Recheck current access before replay. Database uniqueness and worker retry behavior support the same contract. |
| Concurrency | Expected version reaches the database write. Stale decisions map to stable conflict responses; retries do not overwrite a later transition. Leases protect against stale worker completion, not just simultaneous acquisition. |
| Upload/AI | P2 storage/lease mechanisms support P1 checksum/confirmation/state policy. Only the backend worker confirms complete valid survey data; adapter results validate project/input/model provenance. Persist raw AI output immutably. |
| Detection/verification | Retention creates one OPEN defect plus inspection task atomically. PM verification consumes completed task/submitted measurements; schema support does not transfer decision policy to repositories. |
| Repair/evidence | Eligibility, current approved version, submitted snapshots and per-item integrity agree across service and persistence. An approved older version does not authorize assignment after the current pointer changes. |
| Retention/export | P1 authorizes requests/downloads and immutable approved scope; P2 executes and rechecks execution-time legal hold. Retry/partial output handling agrees with status, checksum and audit contracts. |
| Research | Imported pairs, units, exclusions and version provenance agree; no operational Defect/Warranty transition. Synthetic metrics are software evidence only. |

Trace one accepted path and applicable forbidden, stale, replay, rollback paths from API through service to SQL/worker. Report the owner for each defect; do not require both owners to duplicate the same layer's tests.

For shared hotspots (solution/project files, Program/DI, enums, plans/specs), compare the declared exclusive owner and integration sequence. Record an actual/potential overlap as a `Conflict warning` with files, task IDs, owner, required order and proposed resolution. Unclear ownership blocks affected edits, not independent read-only review. At Ready for review Codex Implementer freezes submitted artifacts and yields only the task's review/status sections to Codex; serialize writes to shared plans. If metadata ownership is blocked, report the verdict and deferred status write rather than claiming Done was recorded.

For P2-01, read its current branch-synchronization gate rather than freezing old SHAs here. Local branch logs/status and artifact comparison provide checkout evidence; remote freshness is unknown without an authorized fetch. A clean textual merge alone cannot establish semantic compatibility. Never merge, fetch, push or change branches merely to complete a review.
