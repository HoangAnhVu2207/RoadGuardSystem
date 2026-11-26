# RoadGuard Real-AI API Seed and E2E Implementation Plan

> For agentic workers: use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax.

**Goal:** Build a real-AI Docker Desktop handoff that seeds the two RoadGuard projects through BE APIs, verifies BE-to-AI and role workflows, and reuses one canonical seed manifest for additive server initialization.

**Architecture:** Extend deploy/fe-handoff with the pinned CPU AI worker and internal BE-to-AI endpoint. Keep AI private to the Docker internal network and expose only BE, Mailpit and optional MinIO loopback ports. Add one idempotent API driver that loads private credentials, uploads the approved MP4/SRT pairs through BE, creates DEMO records through existing routes, and writes sanitized receipts. Local mode resets only dedicated volumes. Server mode uses the same driver and manifest with an additive backup gate.

**Tech Stack:** Docker Compose, .NET 8 BE, Python 3.14 AI worker, SQL Server 2019, MinIO, Mailpit, Python API driver and PowerShell entrypoints.

**Spec:** docs/superpowers/specs/2026-11-26-real-ai-api-seed-e2e-design.md

## Global Constraints

- AI mock is disabled in the acceptance path and Anh02__MockEnabled=false.
- FE calls only the BE origin. AI remains internal at http://ai:8999/.
- FT95 checkpoint SHA-256 is bc14e1f0378310640d0e04583982ebb377475211d6d42837f2db494e32ed95a0.
- Seed workflow data only through existing BE APIs. SQL is used for readback and health checks.
- Credentials, bearer tokens, OTPs, signed URLs and full object keys never enter Git, images, logs or receipts.
- Local reset touches only named Docker Desktop volumes. Server mode is additive and gated by backup/checksum.
- Existing dirty plan files remain untouched.

## Review Focus

- Mock configuration accidentally enabled. Test rendered Compose environment and API configuration.
- AI service exposed to FE or BE worker endpoint misconfigured. Test network and port exposure.
- Real MP4/SRT lineage lost or fabricated GPS used. Test checksums, telemetry status, result hash and explicit missing-location state.
- Seed replay duplicates records. Test identical logical keys twice.
- Wrong-role, wrong-project and stale-version mutations are accepted. Test expected 401/403/409 responses.
- Server mode writes before backup or alters existing records. Test preflight abort and project-scope guard.

### Task 1: Canonical real-AI handoff configuration

**Files**
- Modify deploy/fe-handoff/compose.yaml
- Modify deploy/fe-handoff/compose.https.yaml
- Modify deploy/fe-handoff/init.py
- Modify deploy/fe-handoff/Dockerfile.Handoff
- Create deploy/fe-handoff/real-ai-config-test.py
- Test deploy/server/verify_compose.py and the new config test

**Interfaces**
- Consumes existing deploy/server/Dockerfile.AI, requirements-ai.lock, FT95 model path and handoff state volume.
- Produces services api, ai, sql, minio and mailpit with internal AI endpoint and mock disabled.

- [ ] Write failing checks for AI service presence, no public AI port, real worker enabled, internal endpoint and mock disabled.
- [ ] Run checks and confirm current handoff compose fails because it lacks AI and enables mock.
- [ ] Add AI service using the pinned AI Dockerfile, CPU limits, internal network, read-only model and worker token volume.
- [ ] Update handoff initialization to emit real-worker settings and remove mock enablement.
- [ ] Run compose verification and config tests. Expected result is PASS.

### Task 2: Canonical account and fixture manifest

**Files**
- Modify sandbox/seeds/v1/accounts.json
- Create deploy/fe-handoff/real-ai-seed/manifest.json
- Create deploy/fe-handoff/real-ai-seed/README.md
- Modify deploy/fe-handoff/FE-TEST-ACCOUNTS.md
- Test deploy/fe-handoff/real-ai-seed/validate_manifest.py

**Interfaces**
- Consumes private account file outside the repository and the approved media manifest at D:/RoadGuard-AI-LocalTest/server-demo-replay-20261124/manifest.json.
- Produces fourteen logical actor keys, two project keys, two real input pairs, DEMO counts, provenance labels and receipt schema.

- [ ] Validate 2 Supervisors, 2 PMs, 2 Operators, 4 Crews, 4 Reporters, two projects, two input pairs and 20-total-per-category DEMO counts.
- [ ] Record missing actor keys without printing credentials.
- [ ] Add non-secret actor metadata and logical keys. Keep passwords in the private local credential file.
- [ ] Add media checksums and explicit telemetry policy.
- [ ] Document FE handoff fields and protected credential path.
- [ ] Run manifest validation and git diff --check.

### Task 3: API seed and verification driver

**Files**
- Create deploy/fe-handoff/real-ai-seed/seed_and_verify.py
- Create deploy/fe-handoff/real-ai-seed/api_client.py
- Create deploy/fe-handoff/real-ai-seed/receipt.py
- Test deploy/fe-handoff/real-ai-seed/test_seed_contract.py

**Interfaces**
- Consumes Compose BE URL, private credential file, canonical manifest, MP4/SRT assets and current FE API routes.
- Produces idempotent API mutations, real AI job/result lineage, sanitized receipt and FE fixture lookup map.

- [ ] Test private credential loading, manifest checksum verification, idempotency keys, ETag propagation and receipt redaction.
- [ ] Implement API client with explicit 401/403/409/422 handling, ETag capture and safe polling retry.
- [ ] Implement project and membership discovery with scope guard.
- [ ] Implement real Operator upload for both pairs, dataset submission, job polling, result readback and frame checksum capture.
- [ ] Implement API-created DEMO reports, defects, measurement tasks, Normal/FastTrack repair items, evidence files, notifications and offline records where routes are adopted. Mark unsupported routes NOT_RUN.
- [ ] Implement role readback and wrong-role/project checks.
- [ ] Implement replay using the same logical keys and idempotency inputs.
- [ ] Emit sanitized receipt with environment, hashes, logical keys, returned IDs and PASS/FAIL/NOT_RUN/UNKNOWN status.

### Task 4: Local Docker execution and acceptance

**Files**
- Create deploy/fe-handoff/real-ai-seed/run-local.ps1
- Create deploy/fe-handoff/real-ai-seed/stop-local.ps1
- Create deploy/fe-handoff/real-ai-seed/local-acceptance.md
- Test Docker Desktop integration run

**Interfaces**
- Consumes Tasks 1-3 and Docker Desktop context desktop-linux.
- Produces resettable stack, local seed receipt, replay receipt and protected FE handoff directory.

- [ ] Validate Docker context and model checksum without starting services.
- [ ] Start the base real-AI stack without compose.scenarios.yaml.
- [ ] Verify BE, AI, SQL, MinIO and Mailpit health and no host AI port.
- [ ] Run the seed driver and wait for both real AI jobs.
- [ ] Run verification and inspect sanitized receipt.
- [ ] Run replay and compare logical counts and IDs.
- [ ] Copy only approved handoff artifacts, excluding secrets and runtime volumes.
- [ ] Stop stack and verify no process remains.

### Task 5: Docker package and FE handoff

**Files**
- Create deploy/fe-handoff/REAL-AI-FE-HANDOFF.md
- Create deploy/fe-handoff/real-ai-seed/package.ps1
- Modify deploy/fe-handoff/FE-QUICKSTART.md
- Test package checksum and clean-start verification

**Interfaces**
- Consumes local acceptance receipt, compose files, image tags/digests and model checksum.
- Produces FE package with BE/AI compose, role catalog, fixture manifest, endpoint guide and sanitized receipts.

- [ ] Package compose and non-secret configuration with exact image/model digests.
- [ ] Generate role guide and protected credential handoff instructions.
- [ ] Generate clean-start verification for mock disabled and two real AI jobs.
- [ ] Verify package from a clean directory with Docker Desktop.
- [ ] Run two self-review passes over scope, secrets, producer/consumer paths and rollback notes.

### Task 6: Server additive initialization

**Files**
- Create deploy/fe-handoff/real-ai-seed/run-server-additive.ps1
- Create deploy/fe-handoff/real-ai-seed/server-preflight.md
- Test server preflight and additive receipt

**Interfaces**
- Consumes exact package image/model SHA, local acceptance receipt, private server credentials and approved project names.
- Produces server backup/checksum receipt, additive seed receipt and rollback-ready status.

- [ ] Run read-only preflight for hostname, active release, containers, maintenance lock, disk/RAM, AI config and model checksum.
- [ ] Verify backup/checksum gate and stop without a valid backup.
- [ ] Apply the same API seed manifest additively to both named projects, reusing matching real AI lineage.
- [ ] Run role readback and authorization checks without printing secrets or private IDs.
- [ ] Emit server receipt with logical-key mapping and PASS/FAIL/NOT_RUN/UNKNOWN.
- [ ] Run two self-review passes and close the server session after commands finish.

## Verification summary

Before completion report Git SHA, changed paths, image tags/digests, model checksum, local seed/replay receipts, service health, real AI job counts, API status counts, package checksum and server receipt. Server writes are incomplete until their own receipt exists.
