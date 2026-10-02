# RF-08: Shared platform seams without behavioral drift

- **Status/checkpoint:** DONE locally on `anh` at `2efc8a5775f834c7f0fe37cc0ce703011649e1f1`; implementation Done, delivery Ready for integration. See `planning/refactor/reports/RF-08.md`. Existing dirty work was preserved; no change commit.
- **Goal:** characterize and simplify only evidenced duplication in error mapping, authorization, idempotency/concurrency and outbox/transaction coordination (L15-16, CG01-04/13), preserving current public behavior.
- **In scope:** shared helper/interface extraction with same route/status/error codes and durable effects; dead-code candidate audit L09/L10. **Out of scope:** switching to uppercase errors, choosing `/me`/V2 survey, new dispatcher, deleting candidate types or public contract changes without separate approval.
- **Dependencies/decisions:** RF-07 pilot complete; RF-06 isolated tests; Q-RF02-02/03 and consumer decisions required only for behavior-changing stages. Reserve shared DI/ProblemDetails/DbContext one writer at a time.
- **Read first:** R01-03/R16-17, CG01-04/13, L09/L10/L15/L16, controllers' local mappers, `ApiErrorCodes`, `IdempotencyOperationService`, `RoadGuardTransactionService`, outbox registration/tests.
- **Likely files:** `API/Constants/ApiErrorCodes.cs`, selected controller mapper/helper, `API/Extensions/ServiceCollectionExtensions.cs`, shared Service/Repository interfaces and focused tests; paths narrowed in checkpoint before edits.
- **Contract/data/consumer effect:** no intended wire or schema change. Record per-operation old/new error/idempotency/concurrency matrix and FE/Postman consumers. Structural extraction must preserve existing rows and retry semantics.
- **Steps:** capture current behavior -> split into narrow seam tasks (error, auth, idempotency, outbox) -> extract only identical behavior -> run affected checks after each -> register any non-equivalent desired change for RF-09/10.
- **Verify:** affected-project build, focused HTTP status/body/header tests, wrong-actor/scope tests, isolated SQL replay/stale-writer/outbox atomicity; expand breadth only if shared middleware changes. No generic PASS from test count zero.
- **Done when:** each touched seam has equivalence proof and source references; L09/L10 are either retained or removal candidate with full caller/consumer audit; no unapproved endpoint status or data effect changes.
- **Recovery:** revert each independent seam before next; no migration. Keep old mapper available during canary if a separate public change is later approved.

## Two-developer delivery supplement

- **Proposed owner:** A: shared seams; B: module characterization and proposed outbox notes. Split dual-owner parent tasks into single-owner slices before edits. See [ownership plan](../03-two-developer-plan.md).
- **Pre-edit checkpoint:** record exact allowed files/symbols, read-only dependencies, shared-file reservation, baseline commit and preserved dirty changes. Record START / INTEGRATE / RELEASE gates for this slice; existing decision/data gates remain in force.
- **Independence:** use an agreed interface/version and fixture for consumer work; label test-double evidence separately. Do not claim parent Done before required real integration.
- **Self-review:** correctness pass plus ownership/consumer impact pass, safe in-scope autofix, focused verification and final diff review. Record unresolved findings.
- **Cross-owner impact:** create a note from [coordination template](../templates/coordination-note.md), recommend primary fixer and wait for the two developers to assign the overlapping change. Continue independent work; do not edit the other owner's files or duplicate their business logic.
- **Report:** [revised seven-section template](../templates/task-report.md); single-owner task may retain its existing report path, slices use `reports/<TASK-ID>-<SLICE>-<A|B>.md`. Record implementation status separately from delivery stage.

## Execution checkpoint (2026-10-01)

- START: RF-07 local pilot, RF-06 isolated SQL/API fixture and RF-06A data inventory were available. Three clean API source blobs and the dirty RF-07 P112 test hash are recorded in the RF-08 report; focused P112/P120/P121 baseline passed 19/19 before production edit.
- Authorized allowlist: one new internal API project-role parser and its identical direct callers in `ProjectAccessAuthorization`, `ProjectsController` and `ProjectRoadSectionsController`; task/report only otherwise. No shared DI, DbContext, ProblemDetails or transaction writer was taken. Error, idempotency and outbox groups were audited and retained/deferred on semantic differences.
- INTEGRATE: fresh API build 0 errors/0 warnings; same focused API filter 19/19 after; P202/P207 isolated SQL baseline 17/17; `git diff --check` exit 0. L09/L10 remain removal candidates with external consumers unknown. Self-review complete; peer review NOT REQUESTED.
- RELEASE remains open for hosted CI, deployed SQL/dispatcher and outside-repo consumers. Recovery is limited to the three caller edits and new helper; do not reset the checkout. RF-09 was not started.
