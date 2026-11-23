# BE–AI business, persistence and integration readiness audit

Status: owner-requested audit specification and task plan, 2026-11-23. This assigns inspection and knowledge maintenance; it does not authorize deployment or new business rules.

## Baseline and scope

BE worktree: `C:/Users/HoangAnhVu/.codex/worktrees/be-ai-integration/RoadGuardSystem`, branch `anh`, HEAD `4934103d125be1caca4c6a588eed3768c9cafee4`, with existing uncommitted BE–AI changes. AI gitlink baseline: `c689e544400fe326683c1c7f45115b3f7ec01daf`, with worker/Studio changes. Codex owns this slice by the owner's explicit reassignment; preserve all existing dirty paths. The execution ledger records their implementation context. Do not fetch/merge a new baseline during the audit and silently change what is being assessed.

Inspect the path DroneOperator → verified survey video → durable work/attempt → private FT95 → registered artifacts → proposals → PM decision → Crew field inspection → PM review. Inspect adjacent notification, offline, repair, reporting and retention consumers only where they consume these sources. Do not claim whole-project verification from this slice.

Authority order: owner amendments in `BE-AI-INTEGRATION-20261119.md`, `docs/product/confirmed-decisions.md`, relevant current sections of `HUY-FINAL-INTEGRATION.md`, adopted local contracts, current source, executed evidence. Historical draft paragraphs and older Knowledge indexes cannot override current decisions. Code proves implementation, not business approval.

## Readiness verdict at audit entry

CURRENT_VERIFIED: actual local FT95 inference, embedded SRT extraction, HTTP transport, SQL/MinIO registration and PM–Crew measurement confirmation have executed. The Playwright run recorded 15 passing checkpoints; its browser flow reused already ingested proposals. One-minute inference produced 63 per-frame proposals, not 63 distinct physical defects. The five-upload pilot repeated the same clip and does not establish diversity or model accuracy.

CURRENT_VERIFIED by source inspection: `AiRealWorkerExtensions` registers a background worker only when enabled; `AiWorkerRuntimeOptions.Enabled` defaults false. `tests/local-ai/PmCrewHost/Entry.cs` invokes the coordinator directly, so that harness does not prove production hosted dispatch. This checkout has `deploy/fe-handoff`, not the historical `deploy/server` package. There is no basis to infer that the home server contains these uncommitted changes.

Conclusion: core local integration is demonstrated; production attachment readiness remains conditional. UNKNOWN means missing evidence, not a demonstrated defect.

## Required business and persistence invariants

| ID | Required behavior and durable evidence | Audit task |
| --- | --- | --- |
| R01 | Only the currently authorized assigned DroneOperator submits verified project/survey/dataset media. Work retains source FileId/hash, dataset, actor and scope; unverified/foreign files are rejected. | A01, A02 |
| R02 | Each eligible submitted video has durable intent and bounded work; job/attempt/model/config identities remain traceable. Replay creates no duplicate work; failed or zero-result videos remain discoverable. | A02, A06 |
| R03 | Six distinct labels survive raw result → SQL → PM facts. `repair` means an existing patch and cannot silently create new damage. AI creates proposals only; PM controls KEEP_NEW/LINK_EXISTING/REJECT under existing rules. | A03, A05 |
| R04 | Frame timestamp/bbox/confidence/raw result and private artifact FileIds/checksums remain linked to the exact accepted attempt. Decimal persistence must not reject valid model confidence. Stale attempts cannot publish. | A03, A06 |
| R05 | AI extracts supported embedded SRT using FFmpeg; missing, invalid or ambiguous telemetry is explicit. Drone coordinates are references, never ground defect coordinates or physical dimensions. | A04 |
| R06 | Only the assigned operator appends an external SRT/estimate through BE. ETag and idempotency prevent lost updates. Immutable revisions retain actor/time/source/hash/attempt; stale PM source versions fail. | A04, A05 |
| R07 | Precedence is external SRT revision → verified paired SRT with offset → embedded SRT → estimate. Estimate leaves measured telemetry missing. Supplementation does not rerun inference or rewrite accepted PM/Crew history. | A04 |
| R08 | PM decision snapshots retain candidate/source/telemetry/geometry versions and actual decision; Crew assignment is an explicit PM action, never automatic AI dispatch. Reporter/Supervisor must not acquire PM/operator mutation privileges through this slice. | A05 |
| R09 | Crew retains actual ground location, measurements, units/checklist, evidence and explicit unknown reasons. READY derives from required evidence. PM confirmation or supplement request is durable and preserves earlier revisions. | A05 |
| R10 | Transactional field event, audit and outbox survive commit/replay. Notification recipient/current rights, delivery/retry and dedup are checked separately from merely inserting an outbox row. | A07 |
| R11 | Offline/repair/reporting consume accepted current business sources; raw AI confidence/GPS do not grant FastTrack authority, repair eligibility, measured quantity, coverage or closure. | A07 |
| R12 | Restart, interrupted input, queue pressure, lease expiry, retry, partial artifact registration and lost response cannot produce duplicate accepted business data or erase terminal attempt history. | A06 |
| R13 | Private service credential and worker port remain internal. Real application startup/DI must dispatch without manual coordinator calls. Disabled startup requires no model/token. No silent mock fallback. | A08 |
| R14 | Existing retention policy 41A applies to in-scope evidence: warranty + 5 years; technical logs 90 days, temporary exports 30 days, backups 35 days; hold blocks deletion. Missing retention basis remains WAITING_RETENTION_BASIS. Do not invent AI-specific deletion ages or run cleanup. | A07, A09 |
| R15 | Knowledge records decisions, implementation and evidence separately, with date/base/source links. Studio annotations are local metadata, not BE telemetry revisions. Search-index freshness is explicit. | A01, A10 |

## Required audit output

One matrix row per requirement and important durable field: owner/source reference → producing action → API/DTO → entity/column or private artifact → transaction/constraint → authorized readback → test/evidence → gap/severity → task. Use CURRENT_VERIFIED, TARGET_CONFIRMED, PROPOSED, UNKNOWN and HISTORICAL. A code-only observation must say source inspection; a previous executed result must identify its dated evidence. No empty cells: use UNKNOWN with a concrete check.

For persistence, compare count/hash/version before command, after commit, after replay and after restart. Include failure before commit and loss of success response after commit; assert no duplicate decision, task, revision, notification or registered artifact. Existing append-only SQL guards stay enabled. Test data stays in owned disposable resources.

Finding classes: blocker (authority bypass, data loss, invalid source acceptance, missing required durable record); integration gate (unverified hosted wiring, migration/catalog/config/host capacity); improvement (nonblocking usability/observability). Each finding needs an exact source, reproduction or explicitly missing evidence, expected rule and focused verification. Do not manufacture a fix for an UNKNOWN.

## Deployment boundary

A separate rollout package must pin BE and AI commits/images/model, verify actual Ubuntu GPU/driver/disk mount without formatting, private networking, secret files, released SQL catalog/six mappings, FFmpeg paths, migration SQL/backfill and backup/restore. Windows `D:` defaults are not Ubuntu configuration. Local 40 GiB budget/30 GiB free floor are existing local bounds; a 500 GB disk is owner-reported capacity, not a verified mounted volume.

Require private staging smoke with actual hosted worker, then explicit release handoff. Rollback disables new dispatch/worker while retaining accepted results and revisions; do not blindly migrate down over business records. Do not publish Studio or private worker via Funnel. No deployment, shared SQL seed, disk formatting, schema application, push or merge is part of this audit.

## Completion gates

1. A01–A10 each has evidence or an explicit unresolved finding; all R01–R15 map to tasks.
2. Critical missing persistence/authority findings prevent a ready verdict. Unverified integration gates prevent a production-ready verdict even if local tests pass.
3. Existing tests are reused first; new tests target discovered gaps and reproduce failure before a later authorized code fix.
4. Knowledge notes and navigation are updated without secrets, private GPS, frames, credentials or copied runtime payloads. Saving Markdown does not claim SQLite/FTS reindexing.
5. Two documented self-review passes: business/security/persistence; then source/evidence/consumer/deployment consistency. External review is reported separately.

Plan: [task checklist](../../docs/superpowers/plans/2026-11-23-be-ai-readiness-audit.md).

## Executed assessment — 2026-11-23

A01–A10 assessment completed; [requirement/field matrix and findings](../../docs/superpowers/plans/2026-11-23-be-ai-audit-matrix.md) supersedes the entry-only verdict. Local core behavior has evidence, but production attachment is BLOCKED: default SQL retry configuration rejects the worker claim transaction (isolated SQL reproduction); REAL retention linkage and SURVEY dossier integration are incomplete by source inspection. Hosted real-app/restart/race and Ubuntu prerequisites remain explicit gates. No application code, deployment, shared data or migration was changed by this audit. Test counts and self-review evidence belong to the matrix; completing assessment does not accept unresolved requirements as passing.
