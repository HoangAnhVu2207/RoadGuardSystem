# Export authority review — assigned 25/11/2026

Owner assignment: attached EXPORT follow-up, base1a2520f1eed516f2576fec18debadb773f821478.
Writer: Huy/root only. Reuse clean isolated worktree D:/RG-web-b-contracts,
branch codex/export-authority-review. Main checkout WIP7modified/9untracked preserved.
Fetched origin/develop and huy-review both at base. No FE or shared DB mutations.

TARGET_CONFIRMED for this task: preserve export membership gate unless a separate
explicit approved exception is found. Reporting/global Supervisor authority does
not authorize exports. Keep creator-only collection; separately verify detail,
manifest, bytes, worker/replay/source authority. No new migration expected.

Scope: ExportRepository membership predicate if confirmed inconsistent with
admission; focused real HTTP/SQL/MinIO tests; export OpenAPI descriptions/handoff.
Read authority: confirmed-decisions41A is retention policy, not global export grant;
owner-web-b-decisions B3 concerns scoped source reads, not blanket dossier export.
Current source ExportService.AuthorizeAsync, ExportRepository admission/source
authority, CollectionReadRepository before-pagination creator filter establish
implementation only. Owner's current assignment supplies keep-policy authority.

Correlation6d46f061-7f15-47ba-bc6c-7edc5f8c8dc7: no hit in current API container log;
do not assign actor or infer POST. Further saved logs searched outside repo.

Verification: Supervisor no membership vs matched membership, role mismatch,
Web cookie/CSRF and Bearer; current role/account/member validity and revocation;
creator pagination/current direct reads; genuine worker/private MinIO bytes;
existing repair/source/transfer/storage authority regression. Exact wire errors
and correlation after middleware, snapshot immutable. Two actual self-reviews,
build/analyzer/format/source gates and scoped TRX. Normal huy-review push then FF
develop with fetch/ancestry guards; production update API-only local if needed.
No gate/skip weakening, no shared seed/reset. External UI review remains unverified.

## Actual review and evidence

Pass1: traced collection→common export gate→admission/replay→snapshot sources→
worker/render/complete→manifest/content→post-OpenRead. Confirmed role predicate
inconsistency: HTTP Web/Bearer with Supervisor account + Active PM membership
returned200 on reads while admission rejects that role. New regression failed
Expected403/Actual200 before fix, passes after adding current user-role equality.
No expansion of membership/global Supervisor rights. Creator filtering unchanged.

Pass2: checked exact source evidence237IDs (four excerpts updated minimally),
nullable/error wire vs middleware, source history/hash immutability, no migration,
scope and runtime update plan. Corrected handoff to job.completeness (not manifest
availability), and direct403forbidden vs collection403access_forbidden. Correlation
on direct errors is response header, not promised in JSON. Format-only fixture
layout fixed without changing assertions. No external peer review claimed.

Executed final HTTP group90PASS/0FAIL/0SKIP, unit export32PASS/0FAIL/0SKIP; these
are focused regression, not a new full-suite pass. Genuine isolated SQL/MinIO,
Web cookie auth and real renderer/private bytes; repair source history seeded,
not a fresh Crew end-to-end workflow claim. No browser/FE acceptance.
Base CI1a2520f completed success on both branches at start; final CI recorded at
handoff. SQL export, final format/source gates and TRX outcomes recorded separately.
SQL export28PASS/0FAIL/0SKIP; Swagger/exact-source final37PASS/0FAIL/0SKIP overlapping
HTTP90; format whole solution/source10/TRX gates passed. Full API/four full SQL
lanes not rerun in this narrowly scoped task; no hosted result inferred from local.

Follow-up review after first publication: add exact combined export/transfer
assertions to existing REPORT/SURVEY theory. First run1PASS/1FAIL because total-job
assertion incorrectly counted both theory projects on their shared fixture DB;
scope the assertion to its project (retaining exactly-one requirement). Final2PASS:
old source/replay403, mixed package and receiving-project export422, worker denied,
snapshotHash unchanged. No production change after b24e998; follow-up test/docs
commit preserves published history, no changed SQL identity or migration.
