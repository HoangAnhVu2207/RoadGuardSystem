# Survey completion and assessment notifications

Owner assignment: user follow-up on commit 368e556, 2026-11-22. Implement PM confirmation after mandatory assessment and Operator result inbox, preserve lifecycle scope/guards; normal push huy-review then fast-forward develop; no deployment or shared database migration.

Base/HEAD at start: `368e556e4c882570cbbeeed7002a6b9e1825c33d`, both origin branches matched after fetch. Worktree `D:\RG-web-pm-contracts`, branch `codex/survey-completion-assessment`, sole writer Huy/Codex. Main dirty checkout preserved.

Allowed files: SurveyRequest completion transition, new completion DTO/controller/service/repository seam; scope-bound receipt/transaction helper, existing assessment outbox producer, finite H6 registry/catalog/adapter/retry, nullable assessment navigation in notification scope, focused existing HTTP/SQL/model/contract tests, source evidence, runtime OpenAPI, inventory and common Mobile handoff. No schema or package/workflow changes, no FE edits, no privilege broadening or fabricated production status.

Decision: only PM currently authorized in task project confirms explicit ServerConfirmed dataset and its current pm-evidence-review.v1 assessment with all task segment/band position/quality/coverage PASS. No implicit newest dataset, integrity gate, Supervisor approval or completion across parent/child. Lock current PM authority through serializable completion command/replay; preserve task ETag and idempotency receipt. Record selected dataset/assessment/reason in immutable audit with completion. Existing assignment task detail/list is Operator readback.

Assessment notification: same transaction as persisted review; source/transport proof of exact assessment/dataset/task, current Operator assignment and membership. Add event to existing outbox retry/dedupe path, expose verified datasetId/assessmentId alongside task navigation; don't treat assessment as task completion. Existing FIELD/correction audit-only events unchanged.

Two self-review passes, actual test batches, previous CI and limitations are documented in the Mobile handoff. External review pending; HTTP tests use isolated SQL and fixture/mock storage, not deployed FE/app acceptance. No SQL integration identity change or new migration expected.

Owner follow-up: finish Survey work then include base CI fix from run 44927099895 / API job 136915475018. Downloaded artifact api-results 14716473906: exact notification scope field assertion outdated; preserve exact-set test and assert all new nullable IDs/code values. The sole MinIO smoke skip matches existing whitelist. Workflow/gates unchanged; old API result verifier step was skipped after dotnet test failure, not PASS. See handoff for actual batches and limits.
