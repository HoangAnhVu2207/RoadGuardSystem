# BE–AI Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Execute sequentially; shared files have one writer.

**Goal:** Run real FT95 video inference through an isolated local BE–AI flow, register evidence in BE, and return authorized AI proposals without changing production.

**Architecture:** Separate BE and AI containers on a private same-host network, HTTP/JSON control and streamed binary artifacts. BE dispatches committed attempts, polls durable AI jobs, pulls verified artifacts, then invokes existing validated result ingestion. No long-lived callback JWT, public AI route or AI database access.

**Tech Stack:** Existing .NET/EF Core/SQL Server/MinIO, Python/FastAPI/FT95 and Docker Compose; retain repository dependency versions.

**Spec:** [Assigned design](../../../planning/development/BE-AI-INTEGRATION-20261119.md).

**Execution:** [Small-task ledger and current evidence](2026-11-19-be-ai-integration-execution.md).
The ledger is authoritative for current task status and executed evidence. The
original steps below preserve the planning checkpoint; later execution rulings
and the assigned spec's current execution contract supersede conflicting details.

## Global Constraints

- TARGET_CONFIRMED: AI and BE will share the existing Ubuntu host; first verify locally.
- BE baseline `4934103d125be1caca4c6a588eed3768c9cafee4`, branch `anh`; do not import old local anh work.
- AI supplied baseline `555e80d7ca0d9b782d66d728920e8eb368c638a8`; this is not a claim that its remote is current.
- Production release, SQL data, public routes and Funnel configuration are outside this plan.
- Preserve Studio loopback/same-origin checks; never use the legacy worker's mock path as REAL evidence.
- No automatic approval, repair creation, GPS, physical measurement or verified coverage inferred from detections.
- Paid CKEY/Gemini and report generation are excluded. Optional road mask absence is explicit provenance.
- No weights, videos, secrets, signed URLs or runtime artifacts in Git/knowledge notes.
- Owner subsequently assigned Codex/Anh all BE–AI files, including DbContext/migrations/DI; Huy temporarily pauses these files. Preserve one writer/file.
- Plan details below are PROPOSED, not retroactive owner acceptance of a new public contract. Task 1 freezes private contract and documents any business/schema decisions that need the owner.
- No performance claim until measured. Mock, real local, and Ubuntu evidence remain separate.

## Concrete design decisions and gates

Choose BE pull-and-register for this plan. AI receives a service credential, not the callback JWT; BE's existing callback endpoint and its five-minute token behavior remain unchanged. The BE internal orchestrator must still recheck current project/attempt/model/manifest and call `IProcessingV2Service.ReceiveResultAsync`; being internal does not bypass repository validation.

Use a dedicated random service token supplied as a mounted secret on the private network. Authenticate every worker route including artifact retrieval with constant-time comparison; no credential in query strings. Never reuse user/JWT/SMTP secrets. Keep worker authority limited to job admission/read/cancel and attempt-scoped artifacts, without SQL or general filesystem access.

Private worker contract `rg-ai-worker-v1`:

| Method / path | Meaning |
|---|---|
| POST `/internal/v1/jobs` | JSON admission; 202 on first admission, 200 on identical replay, 409 on changed identity/payload |
| PUT `/internal/v1/jobs/{attemptId}/input` | Stream one verified source video; checksum/byte count required, atomically publish only after verification |
| GET `/internal/v1/jobs/{attemptId}` | Durable state, progress, stable error code, result digest when ready |
| GET `/internal/v1/jobs/{attemptId}/result` | Immutable versioned result JSON; 409 until complete |
| GET `/internal/v1/jobs/{attemptId}/artifacts/{artifactId}` | Allowlisted opaque artifact ID; no caller path or URL |
| POST `/internal/v1/jobs/{attemptId}/cancel` | Idempotent cooperative cancellation; completed results cannot be rewritten |

Admission JSON contains `contractVersion`, `jobId`, `attemptId`, `manifestHash`, `modelVersionId`, `checkpointSha256`, `preprocessingVersion`, `configVersion`, `inputSha256`, `inputBytes`, `sampleFps`, `maxFrames`. Hash a canonical payload for replay comparison. Stream BE-verified media; AI receives no public MinIO URL. Initial scope is a single video per admitted attempt; Task 1 must prove the current dataset contract can select this without silently dropping sources. Multi-source datasets are rejected explicitly until designed.

Worker states: `AWAITING_INPUT`, `QUEUED`, `RUNNING`, `COMPLETED`, `FAILED`, `CANCELLED`, `INTERRUPTED`. One running + three pending slots (including awaiting input); identical replays consume no extra slot. Admission expires after 15 minutes without input. Restart converts RUNNING to INTERRUPTED; never silently rerun a job. BE retry creates a new attempt.

Result fields: identity from admission, raw artifact, frame artifacts with hash/size/width/height, detections with stable detection UUID, frame artifact ID, timestamp milliseconds, source class, confidence and normalized bbox; checkpoint/config provenance and limitations. Final schema is written and tested in Task 1 before transport implementation.

Conversion: pixel `[x1,y1,x2,y2]` to `[x1/W,y1/H,(x2-x1)/W,(y2-y1)/H]`. Reject non-finite, inverted, zero-area or out-of-bounds boxes, invalid dimensions and negative/out-of-range timestamps; do not silently clamp. Round nonnegative seconds to milliseconds using a specified decimal half-up rule. Persist detection UUIDs when completing AI output so replay never regenerates them. Class mapping must come from active BE type codes; unknown classes fail mapping rather than become invented defects.

Local defaults: poll every 2 seconds with jitter; HTTP control timeout 10 seconds; binary inactivity timeout 60 seconds. Transport retries at 1/2/4/8/16 seconds with jitter for connection errors/408/429/5xx, honor bounded Retry-After; no automatic retries of 400/401/403/409/422/507. Exhaustion leaves a durable visible retryable failure. No automatic job retry. Task 1 defines the matching existing BE terminal reason and records it in fixtures.

Disk reservation before admission: declared input bytes + `maxFrames * (width * height * 3 + 65536)` + 64 MiB raw/provenance allowance + 2 GiB free reserve. Obtain dimensions from bounded probing, enforce reservation while decoding, stop before exceeding quota. This is a conservative local default, not a measured Ubuntu capacity promise.

## File map

All backend paths below are relative to this worktree. Worker paths are inside the proposed `AI/roadguard-ai-engine` submodule.

| Path | Responsibility |
|---|---|
| `contracts/ai/worker-v1.md`, `contracts/ai/worker-v1.schema.json` | Private wire schema, state/error/class mapping and adoption evidence |
| `tests/fixtures/ai-worker-v1/` | Shared golden and negative JSON fixtures |
| `.gitmodules`, `AI/roadguard-ai-engine` | Pin reviewed independent AI source; no copied nested .git |
| `src/api/be_worker.py` (AI) | Authenticated private HTTP boundary, no Studio routes |
| `src/api/be_worker_contract.py` (AI) | Strict request/result models and conversion |
| `src/api/be_worker_store.py` (AI) | SQLite durable admission/state/artifact manifest, no BE SQL |
| `src/api/be_worker_runner.py` (AI) | Bounded queue invoking existing real `analyze_video` |
| `tests/test_be_worker_contract.py`, `tests/test_be_worker_http.py`, `tests/test_be_worker_recovery.py` (AI) | Contract, transport and crash tests |
| `RoadGuardSystem.Services/Implementations/Processing/AiWorkerClient.cs` | Typed private HTTP client and binary streaming |
| `RoadGuardSystem.Services/Implementations/Processing/AiWorkerOptions.cs` | Validated private base URI, secret file and bounds |
| `RoadGuardSystem.Services/Implementations/Processing/AiWorkerOrchestrator.cs` | Dispatch/poll/pull/current-attempt reconciliation |
| `RoadGuardSystem.Repositories/Interfaces/Processing/IAiWorkerProgressRepository.cs` | Durable lease and progress boundary |
| `RoadGuardSystem.Repositories/Implementations/Processing/AiWorkerProgressRepository.cs` | Atomic persistence using existing outbox/attempt state where sufficient |
| `RoadGuardSystem.Services/Implementations/Processing/AiWorkerArtifactRegistrar.cs` | Verified private storage registration, stable IDs and replay recovery |
| `RoadGuardSystem.API/Extensions/ServiceCollectionExtensions.cs` | Shared DI reservation; feature disabled by default |
| `deploy/local-ai/compose.yaml`, `deploy/local-ai/README.md` | Isolated SQL/MinIO/BE/AI environment and reproducible commands |
| `tests/local-ai/run-e2e.ps1`, `tests/local-ai/benchmark.ps1` | Real HTTP checks and measured local timing |

Potential DbContext/entity/migration edits are deliberately not preapproved: Task 1 determines whether existing outbox leases and file registration can persist all required identities. If insufficient, produce the exact schema amendment and coordinate the shared writer before Task 5. Do not substitute volatile memory for missing durable state.

## Task 1 — Freeze contract against both pinned sources

**Files:** private contract/schema, shared fixtures; existing `ProcessingV2Service.cs`, `ProcessingV2PersistenceService.cs`, `AiDispatchSourceRepository.cs`, `OutboxMessage.cs`, AI `src/inference/mvp_video.py` are sources to inspect.

- [ ] Record HEAD/status and AI SHA; verify branch `anh` remains on the recorded baseline plus this task's work.
- [ ] Trace actual manifest/source selection, active type codes, file registration and current result transaction. Write exact mapping and existing failure statuses into `worker-v1.md`; prove one-video admission or stop that dependent contract for decision.
- [ ] Define schema with strict fields, positive sizes, UUIDs, lowercase SHA256, bounded numbers, immutable identity and error envelope `{code,retryable}`. Add fixtures for duplicate, conflict, stale attempt, missing model and corrupt artifact.
- [ ] Add failing BE contract test `tests/RoadGuardSystem.UnitTests/Processing/AiWorkerContractTests.cs` and AI `tests/test_be_worker_contract.py`. Include this numeric assertion in each language:

```python
def test_pixel_box_to_normalized_xywh():
    assert normalize_box([100, 50, 300, 150], 1000, 500) == [0.1, 0.1, 0.2, 0.2]
```

- [ ] Run `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter FullyQualifiedName~AiWorkerContractTests` and, from AI root, `python -m pytest tests/test_be_worker_contract.py -q`; confirm first failures concern missing intended mapping, not broken tooling.
- [ ] Implement only the pure conversion/schema fixture readers; rerun both. Record rejected cases including NaN, negative time, width zero and unknown class.
- [ ] Record contract adoption and exact persistence gap analysis. If business class equivalence or schema authority is missing, request only that decision; continue pure worker tests.
- [ ] Commit contract + fixtures + pure tests together after green; no public DTO changes.

**Exit:** one wire schema, exact class/state mappings, agreed durable registration seam. Downstream tasks consume this schema rather than invent alternate DTOs.

## Task 2 — Pin AI and implement authenticated admission

**Files:** `.gitmodules`, worker contract/store/HTTP modules; `tests/test_be_worker_http.py`.

- [ ] Add AI submodule from its verified origin at the pinned SHA; use existing local clone as reference only. Keep AI edits on a new integration branch; record modified submodule state until its commit is available remotely.
- [ ] Add HTTP tests: missing/wrong token 401, valid admission 202, replay 200 with same attempt, changed hash 409, fifth pending admission 429. Submit identical attempts concurrently and assert one stored row.
- [ ] Run `python -m pytest tests/test_be_worker_http.py -q` and observe intended failures.
- [ ] Implement worker factory `create_app()` in `src/api/be_worker.py`; durable SQLite unique attempt identity and transactionally counted slots in `be_worker_store.py`. Read token from file at startup; fail closed when absent. Reject caller-supplied URLs/paths and extra JSON fields.
- [ ] Implement input streaming to temporary attempt-owned files, digest/size enforcement, atomic rename and state transition. Repeated identical input must replay safely; wrong digest 422 removes only that temporary file.
- [ ] Add traversal, oversize input, disconnect, concurrent upload and restart tests. Rerun HTTP and contract suites.
- [ ] Commit AI change; update BE submodule pin only when that commit is retrievable by another checkout.

**Exit:** authenticated durable admission and verified input, without claiming inference is implemented.

## Task 3 — Execute the real bounded pipeline

**Files:** AI `be_worker_runner.py`, `be_worker_store.py`, recovery tests; reuse `src/inference/mvp_video.py` unchanged where possible.

- [ ] Add tests proving missing checkpoint fails, checksum mismatch fails, and REAL never constructs `MockAIAdapter`. Verify one running/three pending, cancellation and disk quota.
- [ ] Run `python -m pytest tests/test_be_worker_recovery.py -q` for the red baseline.
- [ ] Call existing `analyze_video(video, output, duration=..., sample_fps=..., max_frames=..., cancelled=..., expected_models=...)` with validated values from admitted configuration and probed video. Resolve model paths only through configured registry; no arbitrary path from the request.
- [ ] Persist state/artifact metadata before acknowledging completion; calculate raw/frame SHA256 and save stable detection identities. Apply Task 1 conversion and class map, mark absent road mask and preserve provenance. Completion means AI output ready, not BE business acceptance.
- [ ] Interrupt worker during inference, restart it and assert INTERRUPTED. Test queued recovery, cancel-before-run, cancel-during-run and disk failure cleanup without removing another attempt's files.
- [ ] Run worker suites plus `python -m pytest tests/test_mvp_video28.py tests/test_ft95_integration.py -q`; classify optional/weight-dependent skips explicitly.
- [ ] Commit reviewed AI worker changes and retrievable submodule pin.

**Exit:** real pipeline callable with durable failure/recovery. Real model pass requires actual weights; mocked runner tests are not sufficient evidence.

## Task 4 — Implement BE private transport

**Files:** `AiWorkerClient.cs`, `AiWorkerOptions.cs`; `tests/RoadGuardSystem.UnitTests/Processing/AiWorkerClientTests.cs`.

- [ ] Create typed methods `SubmitAsync`, `UploadInputAsync`, `GetStatusAsync`, `GetResultAsync`, `OpenArtifactAsync`, `CancelAsync`; all take cancellation tokens and schema-defined identities. No user-supplied base URI or redirects.
- [ ] Test fake HTTP handler: 202 accepted, 409 conflict, 429 bounded Retry-After, 503 retry, cancellation, missing credential and disallowed destination. Assert logs/exceptions contain no token or signed query.
- [ ] Run `dotnet test tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj --filter FullyQualifiedName~AiWorkerClientTests` red.
- [ ] Implement a reusable HttpClient and streamed bodies, validate media digest while streaming, dispose responses and propagate structured errors. Private service HTTP is allowed only with explicitly configured local-AI mode; production address is internal Docker DNS, not Funnel.
- [ ] Rerun transport + contract tests green; commit this independent transport slice.

**Exit:** actual client wire behavior matches worker fixtures; fake transport success is labeled mock verification only.

## Task 5 — Durable orchestration and artifact registration

**Files:** orchestrator, registrar, progress repository interface/implementation, reserved DI; tests `tests/RoadGuardSystem.IntegrationTests/Processing/AiWorkerRecoveryTests.cs` and `tests/RoadGuardSystem.ApiTests/Processing/AiWorkerAuthorizationTests.cs`.

- [ ] Add isolated SQL tests: two BE workers claim one dispatch, crash after remote acceptance, crash after object upload/before DB registration, crash after result commit/before receipt. Expect one accepted business result and stable registered file IDs.
- [ ] Run the new integration/API filters against a uniquely named local test database; fail startup if connection resolves to the home server or a shared database.
- [ ] Implement lease claim/expiry with existing `OutboxMessage.AcquireLease` semantics; do not acknowledge delivery before durable remote receipt. Reconcile accepted nonterminal attempts from DB on restart; do not depend on pending outbox alone after dispatch acknowledgment.
- [ ] Implement verified pull/register: bound artifact count/size to admission, validate digest and project/attempt relationship, create actual BE file records through the Task 1 seam, and reuse deterministic registration identity after crashes. AI-supplied GUIDs never substitute for registered FileIds.
- [ ] Recheck current attempt before each effect and ingestion. Build existing `ReceiveAiResultRequestDto` with registered raw/frame FileIds and call existing `ReceiveResultAsync`. Mark success only on accepted/replayed receipt; failed ingestion remains visible and recoverable.
- [ ] Test older-attempt completion after retry, wrong project/model/hash, two competing results, corrupted frames, result GET failure and canceled attempt. Existing external callback tests must remain green, including expired JWT rejection.
- [ ] Reserve and add DI/hosted orchestration behind disabled-by-default options; no public route or current default behavior change. Run focused processing suites and commit.

**Exit:** committed state survives every crash boundary; accepted results cannot cross projects/attempts. Any required migration follows the explicit Task 1 schema amendment, never an implicit shared-file edit.

## Task 6 — Isolated local stack and real end-to-end verification

**Files:** local compose/readme, `tests/local-ai/run-e2e.ps1`.

- [ ] Create compose project `roadguard-ai-local` with unique named SQL/MinIO/AI volumes, loopback-only BE port, AI private network without published port, model read-only mount, runtime data writable mount and private local secret files outside Git.
- [ ] Add preflight that rejects production hosts, missing checkpoint SHA/video, unsupported runtime and insufficient disk; fail rather than fallback to MOCK. Preserve existing dependency versions.
- [ ] Start with `docker compose -p roadguard-ai-local -f deploy/local-ai/compose.yaml up -d --build`; wait for explicit service health with bounded timeout.
- [ ] Through HTTP create only isolated fixtures needed by the authorized dataset/job workflow. Submit one REAL job using a permitted PM/Supervisor, poll to terminal, read proposals and fetch evidence with authorized BE media access. Record IDs and hashes without tokens.
- [ ] Assert raw/frame registered objects exist, bytes match, frame boxes/timestamps match source, checkpoint provenance is correct, and no human decision/repair was created. Verify unauthorized actor/project cannot read artifacts.
- [ ] Exercise duplicate submit/result, expired existing callback JWT, stale attempt, corrupt media, unavailable AI, missing weights, restart, cancellation, full queue and disk rejection. Use deterministic failure injection only in isolated services.
- [ ] Run `pwsh -File tests/local-ai/run-e2e.ps1` and affected .NET/Python tests. Script exits nonzero for missing real prerequisites; record mock and real results separately.
- [ ] Stop local stack without deleting volumes by default. Provide explicit cleanup confined to `roadguard-ai-local`; commit harness and evidence with secrets scrubbed.

**Exit:** real HTTP + SQL + storage + FT95 flow passes locally. Missing authorized video/weights is a named blocked real test, not success.

## Task 7 — Capacity evidence, review and knowledge handoff

**Files:** benchmark script, `docs/superpowers/plans/2026-11-19-be-ai-integration-evidence.md`, assigned spec, local Knowledge Space notes.

- [ ] Benchmark the same pinned short video/checkpoint three times after one warmup; record source hash, hardware/runtime, control latency, transfer time/bytes, queue delay, inference/ingestion/total duration and peak RAM/VRAM. Mark small sample quantiles descriptive only.
- [ ] Run queue pressure with four admitted jobs and a fifth rejected; ensure BE/SQL remain healthy and resource limits hold. Do not extrapolate a local GPU result to Ubuntu CPU.
- [ ] Self-review 1: trace spec → contract → auth/project/attempt → durable effect → tests. Self-review 2: inspect secrets, race/restart behavior, resource limits, shared-file changes and failure cleanup. Fix findings and rerun affected checks.
- [ ] Record exact commits and dirty paths, all commands with pass/fail/skip counts, mock versus real evidence and external review status. Do not call self-review external review.
- [ ] Copy spec/plan/evidence into local Knowledge Space notes and verify hashes; link them from its README. Label SQLite reindex separately if not executed.
- [ ] Prepare an Ubuntu rollout proposal only after capacity inspection: pinned image/model hashes, resource budgets, private networking, health and rollback to AI-disabled BE. No deployment command is executed by this plan.

## Planning verification (2026-11-19)

CURRENT_VERIFIED: bbox producer is pixel xyxy; BE validator accepts normalized xywh. Existing outbox has lease fields; existing dispatch source only reads committed current attempts. These observations justify contract and durability gates, not a claim that the bridge exists.

Self-review 1: all spec topics mapped to Tasks 1–7; added single-video admission gate to prevent silently dropping manifest sources and kept class mapping dependent on actual active codes.

Self-review 2: separated remote admission from BE acceptance, added polling recovery after outbox completion and artifact crash reconciliation; preserved external callback validation and production isolation.

Implementation, inference, runtime tests and Ubuntu deployment: NOT_RUN in this planning task. Required external inputs for REAL acceptance: authorized short video and exact FT95 checkpoint. Ubuntu CPU/GPU capacity remains UNKNOWN until separately inspected.

## Plan amendment — automatic embedded SRT (2026-11-19)

Apply with the spec's owner clarification. Extend Task 1 fixtures/schema to include telemetry source, extraction outcome, source hash/stream identity and derived SRT artifact. Freeze the existing parser-to-FT95 timeline adapter and derived registration seam before implementation.

Extend Task 3 with AI `src/ingestion/embedded_srt.py` and `tests/test_embedded_srt.py`: ffprobe stream discovery, bounded ffmpeg subprocess with explicit stream mapping and no shell, SRT parse validation, provenance and stable artifact output. Declare FFmpeg/FFprobe runtime dependencies in the new worker container build; fail readiness if unavailable. Do not put media extraction into BE request handlers.

- [ ] Add fixtures/tests for a known MP4 with DJI subtitle track, no subtitle, unsupported data stream, multiple ambiguous streams, invalid SRT, tool timeout, oversized output and malformed media.
- [ ] Verify explicitly paired SRT takes precedence; otherwise extract only once per input hash + stream + extractor/config version within authorized scope. Never reuse artifacts across projects without access checks.
- [ ] Test telemetry association at nonzero start times and variable-frame-rate timestamps; preserve missing GPS and reject invented physical dimensions.
- [ ] Extend Task 5 artifact registration to derived SRT without mutating the committed source manifest; obtain shared-schema decision if current persistence lacks a derived association.
- [ ] Extend Task 6 real acceptance: upload only the authorized MP4, inspect/extract its actual subtitle stream, parse telemetry, complete FT95 inference and read registered derived SRT through authorized BE access. Compare selected cue timestamps/GPS with source, and assert retry does not duplicate the artifact.

Self-review amendment: extraction is new work, not an existing AI capability; parser existence does not prove FT95 integration. No-subtitle, invalid telemetry and subprocess failure remain distinct. Runtime/real extraction tests NOT_RUN.

## Plan amendment — confirmed owner answers and main baseline

Supersedes older AI source pin and missing-checkpoint blocker: target AI c689e544400fe326683c1c7f45115b3f7ec01daf from the isolated main review clone. Both model files exist with hashes recorded in the spec; actual loading/inference remains unverified. Submodule layout is owner accepted. Do not add already tracked model bytes a second time to BE or Knowledge.

Extend Task 1 with separate drone-position versus ground-defect-position contracts, PM-only candidate decisions and assigned-operator supplements. Owner selected automatic dispatch after valid operator submission and operator-estimated drone pins (1.A/2.A, 2026-11-19); implement these without reopening the business choice. Preserve source manifests and existing reviewed candidate versions. New telemetry association persistence/API is a design amendment requiring the shared writer, not a hidden field on an existing public DTO.

Add a supplementation vertical slice after artifact registration: verified fallback SRT upload -> ownership/version check -> immutable telemetry revision -> parser association with existing detections -> authorized read with provenance. Tests must show zero extra inference calls, stable detection identities, no automatic mutation of PM decisions/repairs/crew locations, and denial of another operator/project. Include stale If-Match, idempotent retry, malformed SRT, time-offset mismatch and closed-task authorization cases.

Pilot acceptance: first the known one-minute MP4, then five approximately one-minute clips. The original five-minute sample can supply separate intervals with permission to prepare them; repeated copies only test concurrency/replay, not diversity or accuracy. One running + three pending remains the initial worker capacity: the fifth submission must receive explicit backpressure or wait durably in BE, never disappear or force increased GPU concurrency. No new production duration cap inferred from the pilot.

### Task 8 — Knowledge Space refresh with source-aware evidence

- [ ] Query current catalog for survey, AI candidate decisions, field positioning and processing; record catalog timestamp/source paths and check claims against the pinned live code.
- [ ] Create a concise current workflow note with source paths/SHAs, owner-confirmed decisions, implemented observations and unresolved decisions. Include no secrets, input video, checkpoint bytes, raw GPS or private database contents.
- [ ] Review source registry/index tooling before any rebuild: existing BE_LIVE points at another workspace. Do not overwrite that registry or promote stale records to current by relabeling.
- [ ] If indexing is included during execution, build a new versioned local index with explicit current-source mapping, preserve the old catalog, validate integrity/source hashes/secret filters, then update CURRENT only after checks. Otherwise explicitly report notes saved but not indexed.
- [ ] Verify retrieval of PM review authority, operator submission, telemetry fallback and AI checkpoint baseline from the refreshed source; evidence labels must survive retrieval.

Planning review: new source adds models only, so older findings about missing authenticated real worker and SRT-to-FT95 bridge still apply. Source review covers integration-relevant API/pipeline/parser/review/store interfaces, not a claim that every AI source file has been audited. No production change or inference test performed.


## Confirmed decision implementation steps — 2026-11-19

Extend Task 5 with transactional submission-to-dispatch integration and durable per-video scheduling. Preserve existing operator survey authorization and PM-only candidate decisions. No inference call occurs inside the SQL submission transaction.

- [ ] Add isolated SQL tests for submission rollback, idempotent replay, concurrent duplicate submission and dispatch restart. Assert one logical inference run per committed video/configuration identity and no run for rejected submission.
- [ ] Add five-input acceptance with one running and three worker-pending slots: retain the fifth in durable BE dispatch pending state and eventually admit it as capacity frees. Show per-video state; never report the full dataset complete while an item remains pending/failed.
- [ ] Implement manual-position supplementation in the same telemetry vertical slice: assigned operator, exact video/time interval, approximate drone pin, note, immutable revision, expected version and idempotency. Persist no invented sensor GPS.
- [ ] Test invalid coordinates/time bounds, overlapping intervals, stale updates, wrong operator/project, estimate outside interval, later valid SRT precedence and replay. Assert no model rerun, no extra detections and no mutation of approved decisions or crew ground location.
- [ ] Update contract/read examples and Knowledge notes with TARGET_CONFIRMED business choices separately from CURRENT_VERIFIED implementation evidence.

Two planning review passes: (1) confirmed both selected behaviors replace pending-choice text; (2) checked durable enqueue, fifth-video backpressure and manual-position provenance remain distinct from approval and ground location. Runtime implementation/test status unchanged: NOT_RUN.

## Local disk budget amendment — 2026-11-19

Apply to Tasks 3, 6 and 7: local D: has approximately 81 GB available (82.00 GiB observed); 500 GB capacity is Ubuntu-only. Use D:/RoadGuard-AI-LocalTest for local mutable data with an initial 40 GiB incremental budget and 30 GiB free-space floor. These conservative test limits supplement, rather than replace, per-job expanded-frame reservations.

- [ ] Inspect Docker/WSL image/volume location, environment caches and free space before builds. Account for disks outside D: independently; block dependent setup if space is insufficient. Do not relocate Docker or delete unrelated data automatically.
- [ ] Enforce total budget plus projected frame/output reservation before each job and while writing artifacts; produce a visible disk-budget rejection rather than fill the disk.
- [ ] Run one one-minute input first; measure actual peak disk usage before admitting the five-clip pilot. Keep one active inference; queued inputs must also count toward the budget.
- [ ] Keep models and original MP4 read-only; store derived artifacts separately. Any cleanup targets only the documented test-owned temporary files, preserving source and accepted evidence.

This update changes local capacity planning only. No test environment, files on D: or production disk were modified by this checkpoint.
