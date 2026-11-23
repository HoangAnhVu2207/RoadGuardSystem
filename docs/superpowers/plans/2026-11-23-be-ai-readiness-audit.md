# BE–AI Readiness Audit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to execute this audit task-by-task. Steps use checkbox syntax. This plan does not require delegation.

**Goal:** Establish whether new BE–AI business facts are persisted, readable and safe to integrate, and preserve the findings in Knowledge Space.

**Architecture:** Trace each business invariant from its authority through producer, contract, SQL/artifact persistence and consumer. Reuse focused local evidence, then exercise missing integration boundaries in owned disposable resources. Keep a separate production rollout gate.

**Tech Stack:** .NET/EF Core/SQL Server, private Python HTTP worker/SQLite, FFmpeg/FT95, MinIO, existing Playwright harness, Markdown knowledge notes.

**Spec:** `planning/development/BE-AI-READINESS-AUDIT-20261123.md`.

## Global Constraints

- Preserve all existing dirty paths; branch anh/base 4934103d125be1caca4c6a588eed3768c9cafee4.
- No deployment, shared SQL seed, disk formatting, schema application, push or merge is part of this audit.
- AI proposes; PM decides; operator supplements drone reference; Crew supplies ground measurements.
- Local 40 GiB incremental budget and 30 GiB free floor; no unbounded video reruns.
- Do not publish Studio or private worker via Funnel.
- UNKNOWN means missing evidence, not a demonstrated defect.

## Task artifacts and order

Create `docs/superpowers/plans/2026-11-23-be-ai-audit-matrix.md` in A01; subsequent tasks own named sections sequentially. Findings stay in that matrix rather than a second coordination ledger. Only amend implementation after a confirmed in-scope defect has a reproducing check and the applicable execution authorization; this requested phase is assessment/planning/knowledge maintenance.

A01 → A02/A03/A04 → A05 → A06/A07 → A08 → A09 → A10. Slashes identify independent review sections, not permission to run concurrent writers.

### A01 — Freeze authority and field inventory (R01–R15)

**Read:** assigned integration spec, audit spec, `docs/product/confirmed-decisions.md`, relevant HUY final sections, `contracts/ai/worker-v1.md`, `contracts/ai/video-telemetry-v1.md`, execution ledger.

**Produces:** matrix columns specified by the spec; source inventory with relative path, Git base, dirty status and evidence date.

- [x] Record `git rev-parse HEAD`, `git status --short`, `git -C AI/roadguard-ai-engine rev-parse HEAD`; preserve outputs locally without secrets.
- [x] Give each R01–R15 a matrix row; expand source IDs, actor, status, hashes, timestamps, revision and attempt fields into persistence rows.
- [x] Compare older knowledge assertions with current owner amendments; mark superseded paragraphs HISTORICAL rather than deleting evidence.
- [x] Acceptance: no requirement lacks a task, source or explicit UNKNOWN/check.

### A02 — Operator submission and durable work (R01, R02)

**Read:** `UploadPersistenceService.cs`, `SurveyV2PersistenceService.Dataset.cs`, `AiVideoWorkRepository.cs` under repository Implementations; `Processing/Worker/AiVideoWorkItem.cs` under BusinessObjects; corresponding repository configurations and transport-ledger migration.

**Tests:** `tests/RoadGuardSystem.IntegrationTests/Processing/AiVideoWorkRepositoryTests.cs`, `WorkerLedgerPersistenceTests.cs`.

- [x] Trace verified upload → submitted dataset/outbox → each video work row, including actor/project/survey/source hash and geometry readiness.
- [x] Inspect `Discovery_RequiresSubmissionIntentAndPreparesEveryVideoExactlyOnce`, concurrent preparation, unverified scope and malformed-input cases; distinguish seeded setup from actual public upload/submission.
- [x] Run the focused SQL class only if its evidence does not cover current dirty changes: `dotnet test tests/RoadGuardSystem.IntegrationTests --filter FullyQualifiedName~AiVideoWorkRepositoryTests`.
- [x] Acceptance: multi-video replay and one rejected input do not duplicate or block unrelated eligible inputs; document any public-submission-to-hosted-worker gap for A08.

### A03 — REAL result and six-label storage (R03, R04)

**Read:** `RoadGuardSystem.Services/Implementations/Processing/Worker/AiVideoCoordinator.cs`, `RealAiCandidateFactsReader.cs`, `RoadGuardSystem.Repositories/Implementations/Processing/RealAiCandidateRepository.cs`; `AI/roadguard-ai-engine/src/api/be_worker_runner.py` and adopted wire contract.

- [x] Map detection timestamp/coordinate space/confidence/source class/model to SQL and raw/frame artifact FileIds; verify class-map readiness and REPAIR_PATCH behavior.
- [x] Compare persisted hashes and counts with existing one-video and repeated-sample evidence; label synthetic geometry and repeated inputs explicitly.
- [x] Run affected reader tests if required: `dotnet test tests/RoadGuardSystem.UnitTests --filter FullyQualifiedName~RealAiCandidateFactsReaderTests`.
- [x] Acceptance: raw output, SQL facts and authorized read agree; zero automatic Defects; no mock provenance or decimal rounding false rejection.

### A04 — Telemetry and operator recovery (R05–R07)

**Read:** `AiVideoTelemetryController.cs`, `AiVideoTelemetryService.cs`, `AiVideoTelemetry.cs`, `AiVideoTelemetryRepository.cs`, `AiVideoTelemetryRevision.cs` in their existing layer folders; telemetry contract and migration.

- [x] Inspect measured/missing/invalid/ambiguous SRT, offsets, immutable revision history, actor/attempt binding and ETag/idempotency ordering.
- [x] Inventory which fallback branches have unit, fake-repository HTTP and real SQL evidence; do not describe the HTTP fake as full SQL end-to-end.
- [x] Reuse/run `AiVideoTelemetryTests`, `AiVideoTelemetryRepositoryTests` and `AiVideoTelemetryHttpTests`; target missing commit-response/restart coverage rather than repeating every suite blindly.
- [x] Acceptance: each fallback is read back from BE, measured telemetry beats estimates, stale PM facts are rejected, and local Studio annotation is never counted as a BE revision.

### A05 — PM decisions and Crew measurement (R03, R08, R09)

**Read:** `CandidateDecisionRepository.cs`, `FieldInspectionWorkflowRepository.Business.cs`, existing field workflow files, `tests/RoadGuardSystem.IntegrationTests/Inspections/H3FieldInspectionWorkflowSqlTests.cs` and candidate API acceptance classes.

- [x] Trace KEEP_NEW, LINK_EXISTING and REJECT separately; verify repair-patch restriction, current project membership, decision snapshot and old attempt fencing.
- [x] Trace assignment → accept/start → verified photo → measurement submission → PM CONFIRM or SUPPLEMENT; list durable event/revision/evidence and authorized reads for each transition.
- [x] Compare existing Playwright report with these branches: its successful KEEP_NEW/CONFIRM path does not prove LINK_EXISTING/REJECT/SUPPLEMENT/reassignment.
- [x] Acceptance: AI cannot self-approve, Crew cannot decide candidates, drone coordinates do not populate ground measurements, and earlier accepted snapshots survive later telemetry updates.

### A06 — Failure, retry and no data loss (R02, R04, R12)

**Read/tests:** `AiVideoCoordinatorTests.cs`, `AiVideoAttemptOutcomeRepositoryTests.cs`, `AiVideoWorkRepositoryTests.cs`, `AI/roadguard-ai-engine/tests/test_be_worker_recovery.py`, `test_be_worker_runtime.py`.

- [x] Build failure timeline: admitted/input interrupted/running/completed-artifacts/BE committed/response lost. Identify each durable recovery owner and lease fence.
- [x] Inspect restart, queue one-running/three-waiting, zero detections, cancellation, budget exhaustion, stale attempt, artifact failure and attempt-outcome history assertions.
- [x] Mark ambiguous-commit injection UNKNOWN unless a test actually loses the post-commit response; ordinary replay alone does not establish it.
- [x] Acceptance: counts and hashes after retry/restart match accepted history; no duplicate Defect, revision or artifact registration. Missing cases become exact regression tasks with expected pre/post counts.

### A07 — Adjacent business consumers (R10, R11, R14)

**Read:** field workflow `Emit`, existing notification/outbox dispatcher, offline field adapters, repair eligibility, reporting/export and retention code reached through actual source references. Use `rg -n 'field.task.assigned.v1|field.task.submitted.v1|field.task.supplement_requested.v1' RoadGuardSystem.*` to locate consumers.

- [x] Separate durable event/audit/outbox insertion from actual delivery; record recipients, current-right checks, dedup identity, retry and replay.
- [x] Trace accepted field sources into offline/repair/reporting. Record gates for NORMAL/FastTrack; do not infer authority from AI output or broaden this audit into all repair behavior.
- [x] Compare artifact/video retention and legal hold with policy41A; flag missing source linkage without enabling deletion.
- [x] Acceptance: no missing required recipient/source record, no double count from per-frame proposals, and no deletion date inferred from upload when retention basis is absent.

### A08 — Actual application startup and contract adoption (R13)

**Read:** `RoadGuardSystem.API/Program.cs`, `Extensions/AiRealWorkerExtensions.cs`, `Workers/AiRealVideoWorker.cs`, `AiWorkerRuntimeOptions.cs`; `tests/local-ai/PmCrewHost/Entry.cs` and AI `src/api/be_worker_runtime.py`.

- [x] Contrast manual coordinator harness with production registration, DI lifetimes, geometry/catalog readiness, private token file and worker process startup.
- [x] Specify isolated real-app test: enable worker with test-only absolute paths and matching released catalog; submit verified dataset through API; wait for hosted dispatch/ingestion without calling TickAsync from the test; restart and read persisted work.
- [x] Record whether that test exists/passes. A test plan alone leaves this gate UNKNOWN.
- [x] Acceptance: disabled startup is model/token free; enabled valid startup processes automatically; missing model/map/path fails explicitly and never invokes mock inference.

### A09 — Separate Ubuntu rollout gate (R13, R14)

**Produces:** a rollout prerequisite section in the matrix, not an executable server mutation script.

- [x] List pinned BE/AI images/model/FFmpeg, private network/secret mounts, Linux paths, released catalog and additive migration/backfill prerequisites.
- [x] List read-only host checks for GPU/driver, filesystem/mount/capacity and backup/restore evidence; preserve owner-reported 500GB as unverified until observed.
- [x] Define smoke/rollback: disable dispatch and worker on failure, retain accepted SQL/MinIO records, no blind migration-down, no public worker/Studio.
- [x] Acceptance: each prerequisite has evidence or a blocking gate; no production-ready assertion with unresolved gates.

### A10 — Knowledge reconciliation and final verdict (R15)

**Files:** Knowledge Space `notes/2026-11-23-be-ai-readiness.md`, its README navigation, existing Studio note, this spec and audit matrix.

- [x] Save durable decisions, current facts and evidence limits with source paths/date/base; exclude private coordinates, media and credentials.
- [x] Link notes from Knowledge README. State whether SQLite/FTS was rebuilt; current index sources point elsewhere and do not automatically include this worktree or new notes.
- [x] Self-review1: map every required field/authority invariant to persistence and readback. Self-review2: check evidence labels, source paths, consumers, readiness gates and privacy.
- [x] Deliver verdict with completed checks, concrete findings and remaining ordered tasks. Record external review separately; do not label self-review independent review.

## Planning checkpoint completed 2026-11-23

CURRENT_VERIFIED source inspection: background worker opt-in exists; local harness directly ticks coordinator; four worker persistence entities and three additive migrations exist; telemetry HTTP tests explicitly use a fake repository; field Emit writes event/audit/outbox. Policy41A retention was found and included. These findings are not new full-suite execution.

Self-review1: R01–R15 each map to tasks; PM/operator/Crew boundaries and immutable snapshots retained. Self-review2: corrected the distinction between local annotations and BE revisions, outbox and notification delivery, manual harness and hosted dispatch, repeated clips and model validation. External review for this audit: NOT_REQUESTED. HISTORICAL planning checkpoint: execution was pending at that point. The executed assessment below supersedes that status.

## Execution checkpoint — 2026-11-23

A01–A10 assessment completed. All checked boxes mean the inspection/test/evidence disposition was completed; unresolved acceptance gates remain open as findings, not product PASS. [Executed requirement and durable-field matrix](2026-11-23-be-ai-audit-matrix.md) contains each R01–R15, exact source, readback, test limits and ordered remediation.

| Task | Executed result |
| --- | --- |
| A01 | Frozen BE/AI HEAD and dirty-path evidence, source hash manifest; authority and field matrix; no baseline fetch/merge |
| A02 | Operator/source/intent trace and fresh isolated SQL work/ledger checks; default retry-context blocker F01 reproduced separately |
| A03 | Six labels/raw/frame/current-attempt trace; fresh reader tests; prior FT95/MinIO results accurately classified; no model accuracy claim |
| A04 | Immutable telemetry/precedence/ETag/idempotency traced; fresh unit/SQL/HTTP checks; fake persistence in HTTP explicitly labeled; F06 race/readback gate |
| A05 | KEEP_NEW/LINK_EXISTING/REJECT and FIELD assign/measure/CONFIRM/SUPPLEMENT/reassign reviewed; fresh H3 SQL checks; PM classification correction preserved |
| A06 | Python recovery/runtime plus BE lease/outcome checks; real SQL retry reproduction; AI post-commit ACK-loss gap specified, FIELD lost-ACK evidence kept distinct |
| A07 | Actual H6 notification recipients/delivery/retry, offline, repair, statistics, reporting, retention traced; fresh consumer SQL tests; F02 REAL retention linkage and F03 SURVEY dossier gate |
| A08 | Actual Program/DI/worker composition compared with manual harness; exact real-app hosted/restart test specified; readiness remains blocked F01/F04 |
| A09 | Read-only Ubuntu prerequisite checklist, private smoke and rollback defined; actual host/GPU/disk/deployment not verified |
| A10 | Knowledge note/README reconciled, two self-reviews recorded; no FTS rebuild or external GitHub review claimed |

No application fixes were made by this assessment. The spec assigns audit/knowledge work; follow-up T01–T06 are concrete reproductions/design/verification tasks, not hidden deploy authorization. Production integration is blocked by the findings in the matrix.

\n