# Web B read contracts

Status: ASSIGNED by owner request dated 2026-11-25 (B1, B3, B5, B6–B7,
B10, B12, B13–B14, B15). Writer: Huy. Working branch: codex/web-b-contracts;
delivery branch: huy-review. Base: b891e88ea20073bab7b110f301b7b37ebfa1c40c.
The isolated worktree was clean; the main checkout's existing changes are reserved.

## Scope and evidence

| Item | Authority / current source | Gap and action |
| --- | --- | --- |
| B1 | TARGET_CONFIRMED decision 33A and owner addendum 2026-11-25; CandidateMatchingService / AnhHuyFactsRepository | Case pins a route/set; GPS is per evidence. Add immutable explicit PM confirmation rather than derive a representative point. Use confirmed segments/topology and supported metric facts only. |
| B3 | Owner's exact current obligation responsibility requirement; RepairWorkflowRepository.GuardAsync | Item read works in receiver project. Trace source reads and add only item-scoped facts, without source-project membership. |
| B5 | Confirmed decisions and current role guards | Document facts vs PM-only matching/decisions, and designated Supervisor notification authority. Do not broaden final authority. |
| B6–B7 | Owner's scoped historical read requirement; FIELD history and repair history | Persisted submission/review/reuse records need discoverable exact identities. Preserve historical snapshots; metadata grants no content access. |
| B10 | Existing HandoverDocuments GET/POST | Verify and reuse inventory/attach, including aggregate ETag and negative scope cases. |
| B12 | Owner's safety history requirement; current selection filters | Add scoped discovery for used/resolved measures without weakening the selection collection or write guards. |
| B15 | Existing pinned policy, coverage and eligibility guards | Verify/read back actual pins, status and missing reasons. No invented source or threshold. |
| B13 | Current conclusion command returns 200 | Align response annotations/docs, distinguish publication 201. |
| B14 | Actual maintained scripts and available crash evidence | Trace submit/confirm. Missing exact external script/crash log remains UNKNOWN, not a guessed fix. |

## Verification and delivery

Read-only additions reuse current actor/project/obligation guards in a serializable
transaction. No write grant follows from a read grant. Keep legacy responses and
request/replay contracts. SQL mutation tests use owned disposable databases only.
Focused HTTP/SQL tests cover allowed actors, cross-scope denial, revoked authority,
history and legacy behavior. Run analyzer build, formatting, source contracts and
diff/secret review. Record two self-review passes and actual counts in the handoff.
Only the new explicit matching confirmation needs a migration. Runtime update
must preserve volumes, old image, bootstrap-off configuration and existing data.

## Implemented technical decisions

Owner addendum is recorded in `docs/product/owner-web-b-decisions-20261125.md`.
No existing workflow persisted a PM-confirmed Report location, so migration
`20261125053521_ReportMatchingLocationConfirmation` adds only the immutable append
table, restrictive FKs and per-Report sequence index. Report/Case/file/geometry
locks protect confirmation and receipt; the source version includes confirmation
identity. Down refuses a populated history. No legacy backfill or GPS edit.
SQL Server geography measures only already-valid WGS84 point pairs; a display-only
UTM inverse is not promoted to authoritative measurement. No uncertainty-overlap
model or automatic snapping is invented.

Receiver reads reuse current exact item responsibility. Source-task actions have
a closed GET allowlist, exact historical/current binding, and repeat the same
responsibility guard inside FIELD's transaction. Source images independently
recheck active lineage, stored verification and genuine AI provenance, then repeat
authorization after storage opens. No origin membership or write grant is created.
Package planning shared identity/version omits broad text for mixed responsibility.
Safety history resolves safety ownership separately from formal ownership.

Self-review pass 1: fixed EF projection ordering and SQL geography evaluation over
null rows; preserved old Report source versions when no confirmation exists;
confirmed replay checks authority without nested isolation conflict and same-key
concurrency returns the durable receipt. Migration-generated unrelated ownership
snapshot churn was removed.
Self-review pass 2: traced sibling/source denial, independent safety transfer,
post-open stream revocation/disposal, AI proof reuse, nullable history/readback,
retention inventory of matching confirmations, schema catalog and Seeder coverage.
Resolved-safety lifecycle is not manufactured: current source has no supported
independent resolve command; this limitation is explicit in the handoff.
