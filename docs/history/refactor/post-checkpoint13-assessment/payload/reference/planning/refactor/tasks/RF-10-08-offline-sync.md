# RF-10-08: Offline sync, retry and supervised handover

> Current execution 2026-10-01: Anh alone owns R/C refactor on `anh`. F/G is unassigned for later development; proposed A/B text below is historical only. Future identity after a common baseline is Huy/A (`huy`), Anh/B (`anh`). See `../10-refactor-slices.md` and `../11-development-plan.md`.

- **Status/checkpoint:** PLANNED; re-record branch/HEAD/status. Report `planning/refactor/reports/RF-10-08.md`, checkpoint downloaded-reference contract, sync receipts and handover separately.
- **Goal:** reconcile R15-16/CG12 and Accepted 42A/43A: offline route/segment/destination/task geometry, encrypted export while device access remains, Supervisor approval, PM same-project receipt and original actor trace.
- **In scope:** BE sync operation/receipt/conflict/replay, permission recheck, geometry version and handover audit. **Out of scope:** Android local queue implementation, mandatory offline basemap, organizational recovery key not approved for pilot, deletion of unsynced evidence on token expiry.
- **Dependencies/decisions:** RF-10-01 current auth, RF-10-02 geometry/scope, RF-10-03 survey tasks, RF-10-04 file durability, RF-08 idempotency and RF-09 versioning; Android owner and wire/key protocol, Q-RF02-03 for survey write interpretation.
- **Read first:** R15-16, CG12, 42A/43A full text, `IdempotencyOperationService`, upload resume paths, draft `syncOperations`, project membership guard, Android/FE consumer samples when available.
- **Likely files:** versioned `contracts/events/` and HTTP sync schema, new sync Controller/Service/Repository/DTO/entities/migrations if approved, project authorization, focused API/SQL/offline fixture tests and Postman/`.http`.
- **Contract/data/consumer effect:** new BE route/receipt contract needs Android agreement; include actor/project/device/operation/fingerprint/version and conflict result without PII leak. Additive receipt schema preserves retries; no silent overwrite of old survey/file states. Local export encryption/key ownership documented as external protocol.
- **Steps:** agree downloaded reference/version and receipt semantics -> characterize existing per-command idempotency -> implement bounded sync batch with per-item durable result -> recheck current permission -> design supervised encrypted handover -> test retry, conflict and lost-device limit.
- **Verify:** isolated API and SQL duplicate/reordered/reconnect/stale-role/concurrent-writer tests, no loss of pending evidence, original-actor audit, wrong-project PM rejection, basemap absence does not block geometry operations; external Android contract tests when supplied.
- **Done when:** approved sync/handover semantics are wire-verified with Android and SQL receipts, all Accepted pilot constraints hold; missing external client/key proof yields Partial.
- **Recovery:** disable new sync endpoint while preserving accepted receipts/pending operations; replay after forward repair; no server action deletes client-local data as a rollback technique.

## Two-developer delivery supplement

- **Proposed owner:** B. Split dual-owner parent tasks into single-owner slices before edits. See [ownership plan](../03-two-developer-plan.md).
- **Pre-edit checkpoint:** record exact allowed files/symbols, read-only dependencies, shared-file reservation, baseline commit and preserved dirty changes. Record START / INTEGRATE / RELEASE gates for this slice; existing decision/data gates remain in force.
- **Independence:** use an agreed interface/version and fixture for consumer work; label test-double evidence separately. Do not claim parent Done before required real integration.
- **Self-review:** correctness pass plus ownership/consumer impact pass, safe in-scope autofix, focused verification and final diff review. Record unresolved findings.
- **Cross-owner impact:** create a note from [coordination template](../templates/coordination-note.md), recommend primary fixer and wait for the two developers to assign the overlapping change. Continue independent work; do not edit the other owner's files or duplicate their business logic.
- **Report:** [revised seven-section template](../templates/task-report.md); single-owner task may retain its existing report path, slices use `reports/<TASK-ID>-<SLICE>-<A|B>.md`. Record implementation status separately from delivery stage.
