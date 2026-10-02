# RF-10-08 per-command idempotency assessment (proposed follow-up)

## 1. Ket qua va pham vi

Read-only production assessment + docs-only correction, 2026-10-02; sole current writer per owner request. Recommendation **RETAIN**, ready for reviewer. Implementation: not assigned/not performed; parent RF-10-08 and overall RF-10 remain Partial. Delivery: Draft / Ready for review (no integration/release).

Task authority: `../tasks/RF-10-08-offline-sync.md` defines the parent and existing-command characterization. `../10-refactor-slices.md` contains a 10-08-R01 candidate but no separate R01 definition/approved acceptance scope was found in tasks. This report deliberately uses `RF-10-08-assessment.md`; do not mark RF-10-08-R01 Done. Owner requested assessment now, not refactor implementation/sync/F/G.

## 2. Baseline va quyen sua

Branch anh / HEAD 2efc8a5775f834c7f0fe37cc0ce703011649e1f1 verified against handoff. Dirty paths and exact bytes/hashes: `../evidence/post-checkpoint13-assessment/payload/baseline.json`. 12 current docs snapshotted before editing; 1000 read-only files fingerprinted. Source includes dirty SurveyV2PersistenceService and existing project authorization/controller edits; HEAD is not the full source baseline.

Allowlist: 12 docs named in baseline, these two assessment reports, and new evidence subtree only. No production/test/fixture/schema/contract/migration/CI edit. No Git mutation/build/test/DB. Sole writer reservation; no concurrent task or delegation. START authorized by pasted owner request; INTEGRATE pending reviewer; RELEASE unassigned.

## 3. Thay doi va doi chieu yeu cau

| Requirement / gap | Source / deliverable | Result and evidence |
|---|---|---|
| Trace current callers | `../evidence/post-checkpoint13-assessment/idempotency-matrix.md` | 25 primitive call sites, 14 files / 13 classes; operation/route/actor/nullable-project/key/fingerprint/outcome/auth/precondition/exception boundaries. SOURCE_INSPECTED |
| Separate equivalent repetition from domain policy | IdempotencyOperationService.ExecuteAttemptAsync versus ConsumerEffectService/RoadGuardTransactionService and callers | RETAIN transaction/replay/outcome adapters. Pure SQL error predicate is a small possible later candidate; no forced caller count/extraction |
| Correct P create ordering | ProjectCreationService.CreateAsync and ProjectCreationPersistenceService.TryGetReplayAsync | Role/input -> replay probe -> facts on miss -> ExecuteAsync, rather than old report's simplified facts-before-all-replay claim |
| Accepted characterization | Owner pasted request C/D; correction-04 TRX/source bytes | HISTORICAL reused runtime 10/10; current test hash verified. No new run, no universal scope guarantee |
| Checkpoint-13 docs/provenance | `findings-ledger.md`, `provenance.md`, current slices/checklist and reports | Fixed wording/mapping, preserve old JWT and historical linkage limits; full before/current docs in package |

RETAIN rationale: Upload Complete throws/rolls back stale version; V2 postpone stores failed outcome; P create probes receipt before changed facts after authorization; warranty reevaluates facts before receipt; Part URLs and FileRepository have external storage before SQL and compensation. A shared auth/precondition/transaction runner would change durable receipts, public errors or external effects. Paths/symbols and per-recommendation gates are in matrix.

Source findings A08-01 (callback fingerprint omits identifying fields) and A08-02 (validation failed replay remap) are deferred independent behavior findings. No defect fixed here. Schema/contract/data/recovery impact of this turn N/A; retain before docs for correction recovery.

## 4. Self-review va autofix

Pass 1 compared every matrix row with scope/fingerprint/outcome/check ordering and exceptions; rechecked runtime test assertions separately from source skip-handler proof. Pass 2 checked status authority, dirty preservation, producer/consumer differences and all write paths. Corrections include 14 source files versus 13 classes, distinct survey plan/request evidence and no numeric extraction threshold. Detailed record: `../evidence/post-checkpoint13-assessment/review.md`. Peer review PENDING; no subagent/peer evidence claimed.

## 5. Kiem chung

| Check | Evidence | Result / limit |
|---|---|---|
| Git branch/HEAD/status | baseline.json / checks/current-state.json | CURRENT_VERIFIED; no commit or branch change |
| Source/caller/task search | checks/idempotency-callers.json, task-ids.json | CURRENT_VERIFIED source only; no new R01 definition |
| Source/archive/TRX/hash mapping | checks/reused-trx.json, runtime-archive-source.json, external-runtime-archives.json | Reused correction-04 10/10, source after-hash a7bbae78...; before 6f0bebf1... differs; assembly/linkage gap NOT_VERIFIED |
| Read-only drift, doc diffs, archive extract | checks/current-state.json, diffs/, external archive-verification.json | Fresh static/package verification; counts from generated files |
| Build/tests/DB/provider/full validators | NOT RUN | Explicit assessment/docs-only constraint; no new runtime PASS claim |

## 6. Phoi hop va quyet dinh

No cross-owner note required: sole writer, no production fix assigned. Proposed follow-ups in findings ledger require actual task definition/owner and precise compatibility gates. Callback fingerprint additions alter persisted identity semantics, requiring version/key rollout decision with RF-10-05/AI owner. Q-RF02-03/04/07, Android protocol and F/G remain untouched; decisions do not block this independent read-only deliverable.

## 7. Checkpoint va ban giao

Assessment delivered for reviewer, RETAIN recommendation, proposed follow-up only. RF-10-08-C01 bounded assertions accepted per owner handoff; this turn does not add reviewer acceptance or close checkpoint-13/parent/overall. Docs correction ready for review, residual findings/provenance explicit. Handoff: `../evidence/post-checkpoint13-assessment/RF-10-post-checkpoint-13-assessment-handoff.zip`; external SHA-256 sidecar and archive-verification.json. Stop after handoff; no implementation/F/G/commit/push. Planner: review findings and document closure first, define exact future tasks only when assigned.
