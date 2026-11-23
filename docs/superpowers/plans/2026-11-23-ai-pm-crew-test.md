# AI PM–Crew Local Test Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans. Track steps below.

**Goal:** Let the owner select an actual AI proposal as PM, assign project Crew, submit measurements and review them through real local BE.

**Architecture:** Keep the loopback Studio, private worker and isolated SQL/MinIO. Studio relays a restricted set of existing APIs using distinct authenticated PM/Crew identities. Preserve REAL AI provenance through dataset → survey when creating a field task.

**Tech Stack:** .NET 8, EF SQL Server, MinIO, Python FastAPI, plain browser JavaScript.

**Spec:** planning/development/BE-AI-INTEGRATION-20261119.md and owner-approved PM → Crew local test extension in this conversation.

## Global Constraints

- Local only; no Ubuntu deployment, remote data, schema changes, push or merge.
- Codex owns this BE–AI slice; preserve all pre-existing dirty paths. Base 4934103d125be1caca4c6a588eed3768c9cafee4, branch anh in existing linked worktree.
- Actual inference and accepted BE results; AI proposes, PM decides. Six labels; repair means existing patch.
- Drone GPS is reference only. Crew enters actual damage position and measurements.
- D: mutable storage, 40 GiB incremental budget, minimum 30 GiB free.
- Existing service authorization, ETags, idempotency and evidence validation remain enforced. No production test-auth bypass.

### Task 1: REAL AI field-task provenance

**Files:** FieldInspectionWorkflowRepository.Business.cs and a focused SQL regression in tests/RoadGuardSystem.IntegrationTests/Inspections/.
**Interface:** Existing FieldTaskCreateInput/FieldWorkflowCommand; no public contract changes.

- [x] Added 11-case SQL regression for REAL ledger/attempt provenance and accepted source.
- [x] Red: 6 unsafe REAL states incorrectly admitted; valid REAL lineage already worked.
- [x] Added REAL-only ledger/attempt guard alongside legacy lineage, preserving existing business checks.
- [x] Green: full H3 class 44/44 passed on owned SQL; both self-review passes complete.

### Task 2: Isolated persistent BE fixture

**Files:** tests/local-ai/ local host project/launcher and fixture helpers; reuse existing SQL/MinIO and actual worker integration patterns.
**Interface:** Loopback BE HTTP origin; local fixture context includes project/survey/route/segment and two separate real bearer identities, never production credentials.

- [x] Inspect API host/auth DI and existing integration fixture startup; create opt-in harness with private randomized local resources and loopback binding.
- [x] Seed project members, published geometry and supported catalogs in test DB; ingest authorized minute sample with actual worker through normal coordinator.
- [x] Exercise candidate read/PM decision/field create with real HTTP and distinct tokens. Verify Crew cannot perform PM decision.
- [x] Keep host alive for user test; record owned PIDs/container IDs and safe stop instructions. Never import standalone result as trusted BE data.

### Task 3: Studio workflow UI

**Files:** AI/roadguard-ai-engine/src/api/test_studio.py, src/api/web/test_studio/{index.html,studio.js,studio.css}, tests/test_test_studio.py.
**Interface:** Existing BE relay; explicit local fixture metadata only, tokens in browser memory or protected local harness session, no internal worker credential exposed.

- [x] Added failing relay/fixture/static/photo tests, then made them pass.
- [x] Extended allowlist and implemented guided PM/Crew panels; live verification pending.
- [x] Added selected box, time-aligned BE telemetry and separate ground location/readiness.
- [x] Run Python regressions and JS syntax check; verify browser workflow and negative role request.

### Task 4: Verification and handoff

**Files:** tests/local-ai/README.md, this ledger and existing Knowledge Space note.

- [x] Run affected SQL/HTTP/Python tests and actual local workflow; record exact results and geometry/fixture limits.
- [x] Self-review 1: authorization, source scope, transaction, retry/ETag; self-review 2: dirty preservation, interfaces, resource limits and documentation.
- [x] Independent focused review; fix material issues and rerun affected checks.
- [x] Leave verified local UI/BE running; provide URL and short test steps. Preserve branch uncommitted for owner review.

## Executed evidence — 2026-11-23

CURRENT_VERIFIED: local host PID 23876 at 5087; Studio at 8765, private worker at 8997. Rebuilt host ingested the authorized minute sample: 63 per-frame proposals, 60 SRT intervals. Telemetry HTTP 200, EMBEDDED_SRT. Distinct real PM/Crew authentication; Crew candidate mutation denied 403; foreign Origin denied 403.

Browser executed PM KEEP_NEW → field task NewAssigned → Crew Accepted → InProgress → normal multipart MinIO photo VERIFIED → submission READY with no missing reasons → PM CONFIRM → Completed. Task 6096dca4-8ba5-4456-9ca8-4eb3b4a9563e. Geometry is a synthetic straight 100 m fixture; entered measurements/photo are test evidence only. Readback and screenshot saved under protected D:/RoadGuard-AI-LocalTest/pm-crew/ (pm-crew-workflow-evidence.json and pm-crew-completed.jpg). No browser warning/error entries observed.

Focused verification: H3 SQL 44/44 (agent evidence); telemetry SQL class 17/17 before added retry replay case, then replay theory 2/2 including simulated transient insert; candidate reader 39/39; SRT parser 8/8 with two red cases before fix; Studio/runtime Python 22/22; JavaScript syntax and both repository diff whitespace checks passed. No broad unrelated suite claim.

Live defects fixed: exact confidence projection to existing SQL decimal(6,5); configured EF retry strategy around telemetry transactions and detach failed insert before replay; FFmpeg SRT with multiple blank separator lines. No schema/wire changes for these fixes.

Self-review 1 checked authorization/project scope, immutable source/hash proof, ETags/idempotency and retry transaction lifetime. Self-review 2 checked credentials remain server-side/private, loopback origins, real upload verification, resource budget, existing dirty files and truthful fixture limits. Independent review found task-selection failure retaining a previous task; readTask now clears actionable state and assigns task/submission together only after successful reads. Previously found busy-state and candidate-load hazards were also fixed. External deployment and real field accuracy remain NOT VERIFIED.

Final task-selection behavior regression: Node VM test reproduced enabled upload against the vulnerable source, then passed 1/1 with the fix. Browser reloaded the final script and reread the completed task and READY submission. Final user view selects proposal 2 for another manual test. Local listeners 5087/8765/8997 remain loopback only.

## Owner-requested Playwright acceptance — 2026-11-23

Added only tests/local-ai/{requirements-playwright.txt,test-studio-playwright.py,run-studio-playwright.ps1} and this/README documentation. Installed Playwright 1.65.2 in the D: test venv with managed Chromium on D:. No production dependency change. Headed Playwright ran 15 checkpoints against real local BE/SQL/MinIO, including negative role/input checks, embedded SRT rendering, PM–Crew completion, reload persistence, 390px layout and zero browser errors. Report/video/screenshots/trace are in D:/RoadGuard-AI-LocalTest/pm-crew/playwright/20261123T091124732529Z/. Completed task f0f18ad5-adf1-4116-93e2-b2a479cce9d5. First execution exposed the test's default 5s assertion timeout; explicit 60s assertions handle normal asynchronous upload verification. No app code was changed. Self-review 1: local-only target, private fixture metadata, actual mutations/negative checks. Self-review 2: pinned test dependency, D: browser/artifact storage, unique run outputs, no destructive reset, truthful existing-inference scope. No external code review requested for this tooling-only addition. `pip check` clean. D: retains 68.93 GiB free.
