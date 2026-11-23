# AI Test Studio Implementation Plan

> **For agentic workers:** Use superpowers:subagent-driven-development for the independent UI and local bridge, followed by focused review.

**Goal:** Give the owner a working local UI to upload video, execute actual FT95 inference and inspect proposals, plus an explicit local BE API workbench.

**Architecture:** A loopback-only FastAPI studio embeds the existing authenticated worker runtime. Its private service credential never reaches the browser. Vanilla HTML/CSS/JS uses same-origin studio routes. BE calls target an operator-configured loopback backend only; no production defaults.

**Tech Stack:** Existing FastAPI, httpx, SQLite worker ledger, FT95, FFmpeg, vanilla browser APIs; no new dependencies.

**Spec:** Owner-approved UI scope in chat, supplementing planning/development/BE-AI-INTEGRATION-20261119.md.

## Global constraints / accepted specification

- Local only: 127.0.0.1:8765. Reject non-loopback clients, unrecognized Host, foreign Origin, and mutations without X-Studio-Request: 1.
- All mutable video/artifact/ledger data resides on D: on Windows. 40 GiB studio/worker budget and 30 GiB disk free floor. Video limit 2 GiB, duration <= 180 seconds, sampling 1 FPS, max 180 frames, one running/three waiting.
- AI mode uses the pinned FT95 worker, six original labels, real frame PTS, normalized bbox. Repair means previously patched area; detections are proposals, never approved Defects.
- UI supports video selection/preview, streaming upload progress, durable job history/status, cancellation, explicit terminal retry, result frames/filter/bbox/confidence/timestamps, raw result download, extracted SRT download.
- Missing GPS is shown honestly. Local external SRT/estimated positions are separate local annotations, clearly not BE revisions or ground damage locations. BE telemetry uses existing revision routes and ETag/idempotency headers.
- BE mode is an API workbench for a separately started local BE: bearer credential in browser memory only, explicit route/method allowlist, forwarded ETag/idempotency headers, no internal callbacks, no arbitrary proxy URLs, redirects disabled. Display unconfigured BE explicitly. Do not claim a full BE integration run without one.
- No production writes/deployment, no shared DB mutation, no release/schema/dependency changes. Preserve all pre-existing dirty BE–AI files.

## Task 1: Local bridge and safety boundary

Files: AI/roadguard-ai-engine/src/api/test_studio.py; tests/test_test_studio.py.

- [x] Write failing transport tests for Host/Origin/mutation header, bounded upload, job isolation, configured local BE proxy.
- [x] Embed worker lifespan; stream upload to D:, hash/probe/admit/transfer; retain input for preview/retry. Persist names/job IDs in studio SQLite. Reconcile worker state on reads.
- [x] Serve only registered jobs/artifacts. Implement explicit local-only annotations and bounded local BE relay.
- [x] Run focused tests and self-review safety/resource/error handling.

## Task 2: Browser UI

Files: AI/roadguard-ai-engine/src/api/web/test_studio/{index.html,studio.css,studio.js}.

- [x] Build accessible responsive UI consuming the bridge contract below.
- [x] Show truthful empty/loading/error states, detection filtering, frame overlays, six class legend, missing GPS and PM authority.
- [x] Add local BE workbench using actual route templates and JSON payload editor; distinguish standalone vs BE state.
- [x] Verify JS syntax and actual browser interaction.

## Task 3: Actual inference, review and handoff

Files: tests/local-ai/README.md and this ledger.

- [x] Run actual one-minute source through studio upload; wait for terminal result; verify artifact image/JSON/SRT behavior.
- [x] Browser check desktop/mobile, upload controls, completed detections and console errors.
- [x] Two self-review passes and independent code review; fix material issues and rerun focused checks.
- [x] Document launch/stop instructions, evidence and limitations. Keep local preview running.

## UI bridge contract

- GET /studio/config => {ready, classes, model, sampleFps, maxUploadBytes, maxDurationSeconds, beConfigured, positionMeaning}.
- GET /studio/jobs => {items:[{id,name,createdAt,state,errorCode,inputBytes,result?}]}.
- POST /studio/jobs raw video body, Content-Length and X-Video-Name (encodeURIComponent filename), X-Studio-Request:1 => job metadata. Upload probes before acknowledging; UI must show analyzing metadata until response.
- GET /studio/jobs/{id} => metadata plus result when completed (rg-ai-worker-v1); result.detections fields detectionId,frameArtifactId,timestampMs,sourceClass,confidence,bbox=[x,y,w,h]. result.frameArtifacts descriptors, telemetry and limitations.
- GET /studio/jobs/{id}/video => original uploaded MP4; GET /studio/jobs/{id}/artifacts/{artifactId} => registered result bytes only.
- POST /studio/jobs/{id}/cancel or /retry (empty body) => metadata; retry returns NEW id, terminal FAILED/INTERRUPTED/CANCELLED only.
- GET/POST /studio/jobs/{id}/annotation => {kind:EXTERNAL_SRT|OPERATOR_ESTIMATE,srtText?,latitude?,longitude?,reason?}. Local only, no impact on immutable worker result; render saved annotation separately. max SRT 1 MiB, bounded finite coordinates, nonempty reason for estimates.
- POST /studio/be {method,path,body?,etag?,idempotencyKey?}, Authorization:Bearer <user token> => {status,etag,body}; fixed configured local BE, allowlisted public AI/project/survey/upload/candidate routes only, no cookie auth. No token persistence/logging.

## Execution evidence

Baseline HEAD 4934103d125be1caca4c6a588eed3768c9cafee4, branch anh, pre-existing BE–AI changes preserved. Owner assigns all BE–AI slice files to Codex; UI files are new. UI and bridge have separate named writers.

CURRENT_VERIFIED 2026-11-23: 19 studio/runtime tests PASS, node --check PASS.
Real pilot IDs 057224c8-0660-4c98-9d9c-ed5ec7c37ae4 and
a5d21a41-8df0-47fa-b406-87116757d271 both COMPLETED with 60 frames and
63 per-frame proposals, extracted embedded SRT. Second upload used actual browser
file chooser/upload UI. Verified downloaded JPEG/SRT descriptor hashes, durable
history after server restart, desktop/narrow layout, confidence filtering and
unconfigured BE send disabled. Browser console warnings/errors: none observed.
Evidence: D:/RoadGuard-AI-LocalTest/studio/studio-evidence.json;
screenshot studio-desktop.jpg. No private video/GPS copied into repository.

Self-review 1: corrected static route shadowing, SQLite connection lifetime and
truthful UI empty/configuration states. Self-review 2: checked loopback/Origin
boundary, upload cleanup/retry, credential lifetime, resource reservations and
route templates. Independent read-only review found whole-root reservation
accounting and dataset-read allowlist mismatches; both fixed with regressions.
Reviewer confirmed fixes and 10/10 studio tests, with no remaining material
findings in reviewed scope. Root suite includes 9 runtime tests (19 total).

CURRENT_VERIFIED scope: standalone FT95 UI. BE workbench is implemented but
live local BE is not configured in this session. No server deployment, shared DB,
schema, data or release changes; all previous dirty integration work preserved.
