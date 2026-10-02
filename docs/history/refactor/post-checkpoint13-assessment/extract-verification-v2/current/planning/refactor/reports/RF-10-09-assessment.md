# RF-10-09 notification/outbox assessment (proposed follow-up)

## 1. Ket qua va pham vi

2026-10-02, read-only production assessment and docs correction, sole writer. Recommendation **RETAIN**; ready for reviewer. Implementation not assigned/not performed; parent RF-10-09, RF-10 overall and RF-11 remain Partial. Delivery Draft / Ready for review, not Integrated/Released.

`../tasks/RF-10-09-messaging-reporting-retention.md` defines parent dispatch/reporting/retention with separate gates. `10-09-R01` is a ledger candidate, no separate task definition or acceptance scope found. Report named `RF-10-09-assessment.md`, proposed follow-up; no fabricated R01 Done task. Owner explicitly excluded dispatcher/provider/new endpoints/abstractions/migrations and runtime reruns.

## 2. Baseline va quyen sua

Branch anh, HEAD 2efc8a5775f834c7f0fe37cc0ce703011649e1f1 verified; dirty full source baseline/status and 12 doc before snapshots in `../evidence/post-checkpoint13-assessment/payload/baseline.json` / before/. All production, tests, fixtures, schema, contracts, migrations and CI read-only; allowlist limited to named docs, assessment reports and new evidence subtree. 1000 read-only files fingerprinted for final comparison. No Git change/build/test/DB or external delivery. START owner request; INTEGRATE reviewer pending; RELEASE unassigned.

## 3. Thay doi va doi chieu yeu cau

| Requirement | Path / symbol + behavior | Evidence / limit |
|---|---|---|
| HTTP auth/projection/pagination | NotificationsController -> NotificationService -> NotificationPersistenceService; recipient filter, JSON/Base64 cursor descending time/ID, missing headers 428 | SOURCE_INSPECTED + BOX 2 correction-03 reused runtime; see notification-matrix.md |
| Consumer receipt | NotificationOutboxConsumer.ConsumeAsync -> ConsumerEffectService.ProcessAsync; message/notification-inbox -> stored EffectId | Correction-03 reused direct test, fresh context/different candidate; not provider or HTTP proof |
| MarkRead | NotificationPersistenceService.MarkReadAsync; actor/null/NotificationRead/key, version inside handler, failure outcome stored | Correction-03 state after first success vs after replay + conflict chain only |
| Outbox producer | SurveyAssignmentPersistenceService.ReassignAsync and separate project/defect/admission/processing paths | Source graph distinguishes actual service/controller from registered/direct-only producers; no automatic chain inferred |
| Lease/retry | OutboxWorkRepository + OutboxMessage | Source locks/owner/status/due/attempt checks; supplied retry delay; five focused cases do not execute lease/retry |
| Dispatcher search | API/Services/Repositories, excluding Migrations/bin/obj, normal rg ignore rules | No notification work-repository/consumer caller found; ValidationRunWorker -> CompleteNextValidationRunAsync DIRECT outbox worker found. No global NOT_IMPLEMENTED/deployed backlog claim |
| Docs corrections | current notification baseline/C01 report, slices/checklist, findings/provenance | Corrected attribution/default/header/cursor/payload/backoff and historical package coverage; source untouched |

RETAIN (PROPOSED): command receipt and consumer receipt have different identities/result contracts; specialized validation worker differs from owner-checked work repository; notification query and cursor differ from inspection/survey. Moving these into one runner/pager risks transaction participation, lease ownership, stale/replay behavior and wire compatibility. Each recommendation has path/symbol/behavior/gate in `../evidence/post-checkpoint13-assessment/notification-matrix.md`.

A09-01/02 flag source-level lease exhaustion/fencing risks for a separately defined behavior task. No production fix, scheduler or migration performed. Contract/data effect N/A.

## 4. Self-review va autofix

Pass 1 traced actual worker/caller registrations, recipient query, failed-outcome receipt and entity lease rules; removed false full-repository dispatcher absence and automatic exponential-backoff claims. Pass 2 checked source preservation, historical run attribution, full Phase A docs, manifest completeness and status/owner boundaries. `../evidence/post-checkpoint13-assessment/review.md` records correction/checks. Peer review PENDING; actual deployment/provider integration NOT RUN.

## 5. Kiem chung

| Check | Evidence | Result / limit |
|---|---|---|
| Git/source/caller/task searches | baseline.json; checks/outbox-reachability.json, producer-callers.json, task-ids.json | CURRENT_VERIFIED local source search, not dynamic deployment |
| Source/TRX/archive hash | checks/reused-trx.json, external-runtime-archives.json, runtime-archive-source.json | BOX 2 correction-03 reused 5/5; current hash f6995308... unchanged; BOX 1 is separate correction-04 10/10 |
| Read-only hashes/doc diff/links/archive | checks/current-state.json, diffs/, static-checks.json; archive-verification.json | Fresh static verification, no runtime claim |
| Build/test/DB/dispatcher/provider/CI | NOT RUN | Explicit scope; no lease timing/crash/recovery run or real delivery proof |

## 6. Phoi hop va quyet dinh

No cross-owner edit or coordination note needed for sole-writer assessment. Proposed lease behavior tasks require owner/fencing/crash/retry policy and isolated SQL assertions; notification dispatcher/recipients/channels and external consumer/provider agreement remain F/G. Q-RF02-08 gates retention/deletion; no new business approval needed to finish this docs/read-only scope.

## 7. Checkpoint va ban giao

Bounded RF-10-09-C01 accepted per owner's handoff; RETAIN assessment and checkpoint-13 docs corrections delivered for reviewer, closure PENDING. New recommendations are proposed follow-ups, no R01 or parent Done. Archive under `../evidence/post-checkpoint13-assessment/`, exact hash/counts in external sidecar/verification. Planner: review matrices, finding mappings and provenance limitations; no scheduler/F/G/implementation/commit/push starts from this turn.
